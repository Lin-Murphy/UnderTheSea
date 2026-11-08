using UnityEngine;

namespace UnderTheSea
{
    /// <summary>
    /// 现实世界(Standby)里显示在用户面前的 Start 按钮。
    /// 点击(编辑器:鼠标左键点击按钮)、空格或右手柄 A 键都会开始体验。
    /// 只在 Standby 且 VR 已完全退去时显示;离开 Standby 后隐藏。
    /// 按钮外观在运行时用一个方块 + TextMesh 生成,材质由 Builder 提供。
    /// </summary>
    public class StartButton : MonoBehaviour
    {
        public ExperienceController controller;
        public OceanStageView view;
        public Camera cam;
        public Material buttonMaterial;
        public float distance = 1.5f;

        Transform _root;
        Renderer _renderer;
        Collider _collider;
        bool _wasShown;
        float _pulse;
        Color _idle = new Color(0.15f, 0.65f, 0.85f), _hover = new Color(0.35f, 0.85f, 1f);

        void Awake()
        {
            _root = new GameObject("StartButtonVisual").transform;
            _root.SetParent(transform, false);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(_root, false);
            body.transform.localScale = new Vector3(0.5f, 0.2f, 0.1f);
            _renderer = body.GetComponent<Renderer>();
            if (buttonMaterial != null) _renderer.sharedMaterial = buttonMaterial;
            _collider = body.GetComponent<Collider>();

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(_root, false);
            textGo.transform.localPosition = new Vector3(0, 0, -0.07f);
            var tm = textGo.AddComponent<TextMesh>();
            tm.text = "START";
            tm.fontSize = 100;
            tm.characterSize = 0.04f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            textGo.GetComponent<MeshRenderer>().sharedMaterial = font.material;

            _root.gameObject.SetActive(false);
        }

        void Update()
        {
            bool show = controller.Current == Stage.Standby && view != null && view.VrAmount <= 0.05f;

            if (show && !_wasShown) Place();
            if (show != _wasShown) _root.gameObject.SetActive(show);
            _wasShown = show;
            if (!show) return;

            // 始终面向用户
            Vector3 toUser = _root.position - cam.transform.position;
            if (toUser.sqrMagnitude > 1e-4f) _root.rotation = Quaternion.LookRotation(toUser);

            // 悬停高亮 + 轻微呼吸动画
            bool hovering = Hovering();
            _pulse += Time.deltaTime;
            float s = 1f + Mathf.Sin(_pulse * 2.5f) * 0.03f;
            _root.localScale = Vector3.one * s;
            _renderer.material.color = hovering ? _hover : _idle;

            if (hovering && Input.GetMouseButtonDown(0)) controller.Next();
        }

        void Place()
        {
            // 用水平朝前方向:珊瑚视角会低头看自己,按钮仍放在正前方
            Vector3 f = view != null ? view.FlatForward() : cam.transform.forward;
            _root.position = cam.transform.position + f * distance;
            _pulse = 0f;
        }

        bool Hovering()
        {
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            return _collider.Raycast(ray, out _, 20f);
        }
    }
}
