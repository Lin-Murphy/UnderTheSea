using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace UnderTheSea
{
    /// <summary>
    /// 编辑器 Play 时"头显 + 桌面同时预览,没有头显就自动退回桌面预览"。
    ///
    /// - 戴着头显、Quest Link 已连接:XR 正常启动,头显里是 VR,Game 视图同步显示头显画面(Unity 的 XR 镜像)。
    /// - 没有通过 Link 连接头显:Meta 的 OpenXR 运行时会让会话立刻进入 EXITING,OVRPlugin.shouldQuit 变为 true,
    ///   OVRManager 原本会在自己的 Update 里直接停止 Play。本组件执行顺序最早(早于 OVRManager 的 -100),
    ///   在同一帧先检测到 shouldQuit:停用 OVRManager、关闭 XR,Play 继续以桌面预览运行(键盘/鼠标模拟)。
    /// 只在编辑器里生效;Quest 真机上运行时要求退出时照常退出。
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    public sealed class XRLinkFallback : MonoBehaviour
    {
        static bool fellBack;

        /// <summary>本次 Play 是否已因为没有头显而退回桌面预览。</summary>
        public static bool FellBackToDesktop => fellBack;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            fellBack = false;
            var go = new GameObject("XR Link fallback (editor)") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<XRLinkFallback>();
        }

        void Update()
        {
            if (fellBack || !OVRPlugin.shouldQuit) return;
            fellBack = true;

            // 1. 停用 OVRManager,让它这一帧及以后都不再执行"停止 Play"
            foreach (var manager in FindObjectsByType<OVRManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                manager.enabled = false;

            // 2. 关闭 XR,回到普通的桌面相机渲染
            var xr = XRGeneralSettings.Instance;
            if (xr != null && xr.Manager != null && xr.Manager.isInitializationComplete)
            {
                xr.Manager.StopSubsystems();
                xr.Manager.DeinitializeLoader();
            }

            Debug.Log("[UnderTheSea] 没有检测到通过 Quest Link 连接的头显,已自动切换为桌面预览(键盘/鼠标模拟)。" +
                      "要在头显里预览:先在头显中开启 Quest Link,再重新点 Play。");
        }
#endif
    }
}
