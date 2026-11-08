using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;

/// <summary>
/// 编辑器里点 Play 时是否启动 XR(Quest Link)。默认开启:
/// - 头显通过 Quest Link 连接时:头显里是 VR,Game 视图同步显示头显画面。
/// - 没有头显时:运行时组件 UnderTheSea.XRLinkFallback 会在 OVRManager 停止 Play 之前接管,
///   关闭 XR,自动退回桌面预览(键盘/鼠标模拟)。所以不需要手动切换。
/// 取消勾选菜单 UnderTheSea > XR > Play In Headset (Quest Link) = 只用桌面预览(完全不启动 XR,启动更快)。
/// Android(Quest 真机构建)的设置不受影响。
/// </summary>
[InitializeOnLoad]
public static class XRPlayModeToggle
{
    const string MenuPath = "UnderTheSea/XR/Play In Headset (Quest Link)";

    static XRPlayModeToggle()
    {
        EditorApplication.delayCall += () => Menu.SetChecked(MenuPath, Enabled);
        EditorApplication.playModeStateChanged += change =>
        {
            if (change == PlayModeStateChange.EnteredPlayMode && !Enabled)
                Debug.Log("[UnderTheSea] 桌面预览:本次 Play 不启动 XR(用键盘/鼠标模拟)。要同时在头显里预览,勾选菜单 " + MenuPath + "。");
        };
    }

    static XRGeneralSettings Standalone => XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);

    /// <summary>编辑器 Play 时是否会启动 XR(有头显就同时在头显里预览,没有就自动退回桌面)。</summary>
    public static bool Enabled => Standalone && Standalone.InitManagerOnStart;

    [MenuItem(MenuPath)]
    static void Toggle() => SetEnabled(!Enabled);

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate() { Menu.SetChecked(MenuPath, Enabled); return !EditorApplication.isPlayingOrWillChangePlaymode; }

    public static void SetEnabled(bool enabled)
    {
        var settings = Standalone;
        if (!settings) { Debug.LogWarning("[UnderTheSea] 找不到 Standalone 的 XR 设置。"); return; }
        settings.InitManagerOnStart = enabled;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Menu.SetChecked(MenuPath, enabled);
        Debug.Log(enabled
            ? "[UnderTheSea] 编辑器 Play 将启动 XR:头显连着 Quest Link 时头显与 Game 视图同步预览;没有头显时自动退回桌面预览。"
            : "[UnderTheSea] 编辑器 Play 只用桌面预览(不启动 XR)。");
    }
}
