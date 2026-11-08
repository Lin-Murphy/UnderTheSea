# UnderTheSea — 超声检查儿童安抚 AR/VR (Quest 3) Unity 项目

> 本文件是项目的总纲与**当前状态汇总**(2026-11-05 整理),Claude 每次开发前必读。
> 逐轮修改的完整历史记录在 `Documentation/CLAUDE_history_2026-11-05.md`(需要追溯某个决定时再看)。
> **工作方式:用户逐条提需求,做完一批先汇报、等确认再继续。不要擅自提交(check in)到版本库。**
> 美术迁移授权(2026-11-05):用户已授权接入 `Ultrasound_Under the Sea` 协作项目的全部美术、元素与动画并替换占位效果(细节见 `Documentation/Migration-2026-11-05/README.md`)。
> **下一个对话先看第 9 节 TODO。**

## 1. 项目简介

面向**超声检查中的儿童患者**的情绪安抚体验,AR→VR→AR,运行于 **Meta Quest 3**。

- 患者躺在病床上,**只戴 Quest 3 头显**,无手柄。参与者在故事中**就是一株珊瑚**(第一人称,低头能看到自己)。镜像珊瑚已删除,不要加回。
- 三个角色:**患者**(戴头显)、**医生**(操作超声探头,探头上贴 AprilTag)、**医生助手**(拿右手柄切换场景、控制呼吸)。
- 整个体验在**同一个场景** `Assets/Scenes/Main.unity` 里通过状态切换完成。

## 2. 环境与版本库

- Unity **6000.3.27f2**,URP **17.3.0**(管线资源 `Assets/Settings/UnderTheSea/Quest_URP.asset`,**HDR 关闭**,否则 Passthrough 会蒙一层蓝),Meta XR SDK all **207.0.1**,OpenXR **1.19.0**(不要改成 1.16.1),输入处理 Both。
- 版本库:**Unity Version Control(Plastic)**,工作区在 `/main`。`/main/demo1` 是参考分支(只读,鱼的模型/游动来自这里)。本轮所有修改**都还没有提交**。
- 构建目标 Android;APK 输出 `Builds/UnderTheSea.apk`;Quest 包名 `com.DefaultCompany.UnderTheSea`。adb:`C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`。
- 编辑器里的 Unity MCP 可以编译、执行编辑器脚本、构建,但**不能进入 Play 模式**;运行时效果需要用户点 Play 或在头显里看(Claude 可以用编辑器内逐帧模拟 + 相机渲染截图做验证)。

## 3. 如何重建场景 / 验证

- **Main 由脚本生成**:菜单 `UnderTheSea > Migration > Build Main With Imported Reef Art`(`Assets/Editor/BuildReefMainScene.cs`)。它以 `Assets/Scenes/UnderwaterExamPreview.unity` 的美术为底,加上所有交互、音效、配音并保存 Main,最后自动做静态检查(`ReefMigrationChecks.ValidateSavedScene` → `Documentation/Migration-2026-11-05/editor-validation.json`)。**手改 Main 会在下次重建时被覆盖,改动要写进 Builder。** Play 时不能重建。
- Play 模式流程检查:菜单 `UnderTheSea > Migration > Run Complete Flow Smoke Check`(结果 `flow-validation.json`,已按最新流程更新,但一直没能运行过)。
- AprilTag 自检(不用头显):`UnderTheSea > AprilTag > Self Test (synthetic frame)`。
- 编辑器预览:`UnderTheSea > XR > Play In Headset (Quest Link)` 默认勾选。连着 Quest Link 时头显 + Game 视图同步预览;没有头显时 `XRLinkFallback` 自动退回桌面预览(避免 OVRManager 停止 Play)。
- 旧占位场景生成器 `UnderTheSea > Legacy > Build Placeholder Preview` 只输出 `LegacyStep1Preview.unity`,**不要改回覆盖 Main**。

## 4. 操作

