using System.Collections.Generic;
using UnityEngine;

namespace UnderTheSea
{
    /// <summary>
    /// 场景4 呼吸时的发光:按住右手柄扳机(吸气/憋气)→ ExperienceController.Breath 3 秒升到 1,松开 3 秒回到 0。
    /// - 全场珊瑚 / 海葵 / 参与者自己(主角珊瑚)的枝叶尖端发微光:设置全局 shader 参数 _ReefBreathGlow
    ///   (CollabCoral shader 按网格高度判断"尖端")。
    /// - 从这些尖端冒出细小的发光颗粒,缓慢上浮后淡出;参与者自己身上的颗粒最多。
    /// </summary>
    public sealed class BreathGlow : MonoBehaviour
    {
        public ExperienceController controller;
        public Transform self;                 // 参与者 = 主角珊瑚
        public Transform[] glowObjects;        // 附近的珊瑚和海葵(发颗粒)
        public Material sparkleMaterial;
        public GameObject visibleRoot;         // 水下根节点:不可见时不发颗粒
        [Tooltip("呼吸最大时每秒发出的颗粒数")] public float maxRate = 90f;
        [Range(0f, 1f)] public float selfShare = .4f;
        public Color[] sparkleColors = { new Color(.75f, .95f, 1f, .9f), new Color(1f, .9f, .7f, .9f), new Color(.85f, .8f, 1f, .9f) };

        [Header("天黑:全图缓慢闪烁的粒子(憋气时更亮、更快)")]
        public Transform viewer;               // 相机:粒子分布在参与者周围
        [Tooltip("天黑时每秒新出现的闪烁粒子")] public float nightRate = 60f;
        public Vector3 nightVolume = new Vector3(20f, 5f, 20f);
        public Color[] nightColors = { new Color(.55f, .9f, 1f, 1f), new Color(.7f, 1f, .85f, 1f), new Color(.8f, .85f, 1f, 1f) };

        [Header("水母发光时的粒子")]
        public Transform[] jellies;            // 所有水母(用其中的 "Collab jellyfish" 渲染器范围发粒子)
        public ReefArtDriver art;              // 用来判断水母是否可见
        [Tooltip("呼吸最大时每只水母每秒发出的小颗粒")] public float jellySparkleRate = 22f;
        [Tooltip("水母周围光晕的大小(相对伞盖宽度)")] public float jellyHaloSize = 2.6f;
        public Color[] jellyColors = { new Color(.75f, .92f, 1f, 1f), new Color(1f, .78f, .92f, 1f), new Color(.9f, .85f, 1f, 1f) };
        float[] jellyCarry, jellyHaloTimer;
        Renderer[] jellyRenderers;

        static readonly int GlowId = Shader.PropertyToID("_ReefBreathGlow");
        static readonly int NightId = Shader.PropertyToID("_ReefNightBlend");
        ParticleSystem ps, nightPs;
        float nightCarry;

        /// <summary>天黑闪烁粒子当前数量(自检用)。</summary>
        public int NightParticles => nightPs ? nightPs.particleCount : 0;
        readonly List<(Transform t, Vector3[] tips)> sources = new List<(Transform, Vector3[])>();
        float carry;
        System.Random rng = new System.Random(77);

        /// <summary>当前全局尖端发光强度(自检用)。</summary>
        public float GlowLevel { get; private set; }
        public int LiveParticles => ps ? ps.particleCount : 0;

