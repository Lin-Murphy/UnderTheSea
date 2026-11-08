using UnityEngine;

namespace UnderTheSea
{
    /// <summary>
    /// 场景4 呼吸引导(2026-11-06,按用户要求重做):
    /// 1. 进入场景4 播配音 05 "Take a slow breath in, as deeply as feels comfortable."(由 ReefVoiceOver 的 cue 表负责)。
    /// 2. 05 播完后,助手按住扳机 → 播吸气声(憋气时声音的前半段);吸气 1 秒后视觉中心缓缓出现 "Hold"。
    ///    (05 还没播完就按住:等 05 播完再开始吸气声。)
    /// 3. 松开扳机(呼气)→ "Hold" 缓慢消失,播配音 "Now you can breathe normally.",接着播吐气声(后半段)。
    /// 4. 之后每次按住 / 松开都重复第 2、3 步(配音 05 只在开头播一次)。
    /// 水母/海草/发光等视觉效果仍直接跟随扳机(ExperienceController.Breath),不受这里影响。
    /// </summary>
    public sealed class BreathGuide : MonoBehaviour
    {
        public ExperienceController controller;
        public ReefVoiceOver voiceOver;
        public ReefSoundscape soundscape;
        public Transform viewer;
        public Material textMaterial;

        [Header("开始引导的配音(播完后才开始吸气声)")]
        public string promptCueId = "05";

        [Header("松开扳机时的配音")]
        public AudioClip releaseClip;
        public float releaseStart, releaseEnd;
        [Range(0, 8)] public float releaseGain = 1f;
        public string releaseText = "Now you can breathe normally.";

        [Header("Hold 字幕")]
        public string holdText = "Hold";
        [Tooltip("吸气声开始后多少秒出现 Hold")] public float holdDelay = 1f;
        public float fadeInSeconds = 1f;
        public float fadeOutSeconds = 1.5f;
        [Tooltip("离眼睛的距离(米)")] public float distance = .8f;
        public float characterSize = .012f;
        public Color color = new Color(.92f, .98f, 1f, 1f);

        enum State { Idle, Inhaling, ReleaseVoice, }
        State state;
        float inhaleStart, holdAlpha, holdTarget;
        TextMesh text;
        Font font;
        Transform textRoot;

        /// <summary>自检用。</summary>
        public float HoldAlpha => holdAlpha;
        public string StateName => state.ToString();

        void Awake()
        {
            textRoot = new GameObject("Hold subtitle").transform;
            textRoot.SetParent(transform, false);
            text = textRoot.gameObject.AddComponent<TextMesh>();
            text.text = holdText;
            text.fontSize = 100;
            text.characterSize = characterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font;
            var r = textRoot.GetComponent<MeshRenderer>();
            r.sharedMaterial = textMaterial ? textMaterial : font.material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            textRoot.gameObject.SetActive(false);
        }

        void Start() => controller.StageChanged += OnStageChanged;
        void OnDestroy() { if (controller) controller.StageChanged -= OnStageChanged; }

        void OnStageChanged(Stage previous, Stage next)
        {
            state = State.Idle;
            holdTarget = 0f;
            if (next == Stage.Standby) { holdAlpha = 0f; ApplyText(); }
        }

        bool PromptDone => voiceOver == null || voiceOver.CueFinished(promptCueId);

        void Update()
        {
            float dt = Time.deltaTime, t = Time.time;
            bool inScene = controller.Current == Stage.BreathHold;
            bool held = inScene && controller.BreathHeld;

            if (inScene)
            {
                switch (state)
                {
                    case State.Idle:
                        if (held && PromptDone)
                        {
                            if (soundscape) soundscape.PlayInhale();
                            inhaleStart = t; state = State.Inhaling;
                        }
                        break;
                    case State.Inhaling:
                        if (held) { if (t - inhaleStart >= holdDelay) holdTarget = 1f; }
                        else { holdTarget = 0f; saidRelease = false; state = State.ReleaseVoice; }
                        break;
                    case State.ReleaseVoice:
                        // 先说 "Now you can breathe normally."(别的配音正在播就等它播完),说完接吐气声
                        if (!saidRelease)
                        {
                            if (!voiceOver || !releaseClip || voiceOver.Say("release", releaseClip, releaseStart, releaseEnd, releaseGain, releaseText))
                                saidRelease = true;
                            break;
                        }
                        if (voiceOver && voiceOver.Speaking == "release") break;
                        if (soundscape) soundscape.PlayExhale();
                        state = State.Idle;
                        break;
                }
            }

            float speed = holdTarget > holdAlpha ? 1f / fadeInSeconds : 1f / fadeOutSeconds;
            holdAlpha = Mathf.MoveTowards(holdAlpha, holdTarget, dt * speed);
            ApplyText();
        }

        bool saidRelease;

        void ApplyText()
        {
            bool show = holdAlpha > .001f;
            if (textRoot.gameObject.activeSelf != show) textRoot.gameObject.SetActive(show);
            if (!show || !viewer) return;
            var c = color; c.a = Mathf.SmoothStep(0f, 1f, holdAlpha);
            text.color = c;
            if (textMaterial && font && textMaterial.mainTexture != font.material.mainTexture)
                textMaterial.mainTexture = font.material.mainTexture;   // 动态字体贴图可能重建
            textRoot.position = viewer.position + viewer.forward * distance;
            textRoot.rotation = Quaternion.LookRotation(viewer.forward, viewer.up);
        }
    }
}