| 输入 | 作用 |
|---|---|
| 右手柄 **A** / 空格 / 点击 Start 按钮 | 开始 / 下一阶段 |
| 右手柄 **B** / 键盘 B 或 R | 随时回到 Start(现实) |
| 右手柄 **扳机按住** / 键盘 **F** | 场景4 吸气/憋气,松开 = 呼气(3 秒升/降) |
| Backspace | 上一阶段(调试) |
| 鼠标 左键 / 右键 / 中键 | 编辑器模拟 Tag 1 / 0 / 2 |
| Alt+左键拖动、Home | 编辑器转头、复位 |
| 键盘 **T** | 开关 tag 测试小黄点(默认开,`SoftSeaweedStrands.showTagMarker`) |

## 5. 体验流程(当前实现)

阶段:`Standby → Enter → ApplyGel → Probe → DuskTransition → BreathHold → ExhaleTransition → (Finale 跳过) → Exit → Finished → Standby`

| 阶段 | 内容 |
|---|---|
| **Standby** | 现实(Passthrough),面前 Start 按钮;背景 (0,0,0,0) 透出现实 |
| **1 Enter** | 沙丁鱼风暴:60 条小鱼从四面八方螺旋游来绕着我转(不挡视野),约 2.5s 后海底从近到远长出来,约 7s 后鱼群散开 |
| **2 ApplyGel 涂耦合剂** | 无追踪。"我这一组"小鱼(12 条身体小鱼 + 约 30% 环境鱼)游进我(主角珊瑚)体内的空隙躲着 |
| **3 Probe 探头检查** | 小鱼离开我回到正常。柔软海草随浪抚摸身体,**跟随探头 Tag 1**,有按压感;**小丑鱼**代替原来的 tag 小黄点,在海草里游、拨开海草:tag 在控制区内时约 0.3s 追上 tag,tag 丢失/离开区域时自然随机游动 |
| **DuskTransition** | 天黑到白天的 50%;7 只水母(近处 4 只含左前方新增 + demo1 远处 3 只,正前方那只已下移 1m)以"收缩推进、滑行下沉"一上一下游来;小鱼:我这一组躲回我体内,其余约 80% 躲进附近 10 株珊瑚的空隙,一条"天敌"大鱼缓慢巡游;海草回到正常 |
| **4 BreathHold** | 保持天黑、水母环绕不发光;全图缓慢闪烁的粒子。**按住扳机吸气**:我变大 1.35 倍、海草螺旋包裹我、水母放大到 1.45 倍并发光(亮度已减半)+ 光晕与颗粒、珊瑚/海葵/我的尖端发微光 + 颗粒、闪烁粒子更亮更快;**松开**3 秒恢复;可反复 |
| **4·5 ExhaleTransition** | 按 A 后天亮,全部水母约 14s 内游走消失(不留一只,握手环节已删除);不自动前进,再按 A 退出 |
| **6 Exit** | 风暴鱼群聚拢 → 海底反向退去 → 鱼群螺旋散开回到现实 → Finished 3 秒 → Standby |

环境:海平面在光柱顶部上方 4.5m(起伏 + 水纹光);天空竖直渐变(抬头浅海青蓝 → 地平线 = 水雾色 → 低头深海蓝),远处无缝融入水色;海葵 10 只(demo1 布局);相机在主角珊瑚上方约 0.55m、稍靠后、45° 向下(OVRManager 追踪原点 EyeLevel)。

## 6. AprilTag(真机检测,从 `C:\Users\47632\3D-101` 移植)

- 标记族 **tagStandard41h12**(不是 36h11)。打印含白边 5×5 cm,检测尺寸 0.02777778 m。打印页在 `Documentation/AprilTag/`(`PrintProbeTag.html` = ID 1)。
- **当前只用 ID 1(探头)**:场景3 海草 + 小丑鱼跟随它(`SoftSeaweedStrands.trackTagId = 1`)。ID 0(水草道具)、ID 2(水母玩具)暂不使用。
- 链路:`PassthroughCameraAccess`(左相机)→ 内嵌包 `Packages/jp.keijiro.apriltag`(1.0.3,带标定内参改动)→ `QuestAprilTagPoseSource`(只在 Probe 检测,10Hz)→ `CompositeTagPoseSource`(真机优先,编辑器鼠标模拟兜底)。
- 权限:`horizonos.permission.HEADSET_CAMERA`(`Assets/Plugins/Android/AndroidManifest.xml`)、OVRProjectConfig Passthrough Camera Access 已开;设备上权限已授予。合成画面自检通过;**真机识别与跟随尚未确认**。
- 未做:患者躺姿下"真实肚子 ↔ 虚拟珊瑚"标定;tag 位置目前被限制在珊瑚身体周围的圆柱区域内。

