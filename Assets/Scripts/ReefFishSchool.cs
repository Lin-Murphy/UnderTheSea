using System.Collections.Generic;
using UnityEngine;

namespace UnderTheSea
{
    /// <summary>
    /// 环境鱼群(近处 + 远处),鱼模型与游动方式来自 Unity Version Control 分支 /main/demo1:
    /// ReefScene_Unity.fbx 的 Fish_* 网格,围绕参与者(主角珊瑚)做轨道游动,头朝游动方向。
    ///
    /// 天黑时(天黑过渡 + 场景4):全场约 80% 的小鱼游向附近的珊瑚,其中一部分贴到参与者自己(主角珊瑚)身上;
    /// 它们钻进珊瑚枝杈之间的空隙,在空隙之间来回穿梭。一条较大的"天敌"鱼在礁石间缓慢巡游,
    /// 它靠近时附近的小鱼会更快地躲进下一个空隙。呼气(天亮)后小鱼回到原来的轨道游动。
    /// </summary>
    public sealed class ReefFishSchool : MonoBehaviour
    {
        [Header("鱼(由场景生成器填写)")]
        public Transform[] fish;                  // 先 nearCount 条近处鱼,其余为远处鱼群
        public int nearCount;
        [Tooltip("下标 >= 此值的鱼是「身体小鱼」(原来的探头小鱼):总是躲到参与者自己身上")] public int bodyFishStart = int.MaxValue;
        public Transform heroCoral;               // 参与者自己
        public Transform[] hideCorals;            // 附近可供躲藏的装饰珊瑚(带 MeshFilter)
        public Transform predator;                // 天敌(可为空)

        [Header("场景3:躲进珊瑚")]
        [Range(0f, 1f)] public float hideFraction = .8f;   // 全场多少比例的小鱼去躲
        [Range(0f, 1f)] public float onMeFraction = .3f;   // 躲藏的小鱼里有多少贴到参与者身上
        [Tooltip("躲进枝杈时缩小到原尺寸的比例(便于钻进空隙)")] public float hideScale = .45f;
        public int slotsPerCoral = 8;
        [Tooltip("参与者自己身上的空隙数量(身体小鱼 + 一部分环境小鱼都躲在这里)")] public int slotsOnMe = 16;
        public float cruiseSpeed = 1.6f, travelSpeed = 3.5f, panicSpeed = 3f;
        public float panicRadius = 2.5f;

        enum Mode { Orbit, Hiding, Returning }

        struct FishState
        {
            public Mode mode;
            public Vector3 pos, vel, baseScale;
            public float scaleK, dwell, returnTime, revealThr;
            public int coral, slot;
            public bool hider;
        }

        OceanStageView view;
        FishState[] st;
        readonly List<Transform> coralTs = new List<Transform>();       // [0] = heroCoral
        readonly List<Vector3[]> coralSlots = new List<Vector3[]>();     // 每个珊瑚的空隙点(珊瑚本地坐标)
        Vector3 center;
        Vector3 predatorBase;
        float predatorVis;
        bool initialized;

        /// <summary>只在天黑时躲藏:天黑过渡 + 场景4(天黑、水母环绕)。天亮(呼气)后回到轨道游动。</summary>
        public bool IsHidingStage(Stage s) => s == Stage.DuskTransition || s == Stage.BreathHold;
        /// <summary>涂耦合剂时,"我这一组"小鱼(身体小鱼 + 分配到我身上的环境小鱼)游进我体内躲着;探头检查时出来回到正常。</summary>
        public static bool IsGelStage(Stage s) => s == Stage.ApplyGel;

        /// <summary>当前正贴在参与者身上(主角珊瑚空隙里)的小鱼数量;用于自检。</summary>
        public int FishOnMe { get; private set; }
        /// <summary>当前在任意珊瑚空隙附近(0.25m 内)的小鱼数量。</summary>
        public int FishInGaps { get; private set; }
        public int HiderCount { get; private set; }

