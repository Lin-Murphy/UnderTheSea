using System;
using UnityEngine;

namespace UnderTheSea
{
    /// <summary>
    /// 旁白配音(VO,2026-11-05,按《Voiceover script 确定版》匹配,见 Assets/Audio/VO/VO_script.txt):
    /// 每条配音是一个"提示点"(cue):在某个阶段、满足触发条件、再等待 delay 秒后播放,每轮体验只播一次。
    /// - afterId:必须等指定的上一条配音播完(任何触发类型都适用),保证同一阶段内的顺序。
    /// - fallback:> 0 时,进入阶段 fallback 秒后即使触发条件没满足也播放(例如 tag 一直没识别到)。
    /// - gain:每条配音单独的增益(生成场景时按录音响度自动算出,统一到约 −20 dB)。
    /// 同一时间只播一条;播放时压低开场音乐(环境声只轻微压低)。
    /// 按 B 回到 Start 时停止并重置;正常结束(Finished → Standby)时让结束语(08)播完。
    /// </summary>
    public sealed class ReefVoiceOver : MonoBehaviour
    {
        public enum Trigger
        {
            StageEnter,        // 进入该阶段后 delay 秒
            VrFullyRevealed,   // 该阶段中 VR 完全显现后 delay 秒(用于开场)
            AfterCue,          // 指定的上一条配音播完后 delay 秒(仍需在该阶段内)
            FirstInhale,       // 该阶段中第一次按住扳机(吸气)后 delay 秒
            FirstExhale,       // 该阶段中第一次松开扳机(呼气)后 delay 秒
            FirstTagContact,   // 该阶段中探头 tag 第一次进入海草控制区域后 delay 秒
            ExhaleAfterCue,    // afterId 那条配音开始播放之后,第一次松开扳机(呼气);等那条播完才播
        }

        [Serializable]
        public class Cue
        {
            public string id;
            [TextArea] public string text;
            public AudioClip clip;
            public Stage stage;
            public Trigger trigger;
            public float delay;
            public string afterId;
            [Tooltip("> 0:进入阶段这么多秒后即使条件未满足也播放")] public float fallback;
            [Range(0, 8)] public float gain = 1f;
            [Tooltip("只播放录音的一段:开始秒数")] public float clipStart;
            [Tooltip("只播放录音的一段:结束秒数(0 = 到结尾)")] public float clipEnd;
            [NonSerialized] public bool fired, armed;
            [NonSerialized] public float armTime, playTime;
        }

        public ExperienceController controller;
        public OceanStageView view;
        public ReefSoundscape soundscape;
        public SoftSeaweedStrands seaweed;
        public Cue[] cues = Array.Empty<Cue>();
        [Range(0, 1)] public float volume = 1f;

        AudioSource voice;
        AudioGain voiceGain;
        Cue current;
        bool inhaleSeen, exhaleSeen, tagSeen, wasHeld;
        float lastExhaleTime = -1f, segmentEnd;

        /// <summary>当前正在播放的配音 id(自检用)。</summary>
        public string Speaking => current != null && voice && voice.isPlaying ? current.id : null;

        void Awake()
        {
            var go = new GameObject("Voice-over");
            go.transform.SetParent(transform, false);
            voice = go.AddComponent<AudioSource>();
            voice.playOnAwake = false; voice.spatialBlend = 0f; voice.priority = 0;
            voiceGain = go.AddComponent<AudioGain>();
        }

        void Start() => controller.StageChanged += OnStageChanged;
        void OnDestroy() { if (controller) controller.StageChanged -= OnStageChanged; }

        void OnStageChanged(Stage previous, Stage next)
        {
            inhaleSeen = exhaleSeen = tagSeen = false;
            if (next == Stage.Standby)
            {
                // B 键中途重置:立即停止;正常结束(Finished → Standby):让正在播的结束语播完
                if (previous != Stage.Finished) { voice.Stop(); current = null; }
                foreach (var c in cues) { c.fired = false; c.armed = false; }
                // 正在播的那条保持 fired,避免下一轮立刻重播
                if (current != null && voice.isPlaying) current.fired = true;
                return;
            }
            foreach (var c in cues) if (!c.fired) c.armed = false;
        }

        Cue Find(string id) { foreach (var c in cues) if (c.id == id) return c; return null; }

        bool Done(Cue c) => c != null && c.fired && !(current == c && voice.isPlaying);

        void Update()
        {
            var s = controller.Current;
            float t = Time.time;
            bool held = controller.BreathHeld;
            if (held && !wasHeld) inhaleSeen = true;
            if (!held && wasHeld) { exhaleSeen = true; lastExhaleTime = t; }
            wasHeld = held;

            // 只播一段的配音:到段尾就停
            if (voice.isPlaying && segmentEnd > 0f && voice.time >= segmentEnd) voice.Stop();
            if (seaweed && seaweed.TagInRegion) tagSeen = true;

            // 1. 满足触发条件的提示点开始计时
            foreach (var c in cues)
            {
                if (c.fired || c.armed || !c.clip || c.stage != s) continue;
                if (!string.IsNullOrEmpty(c.afterId) && !Done(Find(c.afterId))) continue;
                bool ready;
                switch (c.trigger)
                {
                    case Trigger.VrFullyRevealed: ready = view == null || view.VrAmount >= .99f; break;
                    case Trigger.FirstInhale: ready = inhaleSeen; break;
                    case Trigger.FirstExhale: ready = exhaleSeen; break;
                    case Trigger.FirstTagContact: ready = tagSeen; break;
                    case Trigger.ExhaleAfterCue:
                        var prev = Find(c.afterId);
                        ready = prev != null && prev.fired && lastExhaleTime > prev.playTime;
                        break;
                    default: ready = true; break;   // StageEnter / AfterCue(顺序由 afterId 保证)
                }
                if (!ready && c.fallback > 0f && controller.TimeInStage >= c.fallback) ready = true;
                if (ready) { c.armed = true; c.armTime = t + c.delay; }
            }

            // 2. 时间到且当前没有配音在播:播放(按表中顺序,一次一条)
            if (!voice.isPlaying)
                foreach (var c in cues)
                {
                    if (c.fired || !c.armed || t < c.armTime || c.stage != s) continue;
                    c.fired = true; current = c; c.playTime = t;
                    voiceGain.gain = c.gain;
                    voice.clip = c.clip; voice.volume = volume;
                    voice.time = Mathf.Clamp(c.clipStart, 0f, c.clip.length - .01f);
                    segmentEnd = c.clipEnd;
                    voice.Play();
                    Debug.Log($"[VO] {c.id}: {c.text}");
                    break;
                }

            // 3. 配音时压低其它声音(平滑 0.3 秒)
            if (soundscape) soundscape.voiceLevel = Mathf.MoveTowards(soundscape.voiceLevel, voice.isPlaying ? 1f : 0f, Time.deltaTime / .3f);
        }
    }
}