## 7. 主要脚本(`Assets/Scripts`)

| 脚本 | 作用 |
|---|---|
| `ExperienceController` | 状态机、呼吸值 `Breath`(扳机/F) |
| `KeyboardStageInput` / `XRControllerStageInput` | 键盘 / 右手柄输入 |
| `OceanStageView` | 总表现层:AR↔VR 显现、天黑、主角珊瑚、转场风暴鱼、调用各子系统 |
| `ReefArtDriver` | 水母游动/发光/呼吸缩放、夜色灯光 |
| `ReefFishSchool` | demo1 鱼群轨道游动、躲进珊瑚/我体内、天敌 |
| `SoftSeaweedStrands` (+`Shaders/SoftSeaweed.shader`) | 24 根 Verlet 软体海草:抚摸、按压、包裹、被小丑鱼拨开 |
| `TagClownfish` | 小丑鱼(`Assets/Models/Clownfish`,Swim_Loop 动画) |
| `BreathGlow` (+`Shaders/BreathSparkle.shader`) | 尖端发光、颗粒、夜间闪烁粒子、水母光晕/颗粒 |
| `ReefSoundscape` | 8 个音效(`Assets/Audio`,原中文文件名),含 `duck` 压低 |
| `ReefVoiceOver` | 旁白 cue 表(`Assets/Audio/VO`) |
| `QuestAprilTagPoseSource` / `CompositeTagPoseSource` / `MockTagPoseSource` / `TagIds` | Tag 位姿 |
| `PassthroughBridge` / `XRLinkFallback` / `StartButton` / `StageHud` / `ReefDesktopCamera` | Passthrough、编辑器 XR 兜底、Start 按钮、HUD、桌面视角 |

其它:`Shaders/SeaSurface.shader`(海平面);`UltrasoundReef/CollabArt/Shaders/CollabCoral.shader`(尖端发光 `_ReefBreathGlow`)、`CollabJelly.shader`(水母发光);`UltrasoundReef/Shaders/UnderwaterSky.shader`(深浅渐变)。编辑器:`Assets/Editor/XRPlayModeToggle.cs`、`AprilTagSelfTest.cs`、`CollabArtSwap.cs`、`ReefMigrationChecks.cs`。

## 8. 音效与配音

- **音效**(`ReefSoundscape`,全部 2D):泡泡声常驻(随 VR 显现);进入 VR 后开场背景乐 → 播完接"播放完背景乐和开场词";每结束一个场景(按 A)播"每结束一个场景";涂耦合剂/天黑开始播"小鱼靠近珊瑚";场景4 憋气背景音循环;扳机按住播"憋气时声音"前半段、松开播后半段;退场播"退场开始进入ar世界"。音量全部 1;每个音效有自己的增益(短音效由 Builder 按录音响度自动算到约 −20 dB,长音轨用实测值:泡泡 ×2.9、开场乐 ×1.3、憋气背景 ×2.5),再乘**总增益 `masterGain` = 1.5**(Quest 自带喇叭偏小,电脑上会偏大)。增益由 `AudioGain`(OnAudioFilterRead,峰值软限幅)实现。配音时开场音乐压到 50%,环境声 85%。用户试听后单独调低(音量字段):"播放完背景乐和开场词" 0.25、"每结束一个场景" 0.25、憋气背景音 0.25、憋气时声音 0.7、退场音效 0.5。**Quest 测试必须先关掉 Quest Link**(Link 开着时回到应用会没声音,只有打开系统菜单时才有)。
- **配音 VO**(`ReefVoiceOver`):`Assets/Audio/VO/VO_01…08`(03、04 为新录音),已按《Voiceover script 确定版》匹配(台词与时机见 `Assets/Audio/VO/VO_script.txt`,cue 表里也存了 text)。01 Enter 显现后 1s;02 涂胶;03 探头第一次碰到 tag(3s 兜底)播 `VO_03`(2026-11-05 晚的新录音,台词加了小丑鱼;旧的 03.1/03.2 不再使用);04 天黑(`VO_04` 已换成新录音:小鱼躲进珊瑚枝间);05 进入场景4;06 按句间停顿切成 4 段跟着扳机播(cue 的 clipStart/clipEnd):06a = 05 播完 + 第一次按扳机后 2s(20s 兜底)、06b = 之后第一次松开扳机、06c = 06b 播完 5s、06d = 第二次松开扳机(新触发 `ExhaleAfterCue`);07 天亮;08 退场,回到现实后继续播完(只有按 B 中断)。每条按响度自动增益到约 −20 dB(原始 −27~−31 dB,之前被同时播放的开场音乐盖住)。配音的增益同样乘总增益 `masterGain`。触发时控制台打印 `[VO] id: 台词`。

