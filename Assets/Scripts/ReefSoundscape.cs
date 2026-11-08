using UnityEngine;

namespace UnderTheSea
{
    /// <summary>
    /// 音效(2026-11-05,文件来自"VR 音效.7z",按文件名放到对应时机,原文件名保留在 Assets/Audio):
    /// - 背景音 泡泡咕嘟咕嘟(常驻):整个水下世界循环播放,音量跟随 VR 显现程度(AR 现实时静音)。
    /// - 进入VR世界后的背景乐 搭配开场词:场景1 中 VR 世界完全显现后播放一次。
    /// - 播放完背景乐和开场词,播放这个:上一条播完后紧接着播放一次。
    /// - 每结束一个场景 放这个:助手按 A 结束场景 1/2/3/4/4·5 时播放(进入退场时由退场音效代替)。
    /// - 小鱼靠近珊瑚:涂耦合剂开始(小鱼游进我体内)、天黑过渡开始(小鱼躲回珊瑚)时播放。
    /// - 憋气时背景音:场景4 期间循环,淡入淡出。
    /// - 憋气时声音:按住扳机(吸气/憋气)播放前半段,停在中点;松开(呼气)播放后半段。可反复。
    /// - 退场开始进入ar世界时播放:进入退场(VR→AR)时播放一次。
    /// 全部为 2D 声音(患者无论朝哪看都一样听到)。
    /// </summary>
    public sealed class ReefSoundscape : MonoBehaviour
    {
        public ExperienceController controller;
        public OceanStageView view;

        [Header("音效(按文件名对应)")]
        public AudioClip bubblesLoop;          // 背景音 泡泡咕嘟咕嘟(常驻)
        public AudioClip enterMusic;           // 进入VR世界后的背景乐 搭配开场词
        public AudioClip afterIntro;           // 播放完背景乐和开场词,播放这个
        public AudioClip sceneEnd;             // 每结束一个场景 放这个
        public AudioClip fishToCoral;          // 小鱼靠近珊瑚
        public AudioClip breathHoldAmbience;   // 憋气时背景音
        public AudioClip breathInOut;          // 憋气时声音(前半段=憋气,后半段=呼气)
        public AudioClip exitToAR;             // 退场开始进入ar世界时播放

        [Header("音量")]
        [Range(0, 1)] public float bubblesVolume = 1f;
        [Range(0, 1)] public float musicVolume = 1f;
        [Range(0, 1)] public float sfxVolume = 1f;
        [Range(0, 1)] public float breathAmbienceVolume = .25f;  // 憋气时背景音:2026-11-05 用户要求 −75%
        [Range(0, 1)] public float breathVolume = .7f;           // 憋气时声音(吸气/呼气):−30%
        [Range(0, 1)] public float afterIntroVolume = .25f;      // 播放完背景乐和开场词,播放这个:−75%
        [Range(0, 1)] public float sceneEndVolume = .25f;        // 每结束一个场景 放这个:−75%
        [Range(0, 1)] public float exitToARVolume = .5f;         // 退场开始进入ar世界时播放(结束音效):−50%

        [Header("总增益(2026-11-05:Quest 自带喇叭偏小,所有声音和配音一起放大;软限幅不会爆音)")]
        [Range(.5f, 4f)] public float masterGain = 1.5f;

        [Header("每个音效的增益(由场景生成器按录音响度算出,统一响度;volume 最多到 1,再用增益补)")]
        [Range(1, 8)] public float bubblesGain = 2.9f;
        [Range(1, 8)] public float enterMusicGain = 1.3f;
        [Range(1, 8)] public float afterIntroGain = 2.2f;
        [Range(1, 8)] public float sceneEndGain = 2f;
        [Range(1, 8)] public float fishToCoralGain = 1.3f;
        [Range(1, 8)] public float exitToARGain = 1.5f;
        [Range(1, 8)] public float breathAmbienceGain = 2.5f;
        [Range(1, 8)] public float breathGain = 3.7f;

        [Header("配音时压低(2026-11-05:只压低开场音乐,环境声只轻微压低)")]
        [Range(0, 1)] public float musicDuck = .5f;
        [Range(0, 1)] public float ambienceDuck = .85f;

        AudioSource bubbles, music, sfx, breathAmb, breath;
        /// <summary>配音播放程度 0..1(由 ReefVoiceOver 平滑写入);0 = 没有配音,不压低。</summary>
        [System.NonSerialized] public float voiceLevel;
        float MusicDuck => Mathf.Lerp(1f, musicDuck, voiceLevel);
        float AmbienceDuck => Mathf.Lerp(1f, ambienceDuck, voiceLevel);
        bool introStarted, afterIntroPending;
        float breathAmbLevel;

        /// <summary>自检用:当前状态。</summary>
        public bool IntroStarted => introStarted;
        public bool BreathPlaying => breath && breath.isPlaying;
        public float BreathTime => breath ? breath.time : 0f;
        public float BubblesVolumeNow => bubbles ? bubbles.volume : 0f;
        public float BreathAmbienceVolumeNow => breathAmb ? breathAmb.volume : 0f;

        AudioGain musicGain, sfxGain;
        float sfxMaxGain = 1f;

        AudioSource NewSource(string name, bool loop, float gain, out AudioGain g)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false; s.loop = loop; s.spatialBlend = 0f;
            g = go.AddComponent<AudioGain>(); g.gain = gain;
            return s;
        }

