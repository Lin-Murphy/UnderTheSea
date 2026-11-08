using UnityEngine;

namespace UnderTheSea
{
    /// <summary>
    /// 步骤 1 的占位表现层:根据当前 Stage 驱动场景里的 placeholder 物体。
    /// 只做"状态是否正确"的演示,不做最终美术与真实追踪。
    /// - 小鱼跟随 / 水草抚摸 / 水母握手 目前都是用假的运动代替,后续步骤替换为 AprilTag 驱动。
    /// - 参与者本身就是主角珊瑚(第一人称),不再有镜像珊瑚。
    /// </summary>
    public class OceanStageView : MonoBehaviour
    {
        [Header("Imported reef artwork")]
        public ReefArtDriver importedArt;
        public ReefFishSchool fishSchool;         // demo1 的鱼群(近处/远处)+ 场景3 躲进珊瑚枝杈
        public SoftSeaweedStrands softSeaweed;    // 场景2 柔软、每根独立运动的海草
        public float sunDayIntensity = 1f;

        [Header("Refs")]
        public ExperienceController controller;
        public Camera cam;
        public Light sun;
        public GameObject underwaterRoot;
        public Transform heroCoral;
        public Transform bellyPoint;
        public TagPoseSource tagSource;           // AprilTag 位姿来源(编辑器用鼠标模拟,真机用检测器)
        public Transform probeMarker;             // 显示探头 tag 当前位置的小方块(调试可视化)
        public Vector3 bellyHalfExtents = new Vector3(0.45f, 0.15f, 0.35f);   // 肚子区域范围,tag 位置会被限制在此内
        public float tagLostGrace = 0.4f;         // tag 丢失多久后小鱼放弃跟随(秒)
        public Transform strokeSeaweedRoot;       // 场景2 抚摸身体的水草(跟随 Tag 0)
        public Transform handPoint;               // 患者"手"的位置:水母触手没有 tag 时伸向这里
        public Transform[] handTentacle;          // 水母握手触手(分节,跟随 Tag 2)
        public Vector3 handHalfExtents = new Vector3(0.7f, 0.3f, 0.55f);   // 触手可达范围(比肚子区域大)
        public Transform[] ambientSeaweed;
        public Transform[] nearFish;
        public Transform[] schoolFish;
        public Transform[] interactFish;          // 会跟随探头 / 回到体内的小鱼
        public Transform[] jellies;
        public Renderer[] jellyRenderers;

        [Header("AR↔VR 转场(步骤5)")]
        public PassthroughBridge passthrough;     // 真机上的 Passthrough;编辑器里为空/不可用,用灰色占位代表现实
        public Transform ground;
        public Transform[] decoCorals;
        public Transform[] transitionFish;        // 转场鱼群(不在 underwaterRoot 下,现实空间里也可见)
        public Color realityColor = new Color(0.18f, 0.18f, 0.20f);   // 编辑器里代表现实
        [Tooltip("进入场景:鱼群出现后多久开始散去(秒)")] public float enterFishHold = 7f;
        [Tooltip("进入/退出:鱼群出现后多久开始显现/退去 VR 场景(秒)")] public float vrStartDelay = 2.5f;
        [Tooltip("VR 场景完全显现/退去所需时间(秒)")] public float vrRevealSeconds = 6f;
        [Tooltip("鱼群聚拢/散去所需时间(秒)")] public float fishFadeSeconds = 2.5f;

        [Header("Look")]
        public Color waterDay = new Color(0.25f, 0.65f, 0.80f);
        public Color waterNight = new Color(0.02f, 0.04f, 0.14f);
        public Color ambientDay = new Color(0.55f, 0.65f, 0.75f);
        public Color ambientNight = new Color(0.05f, 0.07f, 0.16f);
        public Color jellyColor = new Color(0.5f, 0.8f, 1f);
        public Vector3 jellyHandPos = new Vector3(0.9f, 1.0f, 0.7f);
        [Tooltip("天黑程度:0 = 白天,1 = 原来的全黑;用户要求只到白天 50% 的暗度")]
        [Range(0f, 1f)] public float nightDarkness = .5f;