## 9. TODO(下一个对话从这里开始)

1. ✅(2026-11-05 晚)tag 小黄点已恢复:显示真实 tag 位置,与小丑鱼同时显示,键盘 T / `showTagMarker` 开关。待 Play / 真机确认。
2. ✅ VO 已按英文 script 匹配(见第 8 节),新增触发 `FirstTagContact`、cue 的 `fallback`(兜底秒数)和 `gain`。待用户在 Play / 头显里听确认音量与时机。
3. ✅ 背景音偏小的原因:泡泡原始录音 −37 dB,又乘 0.45 音量和 0.45 压低 ≈ −51 dB。已改为音量 1 + 增益 ×2.5,配音时只压到 80%。待用户试听,可在 `ReefSoundscape` Inspector 调 `bubblesGain` / `musicDuck` / `ambienceDuck`(改完要写回 Builder)。
4. ✅ 21:13 已重新构建并安装到 Quest(含 TODO 1–3 + 总增益 1.5)。之前 20:40 装的版本是旧音量逻辑(配音 −30 dB、配音时背景压到 45%),所以在头显里很小声。**待用户在头显里确认音量和 VO 时机**;还小就调 `ReefSoundscape.masterGain`(Builder 里 `sound.masterGain`),重建 + 重新打包。
   以下为构建步骤备忘:
   - 构建:编辑器脚本 `BuildPipeline.BuildPlayer`(场景 `Assets/Scenes/Main.unity`,目标 Android,输出 `Builds/UnderTheSea.apk`;Play 模式下不能构建,约 4–5 分钟)。
   - 安装:`adb install -r Builds/UnderTheSea.apk`(adb 路径见第 2 节),启动 `adb shell monkey -p com.DefaultCompany.UnderTheSea -c android.intent.category.LAUNCHER 1`,用 `adb logcat -s Unity` 查错误。
   - 确认 APK 内含 `horizonos.permission.HEADSET_CAMERA`(aapt dump permissions),然后请用户戴头显验证。
5. (延续)真机验证:AprilTag ID 1 识别与海草/小丑鱼跟随、Passthrough 颜色、按 A 后海底显现、整体性能;患者躺姿标定。
6. (延续)所有改动尚未提交到 Plastic `/main`,由用户决定何时提交。

## 10. 已知问题 / 注意

- 控制台常驻一条 Meta AgentBridge 端口 48735 报错,与项目无关。
- **Meta XR Operator**(`Window > Meta > Meta XR Operator`)若被重新激活,会让 Quest Link 进不去头显(注入 OpenXR API 层)。已 Deactivate;再出问题先查这里。
- `BuildReefMainScene` 每次重建会重新写入 URP 设置、项目设置、烘焙网格等文件(版本库里会显示改动)。
- 编辑器里 Unity 对象不能用 `??` 判空(假 null),Builder 里已踩过两次坑。
- 历史文档中关于镜像珊瑚、握手/触手、Tag 0 水草、Tag 2 水母、场景5 独立阶段的描述都已过时,以本文件为准。