        public void Initialize(OceanStageView stageView)
        {
            view = stageView;
            Vector3 h = heroCoral.position;
            center = new Vector3(h.x, 0f, h.z);

            coralTs.Clear(); coralSlots.Clear();
            AddCoral(heroCoral, 0, slotsOnMe);
            if (hideCorals != null)
                for (int i = 0; i < hideCorals.Length; i++)
                    if (hideCorals[i]) AddCoral(hideCorals[i], i + 1, slotsPerCoral);

            st = new FishState[fish.Length];
            int hiders = 0;
            for (int i = 0; i < fish.Length; i++)
            {
                bool body = i >= bodyFishStart;
                bool near = i < nearCount || body;
                bool hider = body || Hash01(i, 11) < hideFraction;
                bool onMe = body || coralTs.Count == 1 || Hash01(i, 23) < onMeFraction;
                int coral = onMe ? 0 : 1 + (int)(Hash01(i, 37) * (coralTs.Count - 1)) % Mathf.Max(1, coralTs.Count - 1);
                if (hider) hiders++;
                st[i] = new FishState
                {
                    mode = Mode.Orbit,
                    pos = fish[i].position,
                    baseScale = fish[i].localScale,
                    scaleK = 1f,
                    revealThr = near ? .3f + .003f * (i % 40) : .5f + .003f * (i - nearCount),
                    hider = hider,
                    coral = coral,
                    slot = (int)(Hash01(i, 41) * (onMe ? slotsOnMe : slotsPerCoral)),
                    dwell = 0f,
                };
            }
            HiderCount = hiders;
            if (predator) { predatorBase = predator.localScale; predator.gameObject.SetActive(false); }
            initialized = true;
        }

        public void Tick(Stage s, float dt, float t)
        {
            if (!initialized) return;
            bool hideStage = IsHidingStage(s);
            bool gelStage = IsGelStage(s);
            UpdatePredator(hideStage, dt, t);

            int onMe = 0, inGaps = 0;
            for (int i = 0; i < fish.Length; i++)
            {
                ref FishState f = ref st[i];
                var tr = fish[i];
                bool body = i >= bodyFishStart;
                bool near = i < nearCount || body;
                int oi = near ? i : i - nearCount;
                Vector3 orbit = OrbitPoint(near, oi, t);

                // 状态切换
                // 天黑:所有躲藏鱼都躲(我这一组躲进我体内,其余躲进附近珊瑚);涂耦合剂:只有我这一组躲进我体内
                bool hideNow = f.hider && (hideStage || (gelStage && f.coral == 0));
                if (hideNow && f.mode != Mode.Hiding) { f.mode = Mode.Hiding; f.dwell = 0f; }
                else if (!hideNow && f.mode == Mode.Hiding) { f.mode = Mode.Returning; f.returnTime = 0f; }

                Vector3 prev = f.pos;
                float scaleTarget = 1f;
                switch (f.mode)
                {
                    case Mode.Orbit:
                        f.pos = orbit;
                        f.vel = (OrbitPoint(near, oi, t + .05f) - orbit) / .05f;
                        break;

                    case Mode.Hiding:
                    {
                        scaleTarget = i >= bodyFishStart ? .8f : hideScale;   // 身体小鱼本来就小
                        Vector3 slot = SlotWorld(f.coral, f.slot);
                        // 在空隙里轻微游动
                        float ph = i * 1.37f;
                        float wob = .035f * CoralSize(f.coral);
                        Vector3 target = slot + new Vector3(Mathf.Sin(t * 1.7f + ph), Mathf.Sin(t * 2.3f + ph) * .5f, Mathf.Cos(t * 1.4f + ph)) * wob;
                        Vector3 to = target - f.pos;
                        float dist = to.magnitude;
                        bool panic = predatorVis > .3f && predator && (predator.position - f.pos).sqrMagnitude < panicRadius * panicRadius;
                        float maxSpeed = dist > 3f ? travelSpeed : (panic ? panicSpeed : cruiseSpeed);
                        float speed = Mathf.Min(maxSpeed, dist * 2.5f);
                        Vector3 desired = dist > 1e-4f ? to / dist * speed : Vector3.zero;
                        f.vel = Vector3.Lerp(f.vel, desired, 1f - Mathf.Exp(-dt * (panic ? 6f : 3f)));
                        f.pos += f.vel * dt;

                        if (dist < .06f * CoralSize(f.coral) + .03f)
                        {
                            f.dwell += dt * (panic ? 4f : 1f);
                            if (f.dwell > 1.2f + Hash01(i * 7 + f.slot, 5) * 2.3f)
                            {
                                f.dwell = 0f;
                                f.slot = NextSlot(f.coral, f.slot, i);   // 钻到下一个空隙
                            }
                        }
                        if (dist < .25f) { inGaps++; if (f.coral == 0) onMe++; }
                        break;
                    }

                    case Mode.Returning:
                    {
                        f.returnTime += dt;
                        float rate = 1.2f + f.returnTime * 1.5f;   // 逐渐追上快速移动的轨道点
                        f.pos = Vector3.Lerp(f.pos, orbit, 1f - Mathf.Exp(-dt * rate));
                        f.vel = (f.pos - prev) / Mathf.Max(dt, 1e-4f);
                        if ((f.pos - orbit).sqrMagnitude < .08f * .08f) f.mode = Mode.Orbit;
                        break;
                    }
                }

                tr.position = f.pos;
                if (f.vel.sqrMagnitude > 1e-6f)
                {
                    var look = Quaternion.LookRotation(f.vel);
                    tr.rotation = f.mode == Mode.Orbit ? look : Quaternion.Slerp(tr.rotation, look, 1f - Mathf.Exp(-dt * 8f));
                }
                f.scaleK = Mathf.MoveTowards(f.scaleK, scaleTarget, dt * .6f);
                tr.localScale = f.baseScale * (view.Reveal(f.revealThr) * f.scaleK);
            }
            FishOnMe = onMe; FishInGaps = inGaps;
        }