        float _dark, _glow = 0.12f, _breath = 1f, _strokeAmt, _swayAmp = 5f;
        Vector3 _heroBase;
        Vector3[] _interactScale;
        Vector3[] _jellyHome, _jellyPos, _interactPos;
        MaterialPropertyBlock _mpb;
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        void Start()
        {
            _mpb = new MaterialPropertyBlock();
            _heroBase = heroCoral.localScale;

            if (importedArt != null) importedArt.Initialize(this);
            if (fishSchool != null) fishSchool.Initialize(this);
            if (softSeaweed != null) softSeaweed.Initialize(this);

            _jellyHome = new Vector3[jellies.Length];
            _jellyPos = new Vector3[jellies.Length];
            for (int i = 0; i < jellies.Length; i++)
            {
                _jellyHome[i] = jellies[i].position;
                _jellyPos[i] = FarFrom(_jellyHome[i]);
                jellies[i].position = _jellyPos[i];
            }

            if (cam != null) cam.clearFlags = CameraClearFlags.SolidColor;
            BuildReveal();
            _weedPos = bellyPoint.position;
            _tentTip = handPoint != null ? handPoint.position : bellyPoint.position;

            _interactPos = new Vector3[interactFish.Length];
            _interactScale = new Vector3[interactFish.Length];
            for (int i = 0; i < interactFish.Length; i++)
            {
                _interactPos[i] = interactFish[i].position;
                _interactScale[i] = interactFish[i].localScale;
            }

            ApplyVisibility();
            controller.StageChanged += HandleStageChanged;
        }

        void HandleStageChanged(Stage previous, Stage next) => ApplyVisibility();

        void OnDestroy()
        {
            if (controller != null) controller.StageChanged -= HandleStageChanged;
        }

        void ApplyVisibility()
        {
            var s = controller.Current;
            if (probeMarker != null) probeMarker.gameObject.SetActive(false);
        }

        // ---------- AR↔VR 转场(步骤5)----------
        // _vr:   0 = 完全现实(AR),1 = 完全沉浸 VR。环境从近到远逐步"长出来"。
        // _fishAmt: 转场鱼群的出现程度。进入:鱼群先出现→VR 渐显→鱼群散去;退出:鱼群聚拢→VR 渐隐→鱼群散去回到现实。
        float _vr, _fishAmt;

        /// <summary>VR 显现程度 0..1(0 = 完全现实)。</summary>
        public float VrAmount => _vr;

        /// <summary>仅供编辑器自检:不进 Play 模式时直接设定 VR 显现程度。</summary>
        public void ForceVrAmountForVerification(float v) => _vr = Mathf.Clamp01(v);
        /// <summary>仅供编辑器自检:直接推进转场鱼一步。</summary>
        public void StepTransitionFishForVerification(bool gathering, float fishAmount, float dt, float t) { _fishAmt = fishAmount; UpdateTransitionFish(gathering, dt, t); }
        public Vector3 TransitionFishVelocity(int i) => _tVel != null ? _tVel[i] : Vector3.zero;

        class RevItem { public Transform t; public Vector3 baseScale; public float thr; public bool groundLike; }
        readonly System.Collections.Generic.List<RevItem> _rev = new System.Collections.Generic.List<RevItem>();
        Vector3[] _transBase;

        void BuildReveal()
        {
            if (ground != null) _rev.Add(new RevItem { t = ground, baseScale = ground.localScale, thr = 0f, groundLike = true });
            if (decoCorals != null)
                foreach (var c in decoCorals) AddReveal(c, Mathf.Clamp01(c.position.magnitude / 12f) * 0.6f);
            foreach (var w in ambientSeaweed) AddReveal(w, Mathf.Clamp01(w.position.magnitude / 12f) * 0.6f);
            if (fishSchool == null)   // ReefFishSchool 自己按 Reveal() 处理鱼的显现缩放
            {
                for (int i = 0; i < nearFish.Length; i++) AddReveal(nearFish[i], 0.3f + 0.003f * i);
                for (int i = 0; i < schoolFish.Length; i++) AddReveal(schoolFish[i], 0.5f + 0.003f * i);
            }

            if (transitionFish != null)
            {
                int n = transitionFish.Length;
                _transBase = new Vector3[n];
                _tPos = new Vector3[n]; _tVel = new Vector3[n]; _tVis = new float[n]; _tActive = new bool[n];
                for (int i = 0; i < n; i++) _transBase[i] = transitionFish[i].localScale;
            }
        }

