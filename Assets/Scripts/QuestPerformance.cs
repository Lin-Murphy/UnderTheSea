using System;
using UnityEngine;

namespace UnderTheSea
{
    /// <summary>
    /// opt 分支(2026-11-06):Quest 3 运行时性能设置,只在真机上生效(编辑器 / Quest Link 不改)。
    /// - 固定注视点渲染(FFR,默认 Medium + 动态):画面边缘降低着色率,视野中心不受影响。
    ///   本项目有 4×MSAA、大量半透明粒子与光柱,GPU 是主要瓶颈,FFR 是 Meta 推荐的最便宜的节省方式。
    /// - CPU / GPU 性能档位(默认 SustainedHigh):场景3 的 AprilTag 检测在 CPU 上跑,高档位让它更快完成,
    ///   检查时长 10–20 分钟,持续高档位的发热在可接受范围内。
    /// - 刷新率:0 = 系统默认(Quest 3 为 72 Hz);填 90 / 120 时若设备支持就切换。
    /// 启动时在 logcat 打印一行 "[QuestPerformance] …",方便确认是否生效。
    /// </summary>
    public sealed class QuestPerformance : MonoBehaviour
    {
        public OVRManager.FoveatedRenderingLevel foveation = OVRManager.FoveatedRenderingLevel.Medium;
        [Tooltip("让系统按 GPU 负载在 Off..上面的等级之间自动调整")] public bool dynamicFoveation = true;
        public OVRManager.ProcessorPerformanceLevel cpuLevel = OVRManager.ProcessorPerformanceLevel.SustainedHigh;
        public OVRManager.ProcessorPerformanceLevel gpuLevel = OVRManager.ProcessorPerformanceLevel.SustainedHigh;
        [Tooltip("0 = 系统默认刷新率")] public float refreshRate = 0f;

        void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var log = new System.Text.StringBuilder("[QuestPerformance]");
            Try(log, "FFR", () =>
            {
                if (!OVRManager.fixedFoveatedRenderingSupported) return "unsupported";
                OVRManager.foveatedRenderingLevel = foveation;
                OVRManager.useDynamicFoveatedRendering = dynamicFoveation;
                return $"{OVRManager.foveatedRenderingLevel}{(dynamicFoveation ? " (dynamic)" : "")}";
            });
            Try(log, "CPU/GPU", () =>
            {
                OVRManager.suggestedCpuPerfLevel = cpuLevel;
                OVRManager.suggestedGpuPerfLevel = gpuLevel;
                return $"{cpuLevel}/{gpuLevel}";
            });
            Try(log, "Hz", () =>
            {
                var display = OVRManager.display;
                if (display == null) return "no display";
                if (refreshRate > 0f && Array.IndexOf(display.displayFrequenciesAvailable, refreshRate) >= 0)
                    display.displayFrequency = refreshRate;
                return display.displayFrequency.ToString("0");
            });
            Debug.Log(log.ToString());
#endif
        }

        static void Try(System.Text.StringBuilder log, string label, Func<string> action)
        {
            try { log.Append(' ').Append(label).Append('=').Append(action()); }
            catch (Exception e) { log.Append(' ').Append(label).Append("=failed(").Append(e.Message).Append(')'); }
        }
    }
}
