using UnityEngine;

namespace UnderTheSea
{
    /// <summary>
    /// 开发期输入:空格 = 下一阶段,Backspace = 上一阶段,R = 回到待机。
    /// 之后会新增手柄按钮输入组件来替换/补充,状态机本身不需要改动。
    /// </summary>
    public class KeyboardStageInput : MonoBehaviour
    {
        public ExperienceController controller;

        void Update()
        {
            if (controller == null) return;
            if (Input.GetKeyDown(KeyCode.Space)) controller.Next();
            if (Input.GetKeyDown(KeyCode.Backspace)) controller.Previous();
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.B)) controller.ResetToStandby();   // B 与手柄 B 键一致
            controller.keyboardBreathHeld = Input.GetKey(KeyCode.F);   // 按住 F = 右手柄扳机(场景4 吸气/憋气)
            if (Input.GetKeyDown(KeyCode.T))   // T = 开关 tag 测试小黄点
                foreach (var w in FindObjectsByType<SoftSeaweedStrands>(FindObjectsSortMode.None)) w.showTagMarker = !w.showTagMarker;
        }
    }
}