        Vector3 OrbitPoint(bool near, int i, float t) => near
            ? OceanStageView.OrbitPos(i, t, 1.5f, 3f, .4f, .8f, .5f, center)       // demo1 近处鱼
            : OceanStageView.OrbitPos(i, t, 14f, 8f, 1.5f, 4f, .15f, center);      // demo1 远处鱼群

        // ---------- 天敌 ----------
        void UpdatePredator(bool active, float dt, float t)
        {
            if (!predator) return;
            predatorVis = Mathf.MoveTowards(predatorVis, active ? 1f : 0f, dt / 3f);
            bool show = predatorVis > .01f;
            if (predator.gameObject.activeSelf != show) predator.gameObject.SetActive(show);
            if (!show) return;
            Vector3 p = PredatorPos(t), n = PredatorPos(t + .05f);
            predator.position = p;
            if ((n - p).sqrMagnitude > 1e-8f) predator.rotation = Quaternion.LookRotation(n - p);
            predator.localScale = predatorBase * (predatorVis * view.Reveal(.5f));
        }

        Vector3 PredatorPos(float t)
        {
            // 在参与者前方礁石间做缓慢的 8 字形巡游
            float a = t * .11f;
            return center + new Vector3(Mathf.Sin(a) * 5.5f, 1.3f + Mathf.Sin(a * .7f) * .3f, 4.5f + Mathf.Sin(2f * a) * 3f);
        }