        void AddReveal(Transform t, float thr)
        {
            _rev.Add(new RevItem { t = t, baseScale = t.localScale, thr = thr });
        }

        /// <summary>环境物体在 _vr 到达阈值后逐步长出;thr 越大越晚出现。</summary>
        public float Reveal(float thr) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(thr, thr + 0.4f, _vr));

        void UpdateTransition(Stage s, float dt, float t)
        {
            float T = controller.TimeInStage;
            float vrTarget, fishTarget;
            switch (s)
            {
                case Stage.Standby:
                case Stage.Finished: vrTarget = 0f; fishTarget = 0f; break;
                case Stage.Enter:
                    vrTarget = T >= vrStartDelay ? 1f : 0f;
                    fishTarget = T < enterFishHold ? 1f : 0f;
                    break;
                case Stage.Exit:
                    vrTarget = T >= vrStartDelay ? 0f : 1f;
                    fishTarget = (_vr <= 0.02f && T >= vrStartDelay + 0.5f) ? 0f : 1f;
                    break;
                default: vrTarget = 1f; fishTarget = 0f; break;
            }
            // 随时重置(B 键)回到 Standby 时,VR 快速退去(约 1.5 秒)
            float vrRate = s == Stage.Standby ? 4f : 1f;
            _vr = Mathf.MoveTowards(_vr, vrTarget, dt * vrRate / Mathf.Max(0.01f, vrRevealSeconds));
            _fishAmt = Mathf.MoveTowards(_fishAmt, fishTarget, dt / Mathf.Max(0.01f, fishFadeSeconds));

            // 退出转场彻底完成(VR 退尽、鱼群散去)→ 进入 Finished,3 秒后回到 Start 按钮
            if (s == Stage.Exit && T >= vrStartDelay + 0.5f && _vr <= 0.001f && _fishAmt <= 0.001f)
                controller.Next();

            underwaterRoot.SetActive(_vr > 0.001f);

            foreach (var it in _rev)
            {
                float f = Reveal(it.thr);
                it.t.localScale = it.groundLike
                    ? new Vector3(it.baseScale.x * Mathf.Max(0.02f, f), it.baseScale.y, it.baseScale.z * Mathf.Max(0.02f, f))
                    : it.baseScale * f;
            }

            UpdateTransitionFish(fishTarget > .5f, dt, t);
        }

        // 转场鱼:每条鱼是一个有速度、会转弯的个体。聚拢时从远处(7–11m,前方大半圈)游过来,
        // 以有限的转向速度弯着靠近,最后跟上绕圈的位置;散去时掉头向外游走并淡出。不再整体横移/缩放半径。
        Vector3[] _tPos, _tVel;
        float[] _tVis;
        bool[] _tActive;
        [Header("转场鱼游动")]
        public float transitionSwimSpeed = 2.4f;
        public float transitionTurnRate = 2.2f;   // 弧度/秒

        /// <summary>当前可见的转场鱼数量(自检用)。</summary>
        public int TransitionFishVisible { get; private set; }

