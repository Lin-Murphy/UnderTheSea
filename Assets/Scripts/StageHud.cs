using UnityEngine;

namespace UnderTheSea
{
    /// <summary>开发期屏幕提示(IMGUI),显示当前阶段与按键说明。VR 中不可见,仅用于编辑器调试。</summary>
    public class StageHud : MonoBehaviour
    {
        public ExperienceController controller;
        GUIStyle _big, _small;

        void OnGUI()
        {
            if (controller == null) return;
            if (_big == null)
            {
                _big = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                _big.normal.textColor = Color.white;
                _small = new GUIStyle(GUI.skin.label) { fontSize = 14 };
                _small.normal.textColor = new Color(1, 1, 1, 0.85f);
            }

            var info = controller.Info;
            string head = info.MainIndex > 0 ? $"场景 {info.MainIndex}/6  {info.Zh}" : info.Zh;
            GUI.Label(new Rect(16, 12, 900, 34), head, _big);
            GUI.Label(new Rect(16, 44, 900, 24), info.En, _small);

            if (info.IsTransition)
            {
                float left = Mathf.Max(0, info.AutoAdvance - controller.TimeInStage);
                GUI.Label(new Rect(16, 66, 900, 24), $"过渡中,{left:0.0}s 后自动进入下一阶段(空格可跳过)", _small);
            }

            string hint = null;
            switch (controller.Current)
            {
                case Stage.ApplyGel: hint = "小鱼游进我(珊瑚)体内躲起来,无需追踪"; break;
                case Stage.Probe:
                    hint = "按住鼠标左键 = 模拟探头 Tag 1,海草跟随并被按压(松开则默认海浪)"; break;
                case Stage.BreathHold: hint = "按住右手柄扳机 / F 键 = 吸气憋气(我变大1.35倍、海草包裹、水母变大发光、尖端发光),松开 = 呼气;可反复;按 A = 天亮水母游走"; break;
            }
            if (hint != null) GUI.Label(new Rect(16, 88, 900, 24), hint, _small);

            GUI.Label(new Rect(16, Screen.height - 32, 900, 24),
                "[Space / 右手柄A / 点击Start] 开始·下一阶段   [Backspace] 上一阶段   [B / 右手柄B / R] 回到Start   [Alt+左键拖动] 转头 [Home] 复位", _small);
        }
    }
}
