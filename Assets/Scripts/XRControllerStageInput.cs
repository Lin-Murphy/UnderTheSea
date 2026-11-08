using UnityEngine;
using UnityEngine.XR;

namespace UnderTheSea
{
    /// <summary>
    /// 助手手持右手柄:A 键(primaryButton)= 下一阶段 / Start;B 键(secondaryButton)= 回到 Start 按钮状态。
    /// 使用 UnityEngine.XR 内置接口,不依赖额外 SDK 包;未连接头显时自动不起作用。
    /// </summary>
    public class XRControllerStageInput : MonoBehaviour
    {
        public ExperienceController controller;
        bool _prevPressed, _prevB;

        void Update()
        {
            if (controller == null) return;
            var dev = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!dev.isValid) { _prevPressed = false; _prevB = false; controller.xrBreathHeld = false; return; }

            // 右手柄扳机按住 = 场景4 吸气/憋气,松开 = 呼气
            controller.xrBreathHeld = dev.TryGetFeatureValue(CommonUsages.trigger, out float trigger) && trigger > .5f;

            // A 键 = 下一阶段(在 Standby 时即 Start)
            if (dev.TryGetFeatureValue(CommonUsages.primaryButton, out bool pressed))
            {
                if (pressed && !_prevPressed) controller.Next();
                _prevPressed = pressed;
            }

            // B 键 = 随时回到 Start 按钮(未点击)状态
            if (dev.TryGetFeatureValue(CommonUsages.secondaryButton, out bool b))
            {
                if (b && !_prevB) controller.ResetToStandby();
                _prevB = b;
            }
        }
    }
}
