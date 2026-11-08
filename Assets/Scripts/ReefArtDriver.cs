using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UltrasoundReef;

namespace UnderTheSea
{
    /// <summary>Adapts the imported procedural art to the existing experience state machine.</summary>
    public sealed class ReefArtDriver : MonoBehaviour
    {
        public GelSeaweedPreview gel;
        public JellyfishBreath[] jellyAnimation;
        public Transform gelTagTarget;
        public Volume volume;
        public Color daySky, dayEquator, dayGround;
        public float arrivalSeconds = 6f;
        public float arrivalDelay = 1.2f;

        [Header("水母游动(2026-11-05 第二轮)")]
        [Tooltip("场景4 吸气到最大时水母放大到原来的多少倍(2026-11-05:放大幅度 +50%,0.3 → 0.45)")] public float breathJellyScale = 1.45f;
        [Tooltip("场景5 水母游走所需秒数")] public float leaveSeconds = 14f;
        [Tooltip("每次收缩推进时的上下起伏幅度(米)")] public float bobHeight = .09f;

        OceanStageView view;
        Vector3[] homes, positions, starts, aways, leaveFrom, baseScales;
        float[] visibility, pulseRate, pulsePhase;
        float lastGelTracking = float.NegativeInfinity;
        bool initialized;

        public void Initialize(OceanStageView stageView)
        {
            view = stageView;
            int count = view.jellies.Length;
            homes = new Vector3[count]; positions = new Vector3[count];
            starts = new Vector3[count]; visibility = new float[count];
            aways = new Vector3[count]; leaveFrom = new Vector3[count]; baseScales = new Vector3[count];
            pulseRate = new float[count]; pulsePhase = new float[count];
            Vector3 hero = view.heroCoral.position;
            for (int i = 0; i < count; i++)
            {
                homes[i] = view.jellies[i].position;
                baseScales[i] = view.jellies[i].localScale;
                // 从停留点的外侧、更高更远处游来;离开时继续往外、往上游走
                Vector3 outward = new Vector3(homes[i].x - hero.x, 0f, homes[i].z - hero.z);
                outward = outward.sqrMagnitude < .01f ? Vector3.forward : outward.normalized;
                starts[i] = homes[i] + outward * 6.5f + Vector3.up * 2.4f;
                aways[i] = homes[i] + outward * 16f + Vector3.up * 6f;
                positions[i] = starts[i];
                pulseRate[i] = .36f + .05f * (i % 3);
                pulsePhase[i] = i * .37f;
                jellyAnimation[i].handleInput = false;
                jellyAnimation[i].Visibility = 0f;
            }
            if (gel) gel.handleInput = gel.showUI = false;
            initialized = true;
            view.controller.StageChanged += StageChanged;
            StageChanged(Stage.Standby, view.controller.Current);
        }

        void StageChanged(Stage previous, Stage next)
        {
            // Coverage on entry; part the rooted leaves once, then keep them parted through the exam.
            bool parted = (int)next >= (int)Stage.ApplyGel && (int)next <= (int)Stage.Exit;
            if (gel && (gel.gelActive != parted || next == Stage.ApplyGel)) gel.SetGel(parted);
            if (gel && next != Stage.ApplyGel)
            {
                gel.externalGelTarget = null;
                lastGelTracking = float.NegativeInfinity;
            }
            if (next == Stage.DuskTransition)
                for (int i = 0; i < positions.Length; i++)
                {
                    positions[i] = starts[i]; visibility[i] = 0f;
                }
            if (next == Stage.ExhaleTransition || (next == Stage.Finale && previous != Stage.ExhaleTransition))
                for (int i = 0; i < positions.Length; i++) leaveFrom[i] = positions[i];
        }

        public void UpdateGel(Stage stage)
        {
            Vector3 target = default;
            bool tracked = stage == Stage.ApplyGel && view.tagSource != null
                && view.tagSource.TryGetPose(TagIds.Seaweed, out target, out _);
            if (tracked)
            {
                // A tracking target bends the fronds; it never translates their planted roots.
                Vector3 center = view.bellyPoint.position, half = view.bellyHalfExtents;
                gelTagTarget.position = new Vector3(
                    Mathf.Clamp(target.x, center.x - half.x, center.x + half.x),
                    Mathf.Clamp(target.y, center.y - half.y, center.y + half.y),
                    Mathf.Clamp(target.z, center.z - half.z, center.z + half.z));
                lastGelTracking = Time.time;
            }
            gel.externalGelTarget = stage == Stage.ApplyGel && Time.time - lastGelTracking <= view.tagLostGrace
                ? gelTagTarget : null;
        }