        void Awake()
        {
            var go = new GameObject("Breath sparkles");
            go.transform.SetParent(transform, false);
            ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false; main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1400; main.startLifetime = 2.5f; main.startSpeed = 0f; main.startSize = .03f;
            main.gravityModifier = -.008f;   // 轻轻上浮
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .2f), new GradientAlphaKey(.8f, .7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var noise = ps.noise; noise.enabled = true; noise.strength = .05f; noise.frequency = .6f; noise.scrollSpeed = .2f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = sparkleMaterial;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            ps.Play();

            // 天黑闪烁粒子:alpha 在一生中亮-暗-亮-灭 两次,看起来是缓慢地一闪一闪;寿命越短闪得越快
            var ngo = new GameObject("Night twinkle");
            ngo.transform.SetParent(transform, false);
            nightPs = ngo.AddComponent<ParticleSystem>();
            nightPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var nm = nightPs.main;
            nm.playOnAwake = false; nm.loop = true; nm.simulationSpace = ParticleSystemSimulationSpace.World;
            nm.maxParticles = 700; nm.startLifetime = 4f; nm.startSpeed = 0f; nm.startSize = .04f; nm.gravityModifier = -.002f;
            var ne = nightPs.emission; ne.enabled = false;
            var ns = nightPs.shape; ns.enabled = false;
            var nc = nightPs.colorOverLifetime; nc.enabled = true;
            var ng = new Gradient();
            ng.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                       new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .2f), new GradientAlphaKey(.15f, .45f),
                               new GradientAlphaKey(.9f, .7f), new GradientAlphaKey(0f, 1f) });
            nc.color = ng;
            var nn = nightPs.noise; nn.enabled = true; nn.strength = .04f; nn.frequency = .3f; nn.scrollSpeed = .1f;
            var nr = ngo.GetComponent<ParticleSystemRenderer>();
            nr.sharedMaterial = sparkleMaterial; nr.renderMode = ParticleSystemRenderMode.Billboard;
            nr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; nr.receiveShadows = false;
            nightPs.Play();

            AddSource(self, 60);
            if (glowObjects != null) foreach (var o in glowObjects) if (o) AddSource(o, 24);
            Shader.SetGlobalFloat(GlowId, 0f);
        }

        /// <summary>取网格上半部分(本地高度 > 75%)的顶点作为"尖端"颗粒的出生点。</summary>
        void AddSource(Transform t, int count)
        {
            if (!t) return;
            var mf = t.GetComponent<MeshFilter>();
            if (!mf) mf = t.GetComponentInChildren<MeshFilter>();
            if (!mf || !mf.sharedMesh || !mf.sharedMesh.isReadable) return;
            var v = mf.sharedMesh.vertices;
            var b = mf.sharedMesh.bounds;
            float cut = b.min.y + b.size.y * .75f;
            var tips = new List<Vector3>();
            for (int i = 0; i < v.Length && tips.Count < count * 6; i += Mathf.Max(1, v.Length / 2000))
                if (v[i].y >= cut) tips.Add(t.InverseTransformPoint(mf.transform.TransformPoint(v[i])));
            if (tips.Count == 0) return;
            var picked = new Vector3[Mathf.Min(count, tips.Count)];
            for (int i = 0; i < picked.Length; i++) picked[i] = tips[rng.Next(tips.Count)];
            sources.Add((t, picked));
        }

        void Update()
        {
            if (!controller) return;
            float b = Mathf.SmoothStep(0f, 1f, controller.Breath);
            GlowLevel = b;
            Shader.SetGlobalFloat(GlowId, b);
            EmitNight(b);
            EmitJellies(b);
            if (sources.Count == 0 || b <= .001f || (visibleRoot && !visibleRoot.activeInHierarchy)) { carry = 0f; return; }

            carry += maxRate * b * Time.deltaTime;
            var ep = new ParticleSystem.EmitParams();
            while (carry >= 1f)
            {
                carry -= 1f;
                int si = sources.Count == 1 || rng.NextDouble() < selfShare ? 0 : 1 + rng.Next(sources.Count - 1);
                var (t, tips) = sources[si];
                Vector3 p = t.TransformPoint(tips[rng.Next(tips.Length)]);
                ep.position = p + Random.insideUnitSphere * .02f;
                ep.velocity = new Vector3(Random.Range(-.03f, .03f), Random.Range(.04f, .12f), Random.Range(-.03f, .03f));
                ep.startSize = Random.Range(.018f, .04f) * (si == 0 ? 1f : 1.4f);
                ep.startLifetime = Random.Range(1.6f, 3f);
                ep.startColor = sparkleColors[rng.Next(sparkleColors.Length)];
                ps.Emit(ep, 1);
            }
        }

        /// <summary>天黑程度(天黑过渡 → 场景4 时 _ReefNightBlend 到 0.5)决定粒子多少;憋气 b 让它们更亮、闪得更快。</summary>
        void EmitNight(float b)
        {
            float night = Mathf.Clamp01(Shader.GetGlobalFloat(NightId) / .5f);
            if (!nightPs || !viewer || night <= .02f || (visibleRoot && !visibleRoot.activeInHierarchy)) { nightCarry = 0f; return; }
            nightCarry += nightRate * night * (1f + .6f * b) * Time.deltaTime;
            var ep = new ParticleSystem.EmitParams();
            Vector3 c = viewer.position;
            while (nightCarry >= 1f)
            {
                nightCarry -= 1f;
                ep.position = new Vector3(
                    c.x + (Random.value - .5f) * nightVolume.x,
                    Mathf.Max(c.y - 1.6f, -.3f) + Random.value * nightVolume.y,
                    c.z + (Random.value - .5f) * nightVolume.z);
                if ((ep.position - c).sqrMagnitude < .6f * .6f) continue;   // 不贴脸
                ep.velocity = new Vector3(Random.Range(-.02f, .02f), Random.Range(-.01f, .03f), Random.Range(-.02f, .02f));
                ep.startLifetime = Mathf.Lerp(Random.Range(3.5f, 5.5f), Random.Range(1.6f, 2.4f), b);   // 憋气:闪得更快
                ep.startSize = Random.Range(.025f, .05f) * (1f + .35f * b);
                Color col = nightColors[rng.Next(nightColors.Length)];
                col.a = Mathf.Lerp(.45f, 1f, b) * night;                                              // 憋气:更亮
                ep.startColor = col;
                nightPs.Emit(ep, 1);
            }
        }

        /// <summary>
        /// 水母发光时:伞盖周围一团柔和、随呼吸变大的光晕(大颗、短寿命、连续叠加),
        /// 以及从伞盖和触手上飘散出来的小发光颗粒。呼吸越强越多越亮。
        /// </summary>
        void EmitJellies(float b)
        {
            if (jellies == null || jellies.Length == 0 || b <= .02f || (visibleRoot && !visibleRoot.activeInHierarchy)) return;
            if (jellyRenderers == null || jellyRenderers.Length != jellies.Length)
            {
                jellyRenderers = new Renderer[jellies.Length];
                jellyCarry = new float[jellies.Length]; jellyHaloTimer = new float[jellies.Length];
                for (int i = 0; i < jellies.Length; i++)
                {
                    if (!jellies[i]) continue;
                    var collab = jellies[i].Find("Collab jellyfish");
                    jellyRenderers[i] = collab ? collab.GetComponent<Renderer>() : jellies[i].GetComponentInChildren<Renderer>();
                }
            }
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < jellies.Length; i++)
            {
                var r = jellyRenderers[i];
                if (!r || !r.gameObject.activeInHierarchy) continue;
                float vis = art ? art.JellyVisibility(i) : 1f;
                if (vis < .1f) continue;
                Bounds bb = r.bounds;
                float bell = Mathf.Max(bb.extents.x, bb.extents.z);
                Vector3 bellCenter = new Vector3(bb.center.x, bb.max.y - bell * .55f, bb.center.z);
                Color tint = jellyColors[i % jellyColors.Length];

                // 光晕:每 0.08 秒一颗大的柔光,寿命 0.5 秒,叠起来就是一团持续、微微闪动的光
                jellyHaloTimer[i] -= Time.deltaTime;
                if (jellyHaloTimer[i] <= 0f)
                {
                    jellyHaloTimer[i] = .08f;
                    ep.position = bellCenter + Random.insideUnitSphere * bell * .15f;
                    ep.velocity = Vector3.zero;
                    ep.startSize = bell * 2f * jellyHaloSize * Mathf.Lerp(.6f, 1f, b) * Random.Range(.9f, 1.1f);
                    ep.startLifetime = .5f;
                    Color h = tint; h.a = .16f * b * vis;   // 2026-11-05:亮度峰值减半
                    ep.startColor = h;
                    ps.Emit(ep, 1);
                }

                // 小颗粒:约 60% 从伞盖、40% 沿触手飘出,向外缓慢扩散、上浮
                jellyCarry[i] += jellySparkleRate * b * vis * Time.deltaTime;
                while (jellyCarry[i] >= 1f)
                {
                    jellyCarry[i] -= 1f;
                    Vector3 p;
                    if (Random.value < .6f) p = bellCenter + Vector3.Scale(Random.onUnitSphere, new Vector3(bell, bell * .55f, bell));
                    else p = new Vector3(bb.center.x + Random.Range(-.6f, .6f) * bell, Random.Range(bb.min.y, bellCenter.y), bb.center.z + Random.Range(-.6f, .6f) * bell);
                    ep.position = p;
                    Vector3 outward = p - bellCenter; outward.y = 0f;
                    ep.velocity = outward.normalized * Random.Range(.03f, .1f) + Vector3.up * Random.Range(.02f, .08f);
                    ep.startSize = Random.Range(.025f, .055f) * (1f + .5f * b);
                    ep.startLifetime = Random.Range(1.2f, 2.4f);
                    Color s = jellyColors[rng.Next(jellyColors.Length)]; s.a = vis;
                    ep.startColor = s;
                    ps.Emit(ep, 1);
                }
            }
        }

        void OnDisable() => Shader.SetGlobalFloat(GlowId, 0f);
    }
}