        void Awake()
        {
            AudioGain.Master = masterGain;
            bubbles = NewSource("Bubbles (always on)", true, bubblesGain, out _);
            music = NewSource("Intro music", false, enterMusicGain, out musicGain);
            // 一次性音效共用一个声源:声源增益取最大值,每个音效再用 PlayOneShot 的音量比例缩回各自增益
            sfxMaxGain = Mathf.Max(sceneEndGain, fishToCoralGain, exitToARGain, 1f);
            sfx = NewSource("Scene SFX", false, sfxMaxGain, out sfxGain);
            breathAmb = NewSource("Breath-hold ambience", true, breathAmbienceGain, out _);
            breath = NewSource("Breath in/out", false, breathGain, out _);
            bubbles.clip = bubblesLoop; breathAmb.clip = breathHoldAmbience; breath.clip = breathInOut;
            bubbles.volume = 0f; breathAmb.volume = 0f;
        }

        void Start()
        {
            if (!bubbles) Awake();   // 防御:编辑器自检可能先调用 Start
            if (bubbles.clip) bubbles.Play();
            if (breathAmb.clip) breathAmb.Play();
            controller.StageChanged += OnStageChanged;
        }

        void OnDestroy()
        {
            if (controller) controller.StageChanged -= OnStageChanged;
        }

        static bool IsMainScene(Stage s) =>
            s == Stage.Enter || s == Stage.ApplyGel || s == Stage.Probe || s == Stage.BreathHold || s == Stage.ExhaleTransition;

        void OnStageChanged(Stage previous, Stage next)
        {
            if (next == Stage.Standby)
            {
                // 回到 Start(含 B 键重置):停止所有一次性声音,下一轮重新播放开场
                music.Stop(); sfx.Stop(); breath.Stop();
                introStarted = afterIntroPending = false;
                return;
            }
            bool forward = (int)next > (int)previous;
            if (next == Stage.Exit)
            {
                Play(exitToAR, exitToARGain, exitToARVolume);
                return;
            }
            if (forward && IsMainScene(previous)) Play(sceneEnd, sceneEndGain, sceneEndVolume);
            if (next == Stage.ApplyGel || next == Stage.DuskTransition) Play(fishToCoral, fishToCoralGain, 1f);
        }

        void Play(AudioClip clip, float gain, float volume)
        {
            if (clip) sfx.PlayOneShot(clip, sfxVolume * volume * Mathf.Clamp01(gain / sfxMaxGain));
        }

        void Update()
        {
            var s = controller.Current;
            float vr = view ? view.VrAmount : 1f;
            AudioGain.Master = masterGain;   // Inspector 里可以实时调

            // 常驻泡泡声:跟随 VR 显现程度
            bubbles.volume = bubblesVolume * vr * AmbienceDuck;

            // 进入 VR 世界完全显现后:开场背景乐 + 开场词,播完接下一段
            if (s == Stage.Enter && !introStarted && vr >= .99f && enterMusic)
            {
                introStarted = true; afterIntroPending = true;
                music.clip = enterMusic; musicGain.gain = enterMusicGain; music.volume = musicVolume; music.Play();
            }
            if (afterIntroPending && introStarted && !music.isPlaying && s != Stage.Exit && s != Stage.Finished)
            {
                afterIntroPending = false;
                if (afterIntro) { music.clip = afterIntro; musicGain.gain = afterIntroGain; music.volume = musicVolume * afterIntroVolume; music.Play(); }
            }
            if (music.isPlaying && s != Stage.Exit && s != Stage.Finished) music.volume = musicVolume * (music.clip == afterIntro ? afterIntroVolume : 1f) * MusicDuck;
            // 退场 / 回到现实时,开场音乐淡出
            if ((s == Stage.Exit || s == Stage.Finished) && music.isPlaying)
                music.volume = Mathf.MoveTowards(music.volume, 0f, Time.deltaTime / 2f);

            // 场景4:憋气背景音淡入淡出
            breathAmbLevel = Mathf.MoveTowards(breathAmbLevel, s == Stage.BreathHold ? 1f : 0f, Time.deltaTime / 1.5f);
            breathAmb.volume = breathAmbienceVolume * breathAmbLevel * AmbienceDuck;

            UpdateBreath(s);
        }

        bool inhaling;

        /// <summary>吸气声:"憋气时声音"的前半段,播到中点停住(2026-11-06 起由 BreathGuide 在合适时机调用)。</summary>
        public void PlayInhale()
        {
            if (!breath || !breath.clip) return;
            inhaling = true;
            breath.volume = breathVolume; breath.time = 0f; breath.Play();
        }

        /// <summary>吐气声:"憋气时声音"的后半段。</summary>
        public void PlayExhale()
        {
            if (!breath || !breath.clip) return;
            inhaling = false;
            breath.volume = breathVolume; breath.time = breath.clip.length * .5f; breath.Play();
        }

        void UpdateBreath(Stage s)
        {
            if (!breath.clip) return;
            float half = breath.clip.length * .5f;
            if (inhaling && breath.isPlaying && breath.time >= half)
            {
                breath.Pause();                 // 吸气声播到中点停住
                breath.time = half;
            }
            if (s != Stage.BreathHold && breath.isPlaying)
            {
                inhaling = false;
                breath.volume = Mathf.MoveTowards(breath.volume, 0f, Time.deltaTime);
                if (breath.volume <= 0f) breath.Stop();
            }
        }
    }
}
