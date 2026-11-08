using UnityEngine;
using UnityEngine.XR;

namespace UnderTheSea
{
    /// <summary>
    /// Meta Quest Passthrough 的薄封装。场景里的 OVRCameraRig 带 OVRManager(开启 Passthrough)
    /// 与一个 Underlay 的 OVRPassthroughLayer。转场时只需把相机背景 alpha 设为 VR 显现度:
    /// alpha=0 完全透出现实,alpha=1 完全是虚拟场景。
    /// 编辑器/未戴头显时 IsLive=false,表现层改用灰色占位代表现实。
    /// </summary>
    public class PassthroughBridge : MonoBehaviour
    {
        public OVRManager manager;
        public OVRPassthroughLayer layer;

        public bool IsLive =>
            layer != null && manager != null && manager.isInsightPassthroughEnabled && XRSettings.isDeviceActive;

        void Start()
        {
            if (layer != null) layer.hidden = false;
        }
    }
}