        void UpdateTransitionFish(bool gathering, float dt, float t)
        {
            if (transitionFish == null || cam == null || _tPos == null) return;
            Vector3 fwd = FlatForward();
            // 沙丁鱼风暴:以"我"为中心(眼睛稍下方),鱼群从四面八方螺旋游来,绕着我转成一个松散的环,不挡住视线
            Vector3 c = cam.transform.position + Vector3.down * .3f;
            int visible = 0;
            for (int i = 0; i < transitionFish.Length; i++)
            {
                float k = (i * 0.6180339f) % 1f;
                var f = transitionFish[i];
                Vector3 desired;
                Vector3 radial = _tPos[i] - c; radial.y = 0f;
                Vector3 rdir = radial.sqrMagnitude < 1e-4f ? fwd : radial.normalized;
                Vector3 tangent = Vector3.Cross(rdir, Vector3.up) * StormSpin;   // 整个鱼群同一个旋转方向
                if (gathering)
                {
                    if (!_tActive[i])
                    {
                        // 从 360° 四面八方、9–13m 外出生,高低不同;初始方向是"绕着我"的切向 + 一点点向内,所以是螺旋地游来
                        float ang = i * 137.508f + Mathf.Sin(i * 12.9898f) * 20f;
                        Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * fwd;
                        _tPos[i] = c + dir * (9f + 4f * ((i * .37f) % 1f)) + Vector3.up * ((k - .4f) * 3f);
                        Vector3 tan0 = Vector3.Cross(dir, Vector3.up) * StormSpin;
                        _tVel[i] = (tan0 * .85f - dir * .5f).normalized * transitionSwimSpeed;
                        _tVis[i] = 0f; _tActive[i] = true;
                        f.position = _tPos[i];
                        f.rotation = Quaternion.LookRotation(_tVel[i]);   // 出生时头就朝游动方向(不是朝向我)
                        radial = _tPos[i] - c; radial.y = 0f; rdir = radial.normalized;
                        tangent = Vector3.Cross(rdir, Vector3.up) * StormSpin;
                    }
                    Vector3 target = StormPos(c, i, k, t);
                    Vector3 targetVel = (StormPos(c, i, k, t + .05f) - target) / .05f;
                    float ringR = StormRadius(k);
                    float r = radial.magnitude;
                    if (r > ringR + 1.5f)
                        // 远:螺旋向内(大部分是绕行的切向速度,少部分向内)
                        desired = (tangent * .8f - rdir * .6f + Vector3.up * Mathf.Clamp(target.y - _tPos[i].y, -1f, 1f) * .3f).normalized * transitionSwimSpeed;
                    else
                        // 近:跟上自己在风暴环上的位置
                        desired = targetVel + (target - _tPos[i]) * 1.6f;
                    _tVis[i] = Mathf.MoveTowards(_tVis[i], 1f, dt / 1.2f);
                }
                else if (_tActive[i])
                {
                    // 散去:继续绕行的同时螺旋向外游走
                    desired = (tangent * .6f + rdir * .8f).normalized * transitionSwimSpeed * 1.1f;
                    float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_fishAmt * 1.6f - k * .6f));
                    _tVis[i] = Mathf.Min(_tVis[i], fade);
                    if (_tVis[i] <= .01f) _tActive[i] = false;
                }
                else { if (f.gameObject.activeSelf) f.gameObject.SetActive(false); continue; }

