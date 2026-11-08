using UnityEngine;
using UnityEngine.Rendering;

namespace UnderTheSea
{
    /// <summary>
    /// 柔软海草。每一根海草都是独立的一串节点(Verlet 软体),
    /// 有自己的长度、宽度、相位、频率和受力,所以每根的摆动都不一样;
    /// 整体还叠加一股从一侧推过来的"海浪",波峰依次经过每一根。
    ///
    /// - 平时:海草围绕在参与者(主角珊瑚)根部,随水流轻轻摇摆。
    /// - 场景3 探头检查(2026-11-05 与涂耦合剂互换):海草随海浪一波一波地扑向珊瑚身体;
    ///   探头的 Tag 1 被识别时,附近的海草跟着探头走,并在探头"按压"的位置被压扁、向四周摊开;
    ///   丢失时回到默认的海浪抚摸。天黑过渡时回到正常。
    /// - 场景4 吸气:海草螺旋地包裹住参与者。
    /// - 海草不会穿过珊瑚身体,也不会扫到参与者的眼睛(相机)。
    /// 所有海草合并成一个网格绘制,开销很小,适合 Quest。
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class SoftSeaweedStrands : MonoBehaviour
    {
        [Header("引用(由场景生成器填写)")]
        public Transform body;                     // 参与者 = 主角珊瑚
        public Transform eye;                      // 相机,海草不扫到眼睛
        public Transform tagMarker;                // 可选:显示被追踪 tag 当前位置的小球(调试可视化)
        [Tooltip("测试用:显示真实 tag 位置的小黄点(与小丑鱼同时显示)。运行时键盘 T 开关")] public bool showTagMarker = true;
        [Tooltip("海草跟随哪个 tag。2026-11-05 起为探头的 Tag 1(TagIds.Fish);改回 0 即助手手持的水草道具")]
        public int trackTagId = TagIds.Fish;
        [Tooltip("追踪时,附近海草叶尖追 tag 的力度")] public float tagFollowStrength = 24f;
        [Tooltip("按压:探头位置附近的海草被压向身体并向四周摊开的力度")] public float pressStrength = 10f;
        [Tooltip("按压影响的半径(米)")] public float pressRadius = .32f;
        [Tooltip("场景4 吸气时海草包裹身体的力度")] public float wrapStrength = 12f;
        [Header("小丑鱼(2026-11-05:替换 tag 小黄点)")]
        [Tooltip("拨动海草的小丑鱼;为空时直接用 tag 位置")] public Transform interactor;
        [Tooltip("tag 离控制区域多远(米)以内仍算在区域里")] public float regionMargin = .08f;
        [Tooltip("小丑鱼自己随机游动时,拨动海草的力度(相对跟随 tag 时)")] [Range(0f, 1f)] public float wanderPress = .45f;
        [Tooltip("小丑鱼身边被拨开的半径(米)")] public float fishClearRadius = .11f;
        [Tooltip("小丑鱼拨开周围叶片的力度")] public float fishPushStrength = 30f;

        /// <summary>tag 被追踪到且在海草控制区域内(小丑鱼此时跟随 tag)。</summary>
        public bool TagInRegion { get; private set; }
        /// <summary>tag 位置限制到控制区域后的点。</summary>
        public Vector3 TagRegionPoint { get; private set; }
        public Vector3 RegionCenter { get; private set; }
        public float RegionRadius { get; private set; }
        public float RegionHeight { get; private set; }

        [Header("形态")]
        public int strandCount = 24;
        public int segments = 14;
        public Vector2 lengthRange = new Vector2(.85f, 1.6f);
        public Vector2 widthRange = new Vector2(.05f, .09f);
        public float ringInner = .45f, ringOuter = 1.05f;   // 根部离身体中轴的水平距离(相对身体半径的倍数在下面再加)

        [Header("运动")]
        public float buoyancy = 2.2f;
        public float currentStrength = 1.4f;
        public float waveStrength = 1.6f;
        public float waveSpeed = .55f;
        [Range(.8f, 1f)] public float damping = .965f;
        public float strokeStrength = 6f;
        [Range(0f, .5f)] public float bendStiffness = .22f;
        public int iterations = 5;

        /// <summary>Tag 0 当前是否在驱动抚摸位置(自检用)。</summary>
        public bool TagTracked { get; private set; }
        /// <summary>涂耦合剂时贴在身体表面的海草节点比例 0..1(自检用)。</summary>
        public float ContactRatio { get; private set; }
        /// <summary>当前按压深度 0..1(探头 tag 越贴近身体越深;自检用)。</summary>
        public float PressDepth { get; private set; }
        /// <summary>当前包裹程度 0..1(场景4 吸气时)。</summary>
        public float WrapAmount => wrap;
        public int StrandCount => strandCount;

        OceanStageView view;
        Mesh mesh;
        Vector3[] pos, prev, verts, normals;
        Vector2[] uvs;
        Color[] colors;
        float[] segLen, width, phase, freqA, freqB, amp, twist, wave, lean;
        Vector3[] root, facing, offset;
        int nodes;
        float gel, wrap, strokeClock, lostTime = 999f;   // 一开始视为"未追踪到",避免没有 tag 也报告已追踪
        Vector3 strokePoint, tagPoint;
        bool initialized;

        public void Initialize(OceanStageView stageView)
        {
            view = stageView;
            nodes = segments + 1;
            int n = strandCount * nodes;
            pos = new Vector3[n]; prev = new Vector3[n];
            segLen = new float[strandCount]; width = new float[strandCount];
            phase = new float[strandCount]; freqA = new float[strandCount]; freqB = new float[strandCount];
            amp = new float[strandCount]; twist = new float[strandCount]; wave = new float[strandCount];
            lean = new float[strandCount];
            root = new Vector3[strandCount]; facing = new Vector3[strandCount]; offset = new Vector3[strandCount];

            BodyCapsule(out Vector3 baseC, out float radius, out _);
            var rng = new System.Random(20261105);
            float R() => (float)rng.NextDouble();
            for (int s = 0; s < strandCount; s++)
            {
                // 根部:围绕身体一圈,黄金角分布 + 抖动,每根位置都不同
                float a = s * 2.39996f + R() * .5f;
                float r = radius + Mathf.Lerp(ringInner, ringOuter, R()) * .6f;
                root[s] = baseC + new Vector3(Mathf.Cos(a) * r, .02f, Mathf.Sin(a) * r);
                float len = Mathf.Lerp(lengthRange.x, lengthRange.y, R());
                segLen[s] = len / segments;
                width[s] = Mathf.Lerp(widthRange.x, widthRange.y, R());
                phase[s] = R() * 100f;
                freqA[s] = Mathf.Lerp(.35f, .8f, R());
                freqB[s] = Mathf.Lerp(.9f, 1.7f, R());
                amp[s] = Mathf.Lerp(.6f, 1.3f, R());
                twist[s] = Mathf.Lerp(-35f, 35f, R());
                wave[s] = a;                              // 海浪按方位依次经过
                lean[s] = Mathf.Lerp(.6f, 1.2f, R());
                float fa = R() * Mathf.PI * 2f;
                facing[s] = new Vector3(Mathf.Cos(fa), 0f, Mathf.Sin(fa));
                offset[s] = new Vector3(R() - .5f, R() - .5f, R() - .5f) * .35f;
                for (int k = 0; k < nodes; k++)
                {
                    int i = s * nodes + k;
                    pos[i] = prev[i] = root[s] + Vector3.up * (segLen[s] * k);
                }
            }

            mesh = new Mesh { name = "Soft seaweed strands" };
            mesh.MarkDynamic();
            int vc = strandCount * nodes * 2;
            verts = new Vector3[vc]; normals = new Vector3[vc]; uvs = new Vector2[vc]; colors = new Color[vc];
            var tris = new int[strandCount * segments * 6];
            int ti = 0;
            for (int s = 0; s < strandCount; s++)
            {
                Color tint = Color.Lerp(new Color(.85f, .95f, .85f), new Color(1.05f, 1f, .9f), (float)rng.NextDouble());
                for (int k = 0; k < nodes; k++)
                {
                    int v = (s * nodes + k) * 2;
                    float u = k / (float)segments;
                    uvs[v] = new Vector2(0f, u); uvs[v + 1] = new Vector2(1f, u);
                    colors[v] = colors[v + 1] = tint;
                    if (k == segments) continue;
                    tris[ti++] = v; tris[ti++] = v + 2; tris[ti++] = v + 1;
                    tris[ti++] = v + 1; tris[ti++] = v + 2; tris[ti++] = v + 3;
                }
            }
            mesh.vertices = verts; mesh.normals = normals; mesh.uv = uvs; mesh.colors = colors; mesh.triangles = tris;
            GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = GetComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            transform.position = Vector3.zero; transform.rotation = Quaternion.identity; transform.localScale = Vector3.one;
            strokePoint = tagPoint = baseC + Vector3.up * .5f;
            initialized = true;
        }

        /// <summary>主角珊瑚近似为竖直胶囊:底部中心、半径、高度(随呼吸缩放实时更新)。</summary>
        void BodyCapsule(out Vector3 baseCenter, out float radius, out float height)
        {
            var r = body.GetComponent<Renderer>();
            Bounds b = r ? r.bounds : new Bounds(body.position + Vector3.up * .5f, Vector3.one);
            baseCenter = new Vector3(b.center.x, body.position.y, b.center.z);
            radius = Mathf.Max(.12f, Mathf.Max(b.extents.x, b.extents.z) * .8f);
            height = Mathf.Max(.2f, b.max.y - body.position.y);
        }

        public void Tick(Stage s, float dt)
        {
            if (!initialized) return;
            dt = Mathf.Min(dt, 1f / 30f);
            float t = Time.time;
            float reveal = view.Reveal(.15f);
            var mr = GetComponent<MeshRenderer>();
            mr.enabled = reveal > .01f;
            if (!mr.enabled) return;

            // 2026-11-05:与涂耦合剂互换 —— 海草的抚摸/追踪在场景3 探头检查;天黑过渡时回到正常
            bool gelStage = s == Stage.Probe;
            gel = Mathf.MoveTowards(gel, gelStage ? 1f : 0f, dt * (gelStage ? .5f : .35f));
            // 场景4 吸气时包裹身体(跟随呼吸值,3 秒包上,松开 3 秒松开)
            float wrapTarget = s == Stage.BreathHold ? Mathf.SmoothStep(0f, 1f, view.controller.Breath) : 0f;
            wrap = Mathf.MoveTowards(wrap, wrapTarget, dt * 1.5f);
            BodyCapsule(out Vector3 baseC, out float radius, out float height);

            // 抚摸位置:探头 tag 或自动的海浪路线(绕身体缓慢移动,上下起伏)
            Vector3 tag = default;
            bool got = gelStage && view.tagSource != null && view.tagSource.TryGetPose(trackTagId, out tag, out _);
            if (got) { tagPoint = tag; lostTime = 0f; }
            else lostTime += dt;
            TagTracked = gelStage && lostTime <= view.tagLostGrace;
            // tag 是否在海草控制区域内(身体周围的圆柱范围);在外面时小丑鱼自己随机游动
            Vector3 regionPoint = ClampToRegion(tagPoint, baseC, radius, height);
            TagInRegion = TagTracked && (tagPoint - regionPoint).sqrMagnitude < regionMargin * regionMargin;
            TagRegionPoint = regionPoint;
            RegionCenter = baseC; RegionRadius = radius; RegionHeight = height;

            // 拨动海草的"交互者":有小丑鱼时就是小丑鱼(跟着 tag 或自己游);否则直接用 tag
            bool fishActive = gelStage && interactor && interactor.gameObject.activeInHierarchy;
            bool following = fishActive || TagTracked;
            Vector3 fishPos = fishActive ? interactor.position : Vector3.zero;
            strokeClock += dt * (.35f + .25f * gel);
            Vector3 auto = baseC + new Vector3(Mathf.Cos(strokeClock) * radius, height * (.55f + .25f * Mathf.Sin(strokeClock * 1.7f)), Mathf.Sin(strokeClock) * radius);
            Vector3 desiredStroke = fishActive ? ClampToRegion(interactor.position, baseC, radius, height)
                                  : TagTracked ? regionPoint : auto;
            strokePoint = Vector3.Lerp(strokePoint, desiredStroke, 1f - Mathf.Exp(-dt * (following ? 14f : 3f)));
            // 按压点:探头在身体表面上的投影;探头越贴近身体,按压越深。小丑鱼自己游动时只轻轻拨动
            Vector3 pressPoint = ClampToBody(strokePoint, baseC, radius, height);
            float pressScale = fishActive ? (TagInRegion ? 1f : wanderPress) : 1f;
            float press = following ? pressScale * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.05f, .45f, DistanceToBody(strokePoint, baseC, radius, height)))) : 0f;
            PressDepth = press;
            if (tagMarker)
            {
                // 2026-11-05 测试:小黄点 = 真实 tag 位置(未限制到控制区域),与小丑鱼同时显示;showTagMarker / 键盘 T 开关
                bool showMarker = showTagMarker && TagTracked;
                if (tagMarker.gameObject.activeSelf != showMarker) tagMarker.gameObject.SetActive(showMarker);
                tagMarker.position = interactor ? tagPoint : strokePoint;
            }

            Vector3 waveDir = new Vector3(.8f, 0f, .6f).normalized;
            Vector3 g = Vector3.up * buoyancy;
            Vector3 eyePos = eye ? eye.position : new Vector3(0f, 999f, 0f);
            int touching = 0, upper = 0;

            for (int st = 0; st < strandCount; st++)
            {
                int o = st * nodes;
                float rest = segLen[st] * Mathf.Max(.02f, reveal);
                // 海浪:波峰按根部方位依次经过,所以每一根被推的时机都不同
                float swell = Mathf.Sin(t * waveSpeed * 2f - wave[st] * 1.1f + phase[st] * .05f);
                float lap = Mathf.Max(0f, Mathf.Sin(t * 1.15f - wave[st] * 1.4f + phase[st] * .1f));   // 一波一波扑向身体
                // Tag 0 追踪时:根部离 tag 越近的海草越强地伸向 tag(大约半圈海草明显跟随)
                float tagPull = 0f;
                if (following)
                {
                    float d = new Vector2(root[st].x - strokePoint.x, root[st].z - strokePoint.z).magnitude;
                    tagPull = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(radius + .25f, radius + 1.1f, d));
                }

                for (int k = 1; k < nodes; k++)
                {
                    int i = o + k;
                    float u = k / (float)segments;
                    float tipW = Mathf.Pow(u, 1.3f);
                    Vector3 p = pos[i];
                    Vector3 vel = (p - prev[i]) * damping;

                    // 自己的水流:两组不同频率的正弦 + 沿长度的相位差 → 柔软的 S 形摆动
                    Vector3 own = new Vector3(
                        Mathf.Sin(t * freqA[st] + phase[st] + u * 2.6f),
                        0f,
                        Mathf.Cos(t * freqB[st] * .8f + phase[st] * 1.7f + u * 3.1f)) * (currentStrength * amp[st]);
                    own += facing[st] * (Mathf.Sin(t * freqB[st] + phase[st] * .3f + u * 4f) * .6f * amp[st]);
                    Vector3 acc = g * (1f - .2f * gel - .5f * wrap) + (own + waveDir * swell * waveStrength) * tipW * (1f - .6f * wrap);

                    // 场景4 吸气:沿身体螺旋缠绕(把节点拉到身体表面、并沿身体转一个角度)
                    if (wrap > .001f && u > .2f)
                    {
                        float yy = Mathf.Clamp(p.y, baseC.y, baseC.y + height);
                        Vector3 radial = new Vector3(p.x - baseC.x, 0f, p.z - baseC.z);
                        if (radial.sqrMagnitude < 1e-6f) radial = facing[st];
                        radial.Normalize();
                        Vector3 around = Quaternion.AngleAxis((st % 2 == 0 ? 1f : -1f) * 35f, Vector3.up) * radial;
                        Vector3 hug = new Vector3(baseC.x, yy + .08f, baseC.z) + around * radius * 1.02f;
                        acc += (hug - p) * wrapStrength * wrap * Mathf.InverseLerp(.2f, 1f, u);
                    }

                    // 涂耦合剂:上半段被拉向身体上的抚摸点,随浪一波一波贴上去
                    if (gel > .001f && u > .5f)
                    {
                        Vector3 target = strokePoint + offset[st] * radius * 2f;
                        Vector3 axisPt = new Vector3(baseC.x, Mathf.Clamp(p.y, baseC.y, baseC.y + height), baseC.z);
                        Vector3 towardBody = (axisPt - p);
                        float upperW = Mathf.InverseLerp(.5f, 1f, u);
                        if (following)
                        {
                            // 跟随探头 / 小丑鱼:靠近的海草叶尖追按压点(不受海浪节奏影响),其余海草照常随浪轻抚
                            Vector3 tagTarget = pressPoint + offset[st] * .4f;
                            acc += ((tagTarget - p) * tagFollowStrength * pressScale * (fishActive ? .5f : 1f) * tagPull
                                    + ((target - p) * strokeStrength * .35f * lap + towardBody * 1.2f * lean[st]) * (1f - tagPull)) * gel * upperW;
                        }
                        else
                            acc += ((target - p) * strokeStrength * (.35f + .65f * lap) + towardBody * 1.2f * lean[st]) * gel * upperW;
                    }

                    // 按压:探头按下去的位置,海草被压平贴到身体上,并从按压点向四周摊开(像手指按进柔软的海草)
                    if (press > .001f && u > .2f)
                    {
                        Vector3 fromPress = p - pressPoint;
                        float d = fromPress.magnitude;
                        if (d < pressRadius)
                        {
                            float w = (1f - d / pressRadius) * press * gel;
                            Vector3 axisPt = new Vector3(baseC.x, Mathf.Clamp(p.y, baseC.y, baseC.y + height), baseC.z);
                            Vector3 normal = (p - axisPt); normal = normal.sqrMagnitude < 1e-6f ? Vector3.forward : normal.normalized;
                            Vector3 spread = fromPress - normal * Vector3.Dot(fromPress, normal);   // 沿身体表面向外
                            spread = spread.sqrMagnitude < 1e-6f ? Vector3.Cross(normal, Vector3.up) : spread.normalized;
                            acc += (spread * pressStrength - normal * pressStrength * .8f) * w;
                        }
                    }
                    // 小丑鱼穿过海草:身边的叶片被拨开(软推开),所以看得到鱼在海草里游
                    if (fishActive)
                    {
                        Vector3 fd = p - fishPos;
                        float fdist = fd.magnitude;
                        if (fdist < fishClearRadius * 1.8f && fdist > 1e-4f)
                            acc += fd / fdist * (1f - fdist / (fishClearRadius * 1.8f)) * fishPushStrength;
                    }
                    prev[i] = p;
                    pos[i] = p + vel + acc * (dt * dt);
                }

                // 约束:长度、轻微抗弯、身体碰撞、眼睛避让、地面
                for (int it = 0; it < iterations; it++)
                {
                    pos[o] = root[st];
                    for (int k = 1; k < nodes; k++)
                    {
                        int i = o + k;
                        Vector3 d = pos[i] - pos[i - 1];
                        float len = d.magnitude;
                        if (len > 1e-5f)
                        {
                            Vector3 corr = d * ((len - rest) / len);
                            if (k == 1) pos[i] -= corr;
                            else { pos[i] -= corr * .5f; pos[i - 1] += corr * .5f; }
                        }
                        if (k >= 2)
                        {
                            Vector3 d2 = pos[i] - pos[i - 2];
                            float l2 = d2.magnitude, r2 = rest * 1.92f;
                            if (l2 < r2 && l2 > 1e-5f) pos[i] += d2 / l2 * ((r2 - l2) * bendStiffness);   // 柔软但不塌的抗弯
                        }
                    }
                    for (int k = 1; k < nodes; k++)
                    {
                        int i = o + k;
                        pos[i] = PushOutOfBody(pos[i], baseC, radius, height);
                        if (fishActive)
                        {
                            Vector3 fe = pos[i] - fishPos;
                            if (fe.sqrMagnitude < fishClearRadius * fishClearRadius && fe.sqrMagnitude > 1e-8f) pos[i] = fishPos + fe.normalized * fishClearRadius;
                        }
                        Vector3 e = pos[i] - eyePos;
                        if (e.sqrMagnitude < .3f * .3f) pos[i] = eyePos + e.normalized * .3f;
                        if (pos[i].y < baseC.y + .01f) pos[i].y = baseC.y + .01f;
                    }
                }

                for (int k = nodes / 2; k < nodes; k++)
                {
                    upper++;
                    if (DistanceToBody(pos[o + k], baseC, radius, height) < .06f) touching++;
                }
            }
            ContactRatio = upper > 0 ? touching / (float)upper : 0f;
            BuildMesh(reveal);
        }

        /// <summary>Tag 0 的位置只限制在身体周围一个圆柱范围内(半径 + 0.6m,高度略超身体)。</summary>
        static Vector3 ClampToRegion(Vector3 p, Vector3 baseC, float radius, float height)
        {
            Vector3 h = new Vector3(p.x - baseC.x, 0f, p.z - baseC.z);
            float maxR = radius + .6f;
            if (h.magnitude > maxR) h = h.normalized * maxR;
            float y = Mathf.Clamp(p.y, baseC.y + height * .15f, baseC.y + height * 1.1f);
            return new Vector3(baseC.x + h.x, y, baseC.z + h.z);
        }

        Vector3 ClampToBody(Vector3 p, Vector3 baseC, float radius, float height)
        {
            Vector3 h = new Vector3(p.x - baseC.x, 0f, p.z - baseC.z);
            if (h.sqrMagnitude < 1e-4f) h = Vector3.forward * radius;
            h = h.normalized * radius;
            float y = Mathf.Clamp(p.y, baseC.y + height * .2f, baseC.y + height * .95f);
            return new Vector3(baseC.x + h.x, y, baseC.z + h.z);
        }

        static float DistanceToBody(Vector3 p, Vector3 baseC, float radius, float height)
        {
            float y = Mathf.Clamp(p.y, baseC.y, baseC.y + height);
            Vector3 axis = new Vector3(baseC.x, y, baseC.z);
            return (p - axis).magnitude - radius;
        }

        static Vector3 PushOutOfBody(Vector3 p, Vector3 baseC, float radius, float height)
        {
            float y = Mathf.Clamp(p.y, baseC.y, baseC.y + height);
            Vector3 axis = new Vector3(baseC.x, y, baseC.z);
            Vector3 d = p - axis;
            float m = d.magnitude;
            if (m >= radius) return p;
            if (m < 1e-5f) d = Vector3.forward; else d /= m;
            return axis + d * radius;
        }

        void BuildMesh(float reveal)
        {
            for (int s = 0; s < strandCount; s++)
            {
                int o = s * nodes;
                for (int k = 0; k < nodes; k++)
                {
                    int i = o + k;
                    float u = k / (float)segments;
                    Vector3 tangent = pos[Mathf.Min(i + 1, o + segments)] - pos[Mathf.Max(i - 1, o)];
                    if (tangent.sqrMagnitude < 1e-8f) tangent = Vector3.up;
                    tangent.Normalize();
                    // 叶片沿长度轻微扭转,带出柔软的翻折感
                    Vector3 face = Quaternion.AngleAxis(twist[s] * u, Vector3.up) * facing[s];
                    Vector3 side = Vector3.Cross(tangent, face);
                    if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(tangent, Vector3.right);
                    side.Normalize();
                    // 叶形:根部窄、中段最宽、叶尖收尖
                    float w = width[s] * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.Lerp(.18f, 1f, u) * Mathf.PI)), .55f) * Mathf.Max(.02f, reveal);
                    int v = i * 2;
                    verts[v] = pos[i] - side * w;
                    verts[v + 1] = pos[i] + side * w;
                    Vector3 n = Vector3.Cross(side, tangent).normalized;
                    normals[v] = normals[v + 1] = n;
                }
            }
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.RecalculateBounds();
        }

        void OnDestroy() { if (mesh) Destroy(mesh); }
    }
}