        // ---------- 珊瑚空隙 ----------
        void AddCoral(Transform coral, int seed, int count)
        {
            var mf = coral.GetComponent<MeshFilter>();
            if (!mf) mf = coral.GetComponentInChildren<MeshFilter>();
            Vector3[] slots = mf && mf.sharedMesh && mf.sharedMesh.isReadable
                ? FindGaps(mf.sharedMesh, seed, count)
                : null;
            if (slots == null || slots.Length == 0) slots = FallbackSlots(mf ? mf.sharedMesh.bounds : new Bounds(Vector3.up * .5f, Vector3.one), count);
            // 空隙点存成珊瑚 Transform 的本地坐标(MeshFilter 可能在子物体上)
            if (mf && mf.transform != coral)
                for (int i = 0; i < slots.Length; i++)
                    slots[i] = coral.InverseTransformPoint(mf.transform.TransformPoint(slots[i]));
            coralTs.Add(coral);
            coralSlots.Add(slots);
        }

        /// <summary>
        /// 在网格本地空间里找"枝杈之间的空隙":取两个相距较远的表面点的中点,
        /// 要求它离最近的表面既不太近(不是贴在枝干表面/枝干内部)也不太远(仍在珊瑚丛里),高度在珊瑚中上部。
        /// </summary>
        Vector3[] FindGaps(Mesh mesh, int seed, int count)
        {
            var all = mesh.vertices;
            if (all.Length < 8) return null;
            var rng = new System.Random(1234 + seed * 97);
            int sampleCount = Mathf.Min(600, all.Length);
            var verts = new Vector3[sampleCount];
            for (int i = 0; i < sampleCount; i++) verts[i] = all[rng.Next(all.Length)];
            Bounds b = mesh.bounds;
            float size = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            float spread = Mathf.Max(b.size.x, b.size.z);
            var found = new List<Vector3>();
            for (int attempt = 0; attempt < 900 && found.Count < count; attempt++)
            {
                Vector3 a = verts[rng.Next(sampleCount)], c = verts[rng.Next(sampleCount)];
                if ((a - c).magnitude < .3f * spread) continue;
                Vector3 m = (a + c) * .5f;
                float hf = (m.y - b.min.y) / Mathf.Max(1e-4f, b.size.y);
                if (hf < .25f || hf > .9f) continue;
                float nearest = float.MaxValue;
                for (int k = 0; k < sampleCount; k++) nearest = Mathf.Min(nearest, (verts[k] - m).sqrMagnitude);
                nearest = Mathf.Sqrt(nearest);
                if (nearest < .05f * size || nearest > .25f * size) continue;
                bool tooClose = false;
                foreach (var f in found) if ((f - m).magnitude < .12f * size) { tooClose = true; break; }
                if (!tooClose) found.Add(m);
            }
            return found.ToArray();
        }

        Vector3[] FallbackSlots(Bounds b, int count)
        {
            var s = new Vector3[count];
            for (int i = 0; i < s.Length; i++)
            {
                float a = i * 2.39996f, r = .25f + .15f * (i % 3);
                s[i] = b.center + new Vector3(Mathf.Cos(a) * b.extents.x * r, (.1f + .1f * (i % 4)) * b.size.y, Mathf.Sin(a) * b.extents.z * r);
            }
            return s;
        }

        Vector3 SlotWorld(int coral, int slot)
        {
            var slots = coralSlots[coral];
            return coralTs[coral].TransformPoint(slots[slot % slots.Length]);
        }

        float CoralSize(int coral)
        {
            Vector3 s = coralTs[coral].lossyScale;
            return Mathf.Max(.3f, Mathf.Max(s.x, Mathf.Max(s.y, s.z)));
        }

        int NextSlot(int coral, int slot, int fishIndex)
        {
            int n = coralSlots[coral].Length;
            if (n <= 1) return 0;
            int step = 1 + (int)(Hash01(fishIndex * 13 + slot, 3) * (n - 1));
            return (slot + step) % n;
        }

        static float Hash01(int i, int salt)
        {
            uint x = (uint)(i * 747796405 + salt * 2891336453);
            x ^= x >> 16; x *= 0x7feb352d; x ^= x >> 15; x *= 0x846ca68b; x ^= x >> 16;
            return (x & 0xffffff) / (float)0x1000000;
        }
    }
}