                _tVel[i] = Vector3.RotateTowards(_tVel[i], desired, transitionTurnRate * dt, 4f * dt);
                _tPos[i] += _tVel[i] * dt;
                // 不贴到脸上:离眼睛至少 1.6m
                Vector3 fromEye = _tPos[i] - cam.transform.position;
                if (fromEye.sqrMagnitude < 1.6f * 1.6f) _tPos[i] = cam.transform.position + fromEye.normalized * 1.6f;
                bool show = _tVis[i] > .01f;
                if (f.gameObject.activeSelf != show) f.gameObject.SetActive(show);
                if (!show) continue;
                visible++;
                f.position = _tPos[i];
                if (_tVel[i].sqrMagnitude > 1e-6f)
                    f.rotation = Quaternion.Slerp(f.rotation, Quaternion.LookRotation(_tVel[i]), 1f - Mathf.Exp(-dt * 8f));
                f.localScale = _transBase[i] * _tVis[i];
            }
            TransitionFishVisible = visible;
        }

        /// <summary>相机的水平朝前方向(珊瑚视角会低头看自己,转场鱼群/按钮仍放在正前方)。</summary>
        public Vector3 FlatForward()
        {
            Vector3 f = cam.transform.forward; f.y = 0f;
            return f.sqrMagnitude < 1e-4f ? Vector3.forward : f.normalized;
        }

        const float StormSpin = 1f;   // 风暴旋转方向
        static float StormRadius(float k) => 2.3f + 1.9f * k;

        /// <summary>风暴环上的位置:半径 2.3–4.2m、高度在眼睛上下约 ±1.3m 分层,同向绕行,速度略有差异。</summary>
        static Vector3 StormPos(Vector3 c, int i, float k, float t)
        {
            float r = StormRadius(k);
            float w = (1.15f - .25f * k) / r * 1.6f;              // 内圈转得快一点
            float a = i * 2.39996f + t * w * StormSpin;
            float h = (((i * .7548f) % 1f) - .45f) * 2.6f + Mathf.Sin(t * .8f + i) * .2f;
            return c + new Vector3(Mathf.Cos(a) * r, h, Mathf.Sin(a) * r);
        }

        static Vector3 SwirlPos(Vector3 c, int i, float k, float t, float expand)
        {
            float a = t * (0.9f + 0.5f * k) * (i % 2 == 0 ? 1f : -1f) + i * 2.4f;
            float r = (0.5f + k * 0.9f) * expand;
            float h = (k - 0.5f) * 1.2f + Mathf.Sin(t * 1.7f + i) * 0.1f;
            return c + new Vector3(Mathf.Cos(a) * r, h, Mathf.Sin(a) * r);
        }

        void Update()
        {
            var s = controller.Current;
            float dt = Time.deltaTime;
            float t = Time.time;

            UpdateTransition(s, dt, t);
            UpdateLighting(s, dt);
            UpdateCorals(s, dt, t);
            UpdateSeaweed(s, dt, t);
            UpdateProbeTag(s, dt);
            UpdateFish(s, dt, t);
            UpdateJellies(s, dt, t);
            UpdateHandTentacle(s, dt, t);
        }

        // ---------- 水母握手触手(Tag 2)----------
        float _tentAmt, _tentLostTime;
        bool _tentTracked;
        Vector3 _tentTip;

        void UpdateHandTentacle(Stage s, float dt, float t)
        {
            if (handTentacle == null || handTentacle.Length < 2 || jellies.Length == 0) return;

            bool finale = s == Stage.Finale;
            Vector3 jelly = jellies[0].position;
            bool arrived = finale && (jelly - jellyHandPos).sqrMagnitude < 0.5f * 0.5f;

            // 触手伸出 / 收回
            _tentAmt = Mathf.MoveTowards(_tentAmt, arrived ? 1f : 0f, dt * 0.6f);

            // Tag 2 位置(丢失时伸向患者的手)
            Vector3 p = default;
            bool got = arrived && tagSource != null && tagSource.TryGetPose(TagIds.Jelly, out p, out _);
            Vector3 rest = handPoint != null ? handPoint.position : bellyPoint.position;
            Vector3 target = rest;
            if (got)
            {
                _tentLostTime = 0f; _tentTracked = true;
                target = ClampTo(p, bellyPoint.position, handHalfExtents);
            }
            else
            {
                _tentLostTime += dt;
                if (_tentLostTime > tagLostGrace) _tentTracked = false;
                if (_tentTracked) target = _tentTip;
            }
            _tentTip = Vector3.Lerp(_tentTip, target, 1f - Exp(dt * (got ? 8f : 2f)));

            // 触手:从水母伞下方到 tip 的一条下垂曲线,分节摆放
            Vector3 b = jelly + Vector3.down * 0.5f;
            Vector3 tip = Vector3.Lerp(b + Vector3.down * 0.4f, _tentTip, _tentAmt);
            float dist = (tip - b).magnitude;
            int n = handTentacle.Length;
            bool show = _tentAmt > 0.01f;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1);
                Vector3 pt = Vector3.Lerp(b, tip, u)
                             + Vector3.down * (Mathf.Sin(u * Mathf.PI) * 0.12f * Mathf.Min(1f, dist))
                             + new Vector3(Mathf.Sin(t * 2f + u * 5f), 0, Mathf.Cos(t * 1.7f + u * 5f)) * 0.02f * u;
                float size = Mathf.Lerp(0.07f, 0.035f, u);
                var seg = handTentacle[i];
                seg.gameObject.SetActive(show);
                seg.position = pt;
                seg.localScale = Vector3.one * size;
            }
        }

        static float Exp(float x) => Mathf.Exp(-x);

        static Vector3 ClampTo(Vector3 p, Vector3 c, Vector3 h)
        {
            return new Vector3(
                Mathf.Clamp(p.x, c.x - h.x, c.x + h.x),
                Mathf.Clamp(p.y, c.y - h.y, c.y + h.y),
                Mathf.Clamp(p.z, c.z - h.z, c.z + h.z));
        }

        // ---------- 探头 AprilTag(tag 1)----------
        bool _probeTracked, _probeFollow;
        /// <summary>探头小鱼正在跟随(真实 Tag 1 或默认路线)。</summary>
        public bool ProbeFollowing => _probeFollow;
        /// <summary>主角珊瑚当前相对原始大小的倍数(呼吸时到 1.35)。</summary>
        public float HeroGrowth => _breath;
        Vector3 _probePos;
        float _probeLostTime;

        void UpdateProbeTag(Stage s, float dt)
        {
            // 探头 Tag 1 现在驱动场景3 的海草(SoftSeaweedStrands);有 ReefFishSchool 时小鱼不再跟随探头
            bool active = fishSchool == null && (s == Stage.Probe || s == Stage.DuskTransition);
            Vector3 p = default;
            bool got = active && tagSource != null && tagSource.TryGetPose(TagIds.Fish, out p, out _);

            if (got)
            {
                _probePos = ClampToBelly(p);
                _probeLostTime = 0f;
                _probeTracked = true;
            }
            else
            {
                _probeLostTime += dt;
                if (!active || _probeLostTime > tagLostGrace) _probeTracked = false;
            }

            // Tag 1 追踪不到时播放默认动画:小鱼群沿一条缓慢的"探头路线"在身体上来回游
            if (active && !_probeTracked)
            {
                float t = Time.time;
                Vector3 auto = ClampToBelly(bellyPoint.position + new Vector3(Mathf.Sin(t * .55f) * .35f, .05f + Mathf.Sin(t * 1.1f) * .08f, Mathf.Cos(t * .42f) * .28f));
                _probePos = Vector3.Lerp(_probePos == Vector3.zero ? auto : _probePos, auto, 1f - Mathf.Exp(-dt * 2f));
            }
            _probeFollow = active;

            if (probeMarker != null)
            {
                probeMarker.gameObject.SetActive(_probeTracked);
                if (_probeTracked) probeMarker.position = _probePos;
            }
        }

        Vector3 ClampToBelly(Vector3 p)
        {
            Vector3 c = bellyPoint.position;
            return new Vector3(
                Mathf.Clamp(p.x, c.x - bellyHalfExtents.x, c.x + bellyHalfExtents.x),
                Mathf.Clamp(p.y, c.y - bellyHalfExtents.y, c.y + bellyHalfExtents.y),
                Mathf.Clamp(p.z, c.z - bellyHalfExtents.z, c.z + bellyHalfExtents.z));
        }

        // ---------- 天黑 / 天亮 ----------
        void UpdateLighting(Stage s, float dt)
        {
            // 天黑只到白天的 50% 暗度,过渡结束后保持在这个暗度直到呼气
            float darkTarget = (s == Stage.DuskTransition || s == Stage.BreathHold) ? nightDarkness : 0f;
            _dark = Mathf.MoveTowards(_dark, darkTarget, dt * (importedArt != null ? 1f / 4.5f : 0.3f));

            if (sun != null) sun.intensity = Mathf.Lerp(sunDayIntensity, 0.16f, _dark);
            RenderSettings.ambientLight = Color.Lerp(ambientDay, ambientNight, _dark);

            Color water = Color.Lerp(waterDay, waterNight, _dark);
            if (cam != null)
            {
                // 真机:背景用预乘 alpha(颜色 × _vr, alpha = _vr),Standby 时为 (0,0,0,0),现实画面不会被染色;
                // 编辑器:灰色(现实占位)→ 水色
                cam.backgroundColor = (passthrough != null && passthrough.IsLive)
                    ? new Color(water.r * _vr, water.g * _vr, water.b * _vr, _vr)
                    : Color.Lerp(realityColor, water, _vr);
            }
            RenderSettings.fogColor = water;
            if (importedArt != null) importedArt.ApplyLighting(_dark);
        }

        // ---------- 珊瑚:主角(参与者自己)----------
        void UpdateCorals(Stage s, float dt, float t)
        {
            // 场景4 吸气(按住扳机)时主角珊瑚变大到 1.35 倍,松开 3 秒缩回;与水母放大、尖端发光同一个呼吸值
            _breath = 1f + .35f * Mathf.SmoothStep(0f, 1f, controller.Breath);
            float pulse = 1f + Mathf.Sin(t * 1.2f) * 0.015f;
            float k = _breath * pulse * Mathf.Max(0.001f, Reveal(0f));   // 随 VR 显现长出
            heroCoral.localScale = _heroBase * k;
        }

        // ---------- 水草 ----------
        void UpdateSeaweed(Stage s, float dt, float t)
        {
            if (softSeaweed != null) { softSeaweed.Tick(s, dt); return; }
            if (importedArt != null && importedArt.gel != null) { importedArt.UpdateGel(s); return; }
            _swayAmp = Mathf.Lerp(_swayAmp, s == Stage.ApplyGel ? 12f : 5f, 1f - Mathf.Exp(-dt * 2f));
            for (int i = 0; i < ambientSeaweed.Length; i++)
                ambientSeaweed[i].localRotation = Quaternion.Euler(
                    Mathf.Sin(t * 0.9f + i) * _swayAmp, 0f, Mathf.Cos(t * 0.7f + i * 1.3f) * _swayAmp);

            // 场景2:水草跟随助手手持的 Tag 0 抚摸身体;tag 丢失时退回自动来回抚摸
            _strokeAmt = Mathf.MoveTowards(_strokeAmt, s == Stage.ApplyGel ? 1f : 0f, dt * 1.5f);

            Vector3 p = default;
            bool got = s == Stage.ApplyGel && tagSource != null && tagSource.TryGetPose(TagIds.Seaweed, out p, out _);
            if (got) { _weedTarget = ClampToBelly(p); _weedLostTime = 0f; _weedTracked = true; }
            else
            {
                _weedLostTime += dt;
                if (s != Stage.ApplyGel || _weedLostTime > tagLostGrace) _weedTracked = false;
            }

            Vector3 b = bellyPoint.position;
            Vector3 auto = b + new Vector3(Mathf.Sin(t * 0.8f) * 0.35f, 0.05f, Mathf.Cos(t * 1.3f) * 0.25f);
            Vector3 target = _weedTracked ? _weedTarget : auto;
            _weedPos = Vector3.Lerp(_weedPos, target, 1f - Exp(dt * (_weedTracked ? 8f : 2f)));

            if (strokeSeaweedRoot != null)
            {
                bool show = _strokeAmt > 0.01f;
                strokeSeaweedRoot.gameObject.SetActive(show);
                strokeSeaweedRoot.localScale = Vector3.one * _strokeAmt;
                strokeSeaweedRoot.position = _weedPos;
                strokeSeaweedRoot.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 2f) * 15f);
            }
        }

        // 水草 Tag 0 状态
        bool _weedTracked;
        Vector3 _weedTarget, _weedPos;
        float _weedLostTime;

        // ---------- 小鱼 ----------
        void UpdateFish(Stage s, float dt, float t)
        {
            if (fishSchool != null) fishSchool.Tick(s, dt, t);
            else
            {
                for (int i = 0; i < nearFish.Length; i++)
                    Orbit(nearFish[i], i, t, 1.5f, 3f, 0.4f, 0.8f, 0.5f);
                for (int i = 0; i < schoolFish.Length; i++)
                    Orbit(schoolFish[i], i, t, 14f, 8f, 1.5f, 4f, 0.15f);
            }

            if (fishSchool == null) for (int i = 0; i < interactFish.Length; i++)   // 有 ReefFishSchool 时,小鱼由它负责(场景2/天黑躲进我体内)
            {
                var f = interactFish[i];
                Vector3 target;
                float scaleTarget = 1f;
                float speed = 1.5f;
                float ph = i * 1.7f;

                if (_probeFollow)   // 跟随 Tag 1;追踪不到时跟随默认路线
                {
                    // 围绕探头位置游动
                    target = _probePos + new Vector3(Mathf.Cos(t * 1.5f + ph), 0.1f * Mathf.Sin(t * 2f + ph), Mathf.Sin(t * 1.5f + ph)) * 0.18f;
                    speed = 3f;
                }
                else if (s == Stage.BreathHold)
                {
                    target = heroCoral.position + Vector3.up * 0.25f;   // 回到体内
                    scaleTarget = 0f;
                    speed = 2f;
                }
                else
                {
                    target = OrbitPos(i, t, 1.5f, 3f, 0.4f, 0.8f, 0.5f);
                }

                Vector3 prev = _interactPos[i];
                _interactPos[i] = Vector3.Lerp(_interactPos[i], target, 1f - Mathf.Exp(-dt * speed));
                f.position = _interactPos[i];
                Vector3 v = _interactPos[i] - prev;
                if (v.sqrMagnitude > 1e-8f) f.rotation = Quaternion.Slerp(f.rotation, Quaternion.LookRotation(v), dt * 8f);
                float currentScale = f.localScale.x / Mathf.Max(.001f, _interactScale[i].x);
                float sc = Mathf.Lerp(currentScale, scaleTarget * Reveal(0.3f), 1f - Mathf.Exp(-dt * 3f));
                f.localScale = _interactScale[i] * sc;
            }
        }

        public static Vector3 OrbitPos(int i, float t, float rBase, float rSpan, float hBase, float hSpan, float speed, Vector3? offset = null)
        {
            float k = (i * 0.6180339f) % 1f;
            float r = rBase + k * rSpan;
            float a = t * speed * (0.6f + k) * (i % 2 == 0 ? 1f : -1f) + i * 2.4f;
            float h = hBase + (((i * 0.37f) % 1f) * hSpan) + Mathf.Sin(t * 1.3f + i) * 0.15f;
            return new Vector3(Mathf.Cos(a) * r, h, Mathf.Sin(a) * r) + (offset ?? Vector3.zero);
        }

        static void Orbit(Transform f, int i, float t, float rBase, float rSpan, float hBase, float hSpan, float speed, Vector3? offset = null)
        {
            Vector3 p = OrbitPos(i, t, rBase, rSpan, hBase, hSpan, speed, offset);
            Vector3 n = OrbitPos(i, t + 0.05f, rBase, rSpan, hBase, hSpan, speed, offset);
            f.position = p;
            if ((n - p).sqrMagnitude > 1e-8f) f.rotation = Quaternion.LookRotation(n - p);
        }

        // ---------- 水母 ----------
        void UpdateJellies(Stage s, float dt, float t)
        {
            if (importedArt != null) { importedArt.UpdateJellies(s, dt, t, controller.Breath); return; }
            for (int i = 0; i < jellies.Length; i++)
            {
                Vector3 target = JellyTarget(i, s);
                float speed = (s == Stage.Finale && i == 0) ? 1.2f : 0.6f;
                _jellyPos[i] = Vector3.Lerp(_jellyPos[i], target, 1f - Mathf.Exp(-dt * speed * 0.5f));
                jellies[i].position = _jellyPos[i] + Vector3.up * Mathf.Sin(t * 0.8f + i * 1.9f) * 0.12f;
            }

            float glowTarget = s == Stage.BreathHold ? 1f : (s == Stage.DuskTransition ? 0.4f : 0.12f);
            _glow = Mathf.MoveTowards(_glow, glowTarget, dt * 0.6f);
            Color e = jellyColor * (_glow * 3f);
            _mpb.SetColor(EmissionColor, e);
            for (int i = 0; i < jellyRenderers.Length; i++)
                jellyRenderers[i].SetPropertyBlock(_mpb);
        }

        Vector3 JellyTarget(int i, Stage s)
        {
            Vector3 home = _jellyHome[i];
            switch (s)
            {
                case Stage.DuskTransition:
                case Stage.BreathHold:
                    return home;                                         // 飘来并停留
                case Stage.ExhaleTransition:
                    return i == 0 ? home : FarFrom(home);                // 缓慢飘走,留下一只
                case Stage.Finale:
                    return i == 0 ? jellyHandPos : FarFrom(home);        // 留下的那只靠近患者(握手)
                default:
                    return FarFrom(home);                                // 其它阶段不在场
            }
        }

        static Vector3 FarFrom(Vector3 home)
        {
            Vector3 d = new Vector3(home.x, 0, home.z);
            if (d.sqrMagnitude < 0.01f) d = Vector3.forward;
            return home + d.normalized * 40f + Vector3.up * 6f;
        }
    }
}