        /// <summary>
        /// 水母像真实水母那样游:伞盖一缩一放,收缩时向前推进并上浮一点,舒张时滑行、缓慢下沉,
        /// 所以整体是"一上一下"地靠近。
        /// - 天黑过渡:从远处一下一下游到停留点,逐渐显现;过渡结束后在停留点附近悬浮(不发光)。
        /// - 场景4:呼吸程度 breath(按住扳机 3 秒到 1)让水母放大到 130% 并慢慢发光;松开 3 秒回到正常。
        /// - 呼气过渡:天亮,水母仍悬浮。场景5:全部水母缓慢游走并消失,不留下任何一只。
        /// </summary>
        public void UpdateJellies(Stage stage, float dt, float clock, float breath)
        {
            float T = view.controller.TimeInStage;
            float b = Mathf.SmoothStep(0f, 1f, breath);
            for (int i = 0; i < positions.Length; i++)
            {
                float delay = i * .35f;
                switch (stage)
                {
                    case Stage.DuskTransition:
                    {
                        // 在过渡时间内恰好到达:脉冲式推进(每个脉冲先快后慢)
                        float dur = Mathf.Max(1f, view.controller.duskDuration - .4f - delay);
                        float x = Mathf.Clamp01((T - delay) / dur);
                        positions[i] = Vector3.Lerp(starts[i], homes[i], Pulsed(x, 3 + i % 2));
                        visibility[i] = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((T - delay) / 2.5f));
                        break;
                    }
                    case Stage.BreathHold:
                        positions[i] = Vector3.Lerp(positions[i], homes[i], 1f - Mathf.Exp(-dt * 1.2f));
                        visibility[i] = Mathf.MoveTowards(visibility[i], 1f, dt * .5f);
                        break;
                    case Stage.ExhaleTransition:   // 场景4·5 合并:天亮了水母就游走
                    case Stage.Finale:
                    {
                        float x = Mathf.Clamp01((T - delay) / leaveSeconds);
                        positions[i] = Vector3.Lerp(leaveFrom[i], aways[i], Pulsed(x, 4));
                        visibility[i] = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.45f, 1f, x));
                        break;
                    }
                    default:
                        visibility[i] = Mathf.MoveTowards(visibility[i], 0f, dt * .5f);
                        break;
                }

                // 伞盖节律:c∈[0,.3) 收缩(上浮、身体变瘦长),c∈[.3,1) 舒张滑行(缓慢下沉)
                float c = Mathf.Repeat(clock * pulseRate[i] + pulsePhase[i], 1f);
                float rise = c < .3f ? Mathf.SmoothStep(0f, 1f, c / .3f) : 1f - (c - .3f) / .7f;
                float squeeze = c < .3f ? Mathf.Sin(c / .3f * Mathf.PI) : 0f;
                view.jellies[i].position = positions[i] + Vector3.up * (rise - .5f) * bobHeight;
                float grow = 1f + (breathJellyScale - 1f) * b;
                view.jellies[i].localScale = Vector3.Scale(baseScales[i],
                    new Vector3(1f - .07f * squeeze, 1f + .06f * squeeze, 1f - .07f * squeeze)) * grow;
                jellyAnimation[i].Visibility = visibility[i];
                jellyAnimation[i].Breath = b;   // 只在场景4 吸气时发光;天黑悬浮时不发光
            }
        }

        /// <summary>0..1 的脉冲式进度:n 次"推进-滑行",在 x=1 时恰好到达 1。</summary>
        static float Pulsed(float x, int n)
        {
            float w = 2f * Mathf.PI * n;
            return Mathf.Clamp01(x - Mathf.Sin(w * x) / w);
        }

        /// <summary>某只水母当前是否可见(自检用)。</summary>
        public float JellyVisibility(int i) => visibility != null && i < visibility.Length ? visibility[i] : 0f;
        public Vector3 JellyBaseScale(int i) => baseScales[i];

        public void ApplyLighting(float dark)
        {
            Shader.SetGlobalFloat("_ReefNightBlend", dark);
            RenderSettings.ambientSkyColor = Color.Lerp(daySky, new Color(.035f, .07f, .12f), dark);
            RenderSettings.ambientEquatorColor = Color.Lerp(dayEquator, new Color(.025f, .05f, .09f), dark);
            RenderSettings.ambientGroundColor = Color.Lerp(dayGround, new Color(.015f, .03f, .05f), dark);
            // Preserve passthrough alpha throughout AR and the reveal. The authored sky and
            // volume are used once fully immersed; post processing never grades the AR Start view.
            bool immersed = view.VrAmount >= .999f;
            view.cam.clearFlags = immersed && RenderSettings.skybox != null
                ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            var cameraData = view.cam.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData) cameraData.renderPostProcessing = immersed;
            if (volume) volume.weight = immersed ? 1f : 0f;
        }

        public void UpdateBackgroundFish(Transform[] fish, float dt, float clock)
        {
            // Preserve the source preview's slow school route and smooth orientation.
            for (int i = 0; i < fish.Length; i++)
            {
                float a = clock * .09f + i * .25f;
                Vector3 goal = new Vector3(-3.5f + Mathf.Sin(a) * 3.4f,
                    3.6f + Mathf.Sin(a * .65f + i) * .5f, 10f + Mathf.Cos(a) * 2f);
                Vector3 direction = goal - fish[i].position;
                float blend = 1f - Mathf.Exp(-dt * 2.2f);
                fish[i].position = Vector3.Lerp(fish[i].position, goal, blend);
                if (direction.sqrMagnitude > .00005f)
                    fish[i].rotation = Quaternion.Slerp(fish[i].rotation, Quaternion.LookRotation(direction), blend);
            }
        }

        void OnDestroy()
        {
            if (initialized && view != null && view.controller != null)
                view.controller.StageChanged -= StageChanged;
            Shader.SetGlobalFloat("_ReefNightBlend", 0f);
        }
    }
}
