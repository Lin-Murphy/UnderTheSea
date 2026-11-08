# UnderTheSea 重做 · 冷启动文档(v2,2026-11-06)

> **用途**:让一个新的 AI 对话 / 新 Unity 工程**从 0 重做**「超声检查儿童安抚 AR/VR(Quest 3)」体验。
> **本文规定的**:目标、用户与现场、硬约束、核心思想、体验节拍、技术栈、踩过的坑、可复用资产、路线图。
> **本文不规定的**:具体玩法、画面、动画、交互细节。第 4 节的「旧版做法」只是参考,**不要照抄**,鼓励重新设计。
> **用法**:新建工程后把本文件复制为工程根目录的 `CLAUDE.md`;新对话第一条消息贴第 0 节的 Prompt。
> **旧工程**:`C:\Users\47632\UnderTheSea`(Unity Version Control / Plastic `/main`)。可复用的资产都在这里(第 10 节);旧版逐轮历史见旧工程 `Documentation/CLAUDE_history_2026-11-05.md`(一般不需要看)。

---

## 0. 开场 Prompt(复制给新的 AI)

```text
你将和我一起从 0 重做一个 Unity 项目「UnderTheSea」:给正在做腹部超声检查的儿童戴的 Meta Quest 3 安抚体验(现实 → 海底 → 现实,AR→VR→AR)。
请先完整阅读工程根目录的 CLAUDE.md(冷启动文档)。

规则:
1. 第 2 节(用户、现场、硬约束)、第 3 节(核心思想)、第 8 节(技术坑)必须遵守。
   第 4 节「旧版做法」只是参考:玩法、画面、动画、交互请你重新设计,可以更好、更简单或完全不同,
   但第 4 节列出的每个「体验节拍」都要有,每个节拍「必须传达」的感受都要做到。
2. 先不要写代码。第一步请给我:
   ① 你对核心思想的理解(5 句以内);
   ② 你的创意方案:每个节拍的画面 / 动画 / 交互 / 声音设计,和旧版相比改了什么、为什么;
   ③ 技术架构草图(脚本划分、数据流、怎么在不能进 Play 模式时做验证);
   ④ 第一个里程碑的范围和验收标准;
   ⑤ 你需要我确认的问题(不超过 5 个)。
   等我确认后再动手。
3. 之后按第 12 节路线图逐个里程碑推进。每完成一个先汇报:做了什么、怎么验证的、
   哪些只是编译通过 / 编辑器模拟通过 / 还需要我在头显里确认。等我确认再继续。
4. 不要擅自提交版本库;不要引入付费或联网服务;不要删除我提供的素材。
```

---

## 1. 一句话

让躺在检查床上的孩子戴上 Quest 3,在检查期间**变成海底的一株珊瑚**;检查中会发生的每一种身体感受(冰凉的耦合剂、探头的移动和按压、医生要求的吸气憋气)都被**提前翻译成海底故事里一个温柔的事件**,从而降低紧张、提高配合度。体验从现实开始,到现实结束。

---

## 2. 用户、现场与硬约束(不可改)

### 2.1 三个角色

| 角色 | 在做什么 | 和系统的关系 |
|---|---|---|
| **患者(儿童)** | 躺 / 半躺在检查床上,**只戴 Quest 3,无手柄** | 完全被动;只有头部转动;不需要学习任何操作 |
| **医生** | 在患者腹部操作超声探头,**探头上贴 AprilTag(ID 1)** | 不碰头显和手柄;系统通过头显的 Passthrough 相机看到探头上的 tag |
| **医生助手** | 站在旁边,手持**右手柄**(左手柄空置) | 唯一操作者:开始、推进阶段、控制呼吸、随时退出 |

### 2.2 硬约束

- **设备**:Meta Quest 3(需要 Passthrough 和 Passthrough Camera API,只有 Quest 3 / 3S 支持)。APK 独立运行,不依赖电脑。
- **单场景**:整个体验在**一个 Unity 场景**内通过状态切换完成,不加载新场景(避免黑屏)。
- **从现实开始、到现实结束**:戴上头显先看到真实房间(Passthrough)和一个开始入口;结束后回到真实房间,患者可以安心摘头显。进出都要有柔和过渡,不能硬切。
- **节奏由助手控制**:临床步骤之间**不自动前进**(检查时长不确定);只有纯转场可以按时长自动前进。
- **随时安全退出**:任何阶段按 B,1–2 秒内回到现实。
- **呼吸**:助手**按住扳机 = 吸气 / 憋气,松开 = 呼气**;可以反复多次;没有倒计时,没有"失败"。
- **探头跟随**:用 AprilTag 追踪真实探头,虚拟世界里要有东西跟随它;**追踪不到时必须有自然的默认动画**(不能卡住、消失或乱跳)。
- **舒适与安全**:不吓人、不强闪、不突然变大声;**不移动相机**(只动世界);任何物体不贴近眼睛(旧版保持 ≥ 1.6 m);Quest 3 上帧率稳定(≥ 72 fps)。
- **语言**:旁白是英文(已录好,第 11 节);调试 HUD / 日志可以用中文。

---

## 3. 核心思想(设计支柱,保留精神,不限形式)

1. **你就是一株珊瑚**。第一人称,看向自己身体的方向能看到"自己"——一株珊瑚。检查发生在你身上,故事也发生在这株珊瑚身上。(旧版试过在对面放一株"镜像珊瑚"代表患者,已被否决,不要再用。)
2. **感受翻译**。每个临床动作都由故事预告、解释、包装:

   | 临床动作 | 孩子的真实感受 | 旧版的翻译(仅参考) |
   |---|---|---|
   | 戴头显、开始 | 陌生、不安 | 鱼群从现实房间里游来,把海底"带"过来 |
   | 涂耦合剂 | 冰凉、湿、突然 | 一道温柔的海浪;小鱼来打招呼,躲进你的枝杈 |
   | 探头移动、按压 | 被按、被移动 | 海草轻抚身体并被按压摊开;小丑鱼跟着探头在你身上游 |
   | 吸气、憋气 | 紧张、难坚持 | 天黑、水母围过来;吸气时水母和你一起"长大、发光";松开慢慢恢复 |
   | 结束、摘头显 | 突然回到现实 | 天亮、水母游走,鱼群把海底带走,回到房间 |

3. **现实 ↔ 海底的桥**。孩子看见海底在房间里"长出来",结束时"退回去"。
4. **呼吸是高潮**。憋气是最难配合的环节,所以反馈要最强、最美、**最即时**(按下扳机的同一帧就开始变化),并且可以反复。
5. **助手是隐形导演**。推进权永远在助手手里;系统不替医生决定时机;任何时候都能安全退出。
6. **追踪不完美也要好看**。真机 tag 会丢、会跳;跟随物要平滑、有"自己的生命",丢失时自然切换到自主行为。
7. **美术基调**:温柔、卡通,但不要太脱离现实。声音以海洋环境声 + 轻柔音乐 + 旁白为主。

---

## 4. 体验节拍(必须有;表现方式自由)

阶段顺序(旧版枚举):`Standby → Enter → ApplyGel → Probe → DuskTransition → BreathHold → Dawn → Exit → Finished → Standby`

| # | 节拍 | 临床对应 | 推进 | 必须传达 | 旧版做法(**仅供参考,不必照抄**) |
|---|---|---|---|---|---|
| 0 | **待机** | 戴头显前后 | A 开始 | 看到真实房间 + 开始入口;患者不用操作 | Passthrough + 面前一个 Start 按钮 |
| 1 | **进入** | 检查准备 | A 下一步 | 海底在房间里出现、包围患者 | 60 条沙丁鱼从四面八方螺旋游来绕着我转(不挡视野);约 2.5 s 后海底从近到远长出来;约 7 s 后鱼群散开 |
| 2 | **涂耦合剂** | 医生涂胶 | A | 冰凉被解释成一件温柔的事 | 一组小鱼游进主角珊瑚的枝杈空隙里躲着 |
| 3 | **探头检查** | 探头在腹部移动、按压 | A | 探头的位置和压力被"看见"、被翻译 | 24 根 Verlet 软体海草随浪抚摸身体,跟随探头 tag 并在按压处被压扁摊开;小丑鱼约 0.3 s 追上 tag、在海草里拨开海草穿行;丢失时随机游 |
| 3→4 | **天黑过渡** | 准备呼吸 | 自动(约 4 s) | 情绪降下来,进入安静的夜晚 | 光线降到白天的 50%;7 只水母以"收缩推进、滑行下沉"游来;小鱼躲回珊瑚;一条"天敌"大鱼缓慢巡游(**重做时建议重新考虑,可能吓到孩子**) |
| 4 | **呼吸 / 憋气** | 医生要求吸气憋气(可能多次) | 扳机按住 / 松开;A 结束 | 吸气 = 增长、憋气 = 保持、呼气 = 慢慢恢复 | 按住 3 s 到最大:我放大 1.35×、海草螺旋包裹我、水母放大 1.45× 并发光 + 光晕 + 颗粒、珊瑚尖端发光、全场闪烁粒子更亮更快;松开 3 s 恢复 |
| 5 | **天亮** | 憋气结束、检查收尾 | A | 放松、告别 | 天亮,全部水母约 14 s 内游走,不留一只 |
| 6 | **退出** | 检查结束 | 自动 | 回到现实 | 鱼群聚拢 → 海底反向退去 → 鱼群散开 → 回到现实 → 3 s 后回到待机 |

> 旧版曾互换过"涂耦合剂"和"探头"两个节拍的内容,也删掉过"和水母握手"的结尾互动、助手手持的水草道具(Tag 0)和水母玩具(Tag 2)。说明:**节拍是固定的,节拍里的内容一直在变**,重做时可以大胆设计。

---

## 5. 可以自由发挥的部分

- 每个节拍的意象、角色、数量、动画、构图、色彩、粒子、着色器。
- 进入 / 退出转场的形式。
- 跟随探头的对象(小丑鱼和海草只是旧版的选择)。
- 呼吸可视化的方式。
- 开始入口的形式。
- 额外的小互动(例如患者注视某处触发彩蛋;注意患者头部活动有限)。
- 代码架构、脚本划分、场景是用脚本生成还是手工 + Prefab。

**不建议改**:第 2 节硬约束、助手的按键映射(第 6 节,助手已熟悉)、tag 规格(已打印实物)。

---

## 6. 操作映射(建议保留)

| 输入 | 编辑器替代 | 作用 |
|---|---|---|
| 右手柄 **A** | 空格 / 鼠标点 Start | 开始 / 下一阶段 |
| 右手柄 **B** | 键盘 B 或 R | 随时回到待机(现实) |
| 右手柄 **扳机按住** | 按住 F | 吸气 / 憋气;松开 = 呼气 |
| — | Backspace | 上一阶段(调试) |
| — | 鼠标左键按住 | 模拟探头 Tag 1(鼠标位置映射到身体区域) |
| — | Alt + 左键拖动 / Home | 编辑器里转头 / 复位 |
| — | T | 开关 tag 调试标记 |

真机上的开始只用 A 键(不做手柄射线点击)。

---

## 7. 技术栈(旧版已验证可用,建议沿用)

- Unity **6000.3.27f2**;URP **17.3.0**;Meta XR SDK all **207.0.1**(`com.meta.xr.sdk.all`);OpenXR **1.19.0** + `com.unity.xr.meta-openxr` 2.7.0;Input System 1.20.1,Active Input Handling = **Both**。
- 构建目标 Android;旧包名 `com.DefaultCompany.UnderTheSea`(新项目可以改)。
- 相机:`OVRCameraRig` + `OVRManager`(开启 Passthrough,追踪原点 EyeLevel)+ Underlay 的 `OVRPassthroughLayer`。
- AprilTag:内嵌包 `jp.keijiro.apriltag` 1.0.3(从旧工程 `Packages/jp.keijiro.apriltag` **整个复制**,包含标定内参的改动;BSD-2-Clause)。
- 版本库:由用户决定(旧工程用 Plastic)。

---

## 8. 技术要点与踩过的坑(必读)

### 8.1 Passthrough / 渲染
- **URP 资源必须关闭 HDR**,相机 `allowHDR = false`。HDR 颜色缓冲 B10G11R11 没有 alpha,Underlay Passthrough 会蒙上一层蓝色。代价:Bloom 会弱一些。
- AR ↔ VR 混合:相机清屏色用**预乘 alpha**:`(water.r*v, water.g*v, water.b*v, v)`,v = VR 显现度;待机时是 `(0,0,0,0)`。
- OVRProjectConfig:Passthrough Support = Required,Passthrough Camera Access 开启。AndroidManifest 需要 `horizonos.permission.HEADSET_CAMERA` 和 `com.oculus.feature.PASSTHROUGH`(可以直接复制旧工程 `Assets/Plugins/Android/AndroidManifest.xml`)。
- Quest 的截图 / 录屏不包含 Passthrough 画面,AR 部分只能戴头显目视确认。

### 8.2 AprilTag
- 标记族 **tagStandard41h12**(不是 36h11)。打印含白边 5×5 cm,**检测尺寸 = 黑框角点跨度 = 0.05 × 5/9 = 0.02777778 m**。打印页在旧工程 `Documentation/AprilTag/PrintProbeTag.html`(按 100% 打印)。
- 链路:`PassthroughCameraAccess`(左相机)→ `GetColors()` → Keijiro `TagDetector`(decimation 2)→ 用 `ViewportPointToRay((0,0))` 和 `((1,1))` 反算 fx / fy / cx / cy → 相机位姿 × tag 局部位姿 = 世界位姿。**先取相机位姿,再拷贝像素。**
- 启动时请求相机权限;Link 下相机可能延迟可用,要重试打开(旧版重试 3 次)。
- 只在需要的阶段检测,10 Hz,省 CPU;超过 0.4 s 没看到就认为丢失。
- 抽象接口 `TagPoseSource.TryGetPose(id, out pos, out rot)`:真机实现 + 编辑器鼠标模拟组合(真机优先);表现层只依赖抽象。
- 编辑器自检:合成一张含 tag 的图喂给检测器,验证 ID 和深度(旧工程 `Assets/Editor/AprilTagSelfTest.cs`,旧版合成画面通过、空白画面 0 误检)。
- ⚠ 真机识别率、抖动、遮挡和躺姿下的对齐**都还没验证过**,这是最大的技术风险(第 14 节)。

### 8.3 编辑器预览 / XR
- 编辑器 Play 时,如果没通过 Quest Link 连头显,OpenXR 运行时会让会话立刻退出,`OVRManager` 会直接停止 Play。解决:加一个执行顺序最早(−32000)的编辑器专用组件,检测到 `OVRPlugin.shouldQuit` 时停用 OVRManager、`DeinitializeLoader`,退回桌面预览(旧工程 `Assets/Scripts/XRLinkFallback.cs`,可直接复用)。
- **Meta XR Operator**(`Window > Meta > Meta XR Operator`)被激活时会注入 OpenXR API 层 `XR_APILAYER_METAX_operator`,导致 Quest Link 进不去头显。保持 Deactivate。
- OpenXR 不要降到 1.16.1(会出编辑器 NullReferenceException)。
- 控制台里 Meta AgentBridge 端口 48735 的报错与项目无关。

### 8.4 音频
- 提供的录音偏小:泡泡环境声约 −37 dB,旁白 −27 ~ −31 dB。`AudioSource.volume` 最大只能到 1,需要用 `OnAudioFilterRead` 做增益 + 软限幅(旧工程 `Assets/Scripts/AudioGain.cs`)。短音效 / 旁白按响度归一到约 −20 dB,再乘一个总增益(Quest 自带喇叭偏小,旧版 1.5)。
- 旁白播放时压低音乐(旧版压到 50%)和环境声(85%),不要压太狠,否则会显得很空。
- **测试 APK 前先关掉 Quest Link**,否则回到应用时没有声音。
- 全部用 2D 声音即可。

### 8.5 Unity 编辑器 / AI 工作流
- Unity MCP 能编译、执行编辑器脚本、相机截图、构建,但**不能进入 Play 模式**。所以从架构上就要让动画能在编辑器里"给定时间逐帧模拟":预留注入阶段时间、呼吸值、合成 tag 的入口;验证方式 = 逐帧模拟 + 相机渲染截图 + 数值断言,最后由用户点 Play / 戴头显确认。
- 旧版用一个编辑器 Builder 脚本生成整个 Main 场景作为唯一来源。好处:AI 可以重复生成,不怕手改丢失;坏处:Builder 越写越大(710 行),每次重建都会改动 URP / 项目设置等大量文件。重做时二选一并写进 CLAUDE.md:**Builder 生成**,或 **Prefab + 少量手工场景**。
- Unity 对象不要用 `??` / `?.` 判空(Unity 的假 null),旧版踩过两次。
- Unity 6.3 的 `FindObjectsByType` 要显式传 `FindObjectsSortMode.None`。
- 旧美术里部分装饰珊瑚网格约 2.4 万顶点,要注意 Quest 的性能预算(旧版没测过帧率)。

### 8.6 构建与安装
- `BuildPipeline.BuildPlayer`(主场景,Android,输出 `Builds/UnderTheSea.apk`),约 4–5 分钟;Play 模式下不能构建。
- adb:`C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`
- 安装 `adb install -r Builds/UnderTheSea.apk`;启动 `adb shell monkey -p <包名> -c android.intent.category.LAUNCHER 1`;日志 `adb logcat -s Unity`;用 `aapt dump permissions` 确认 APK 里有 `HEADSET_CAMERA`。
- 首次启动时在头显里允许相机权限。

---

## 9. 架构建议(可调整)

- **状态机**(`ExperienceController`):只管当前阶段、阶段内时间、呼吸值 `Breath`(0..1;只在呼吸阶段有效,按住 3 s 升到 1,松开 3 s 降到 0);发出 `StageChanged(prev, next)`;转场阶段按时长自动前进,临床阶段只听输入。
- **输入与状态机解耦**:手柄、键盘、自动化测试各自写自己的"按住"标志,最后 OR 起来,互不覆盖。
- **表现层按子系统拆分**:环境显现、光照 / 天色、鱼群、探头跟随物、呼吸效果、音效、旁白各一个组件,只读 Stage / TimeInStage / Breath / 显现度,自己做平滑。避免一个巨大的总表现类(旧版 `OceanStageView` 写到了 631 行)。
- **全局显现度 v**(0 = 现实,1 = 海底)统一驱动背景 alpha、雾、环境声音量、物体出现。
- **Tag 位姿抽象** + 真机 / 模拟组合(8.2)。
- **旁白 cue 表数据化**:阶段 + 触发条件 + 延迟 + 兜底秒数 + 片段起止 + 增益,在 Inspector 里可改;一次只播一条、排队;按 B 时重置。
- **验证钩子**:强制阶段时间、强制呼吸值、合成 tag 源;提供一个"完整流程冒烟检查"菜单。
- **调试**:HUD 显示阶段、Breath、tag 状态;日志 `[Experience] A -> B`、`[VO] id: 台词`。

---

## 10. 可复用资产(都在旧工程 `C:\Users\47632\UnderTheSea`,按需复制)

> 美术授权:用户已授权使用协作项目「Ultrasound_Under the Sea」的全部美术、元素与动画。也可以自由新做美术。

| 资产 | 路径 | 说明 |
|---|---|---|
| 音效(8 个) | `Assets/Audio/*.mp3` | 文件名(中文)就是用途:泡泡常驻环境声、进入 VR 后的开场背景乐、开场后接续、每结束一个场景、小鱼靠近珊瑚、憋气时背景音、憋气时声音(前半段吸气 / 后半段呼气)、退场进入 AR |
| 旁白 | `Assets/Audio/VO/VO_01…08.mp3` + `VO_script.txt` | 见第 11 节;`VO_03.1` / `VO_03.2` 已废弃 |
| 协作美术 | `Assets/UltrasoundReef/CollabArt/ReefScene_Unity.fbx` + `Textures/` | 主角珊瑚 `Player_Coral_Downloaded`、装饰珊瑚 `Coral*` / `Plate_*`、小鱼 `Fish_*`、水母 `Jelly_*` / `Interactive_Jellyfish`、海葵 |
| 环境 | `Assets/UltrasoundReef/Environment`、`Assets/UltrasoundReef/Shaders` | 沙地、岩石、拱门、珊瑚等网格;水下天空渐变着色器 |
| 小丑鱼 | `Assets/Models/Clownfish`(说明在 `Documentation/Clownfish`) | 带骨骼,`Swim_Loop` 动画 |
| AprilTag | `Packages/jp.keijiro.apriltag`、`Documentation/AprilTag/*.html` | 检测库 + 打印页(ID 1 = 探头) |
| 基础设施代码(可直接复用) | `QuestAprilTagPoseSource.cs`、`TagIds.cs`(含 `TagPoseSource`)、`XRLinkFallback.cs`、`AudioGain.cs`、`PassthroughBridge.cs`、`Assets/Editor/AprilTagSelfTest.cs`、`Assets/Editor/XRPlayModeToggle.cs` | 都是踩过坑的基础设施;**玩法 / 表现代码不要照搬** |
| 着色器参考 | `Assets/Shaders/SoftSeaweed`、`BreathSparkle`、`SeaSurface`;`CollabArt/Shaders/CollabCoral`、`CollabJelly` | 可参考,不必照搬 |

---

## 11. 旁白 VO 合约

旁白已经录好。**如果复用这些录音,对应节拍的画面必须和台词内容一致**;如果想改内容,就需要重新录音(先和用户确认)。

| 文件 | 台词 | 旧版触发时机 |
|---|---|---|
| VO_01 | Welcome to Ultrasound Wonders: Under the Sea. Make yourself comfortable. Here, you are a coral, resting gently beneath the waves. For now, breathe normally. Follow your clinician's instructions, and let them know if you need a pause. | 进入:VR 完全显现后 1 s |
| VO_02 | A gentle wave passes over you. Some little fish have come to say hello. | 涂耦合剂:进入后 1 s |
| VO_03 | A little clownfish nestles close as the seagrass gently rests against you. You may feel a little pressure. | 探头:第一次识别到 tag(3 s 兜底) |
| VO_04 | Night is falling over the reef, the little fish nestle among your coral branches. Let's help the jellyfish's gentle light grow. | 天黑过渡:进入后 0.5 s |
| VO_05 | Take a slow breath in, as deeply as feels comfortable. Watch the jellyfish grow and glow. | 呼吸:进入后 1 s |
| VO_06a(0–4.27 s) | Gently hold for a moment. The light rests with you. | 05 播完且第一次按住扳机后 2 s(20 s 兜底) |
| VO_06b(4.27–7.29 s) | Perfect. Now you can breathe normally. | 之后第一次松开扳机 |
| VO_06c(7.29–13.25 s) | Let's try it again. Take a slow breath in, then hold. | 06b 播完 5 s 后 |
| VO_06d(13.25 s–结尾) | Wonderful. You can breathe normally now. | 第二次松开扳机 |
| VO_07 | You can breathe normally now. The light spreads softly around the little fish. | 天亮:进入后 0.5 s |
| VO_08 | Our underwater journey is coming to an end. Thank you for sharing this little ocean with us. Gently bring your attention back to the room. Keep resting while we help you remove the headset. | 退出:进入后 0.3 s;回到现实后继续播完(只有按 B 才中断) |

台词里隐含的画面要求:**我是珊瑚、温柔的海浪、小鱼打招呼、小丑鱼 + 海草贴着我、按压感、天黑、小鱼躲进我的枝杈、水母的光随呼吸长大、光散开到小鱼身边、回到房间**。

---

## 12. 重做路线图(每个里程碑完成后汇报,等确认)

| 里程碑 | 内容 | 验收 |
|---|---|---|
| **M0 方案** | 读文档 → 给出创意方案、架构、问题清单(见第 0 节) | 用户确认 |
| **M1 工程骨架** | 新工程、包、URP(HDR 关)、XR / Passthrough、Manifest、编辑器 XR 兜底、构建 + adb 安装脚本 | 编辑器能 Play(有无头显都行);Quest 上看到 Passthrough + 一个测试物体,颜色正常 |
| **M2 真机 AprilTag 冒烟**(尽早去风险) | 移植检测链路 + 调试标记 | 头显里看到标记跟着打印的 ID 1 tag 走;记录识别距离、角度、延迟、丢失情况 |
| **M3 流程骨架** | 状态机、输入、HUD、占位表现、显现度 v、AR↔VR 占位过渡 | A / B / 扳机 / 键盘全通;任意阶段按 B 回到现实 |
| **M4 世界与"我是珊瑚"** | 海底环境、主角珊瑚、机位与朝向、进入 / 退出转场 | 编辑器截图 + 头显目视:能看到"自己",过渡柔和 |
| **M5 探头互动** | 跟随物、按压感、丢失时的默认行为 | 模拟 + 真机都跟得上、不抖、丢失时自然 |
| **M6 呼吸高潮** | 天黑过渡 + 呼吸可视化 | 按下同一帧就有响应,3 s 到最大,松开 3 s 恢复,可反复 |
| **M7 其余节拍** | 涂耦合剂、天亮、退出 | 完整流程走通 |
| **M8 声音与旁白** | 音效、VO cue 表、增益 / 压低 | 在 Quest 喇叭上听清,旁白不被音乐盖住 |
| **M9 真机打磨** | 性能、舒适度、躺姿对齐、完整流程走查 | 第 13 节验收清单全部通过 |

---

## 13. 验收清单

- [ ] Quest 3 上(关掉 Link)从待机走到退出再回到待机,`logcat` 无错误。
- [ ] 任意阶段按 B,1–2 秒内回到现实,下一轮能正常重新开始(声音、旁白都重置)。
- [ ] Passthrough 颜色正常,没有蓝色蒙层;进出过渡柔和。
- [ ] 患者躺 / 半躺时能自然看到"自己这株珊瑚"。
- [ ] 探头 tag 真机识别,跟随延迟 < 0.5 s;丢失时自然过渡到默认行为。
- [ ] 呼吸:按下即响应,可以反复,没有失败状态。
- [ ] 稳定 ≥ 72 fps。
- [ ] 声音在 Quest 喇叭上清楚,旁白时机正确。
- [ ] 没有物体贴近眼睛,没有相机运动,没有吓人的元素。

---

## 14. 未解决的问题 / 风险(重做时要先和用户确认)

1. **躺姿对齐**:旧版把相机放在主角珊瑚上方约 0.55 m、稍靠后、45° 向下(追踪原点 EyeLevel),这是按"坐着 / 看向前下方"设计的。患者躺着时自然视线朝向天花板,**能不能看到"自己"在真机上没验证过**。可能的方向(待确认):开始时按 A 以患者当前头部朝向重新定向世界;或加一个"重新居中"键。
2. **真实肚子 ↔ 虚拟身体的标定**:没做。旧版只是把 tag 位置钳制在珊瑚身体周围的圆柱区域里。可能的方向:开始前助手把探头放在患者肚子上按一次键,作为身体锚点。
3. **真机 AprilTag**:识别距离、光照、探头倾角、被医生的手遮挡、5 cm tag 是否够大,都没测过。
4. **性能**:Quest 3 实机帧率没测过(装饰珊瑚网格偏重)。
5. **音量 / 旁白时机**:在头显里还没最终确认。
6. **内容与人群**:目标年龄段、一次检查的典型时长、憋气通常几次、医院现场流程——需要向用户确认。"天敌"大鱼这类带紧张感的元素是否合适,需要确认。

---

## 15. 工作约定

- 用户逐条提需求;每做完一批先汇报,等确认再继续。
- **不要擅自提交版本库**,由用户决定何时提交。
- 汇报时如实区分:只编译通过 / 编辑器逐帧模拟通过 / Play 模式通过 / 真机通过。
- `CLAUDE.md` 只保留"当前状态汇总";逐轮修改历史另存到 `Documentation/` 下。
- 如果选择用 Builder 脚本生成场景:**所有场景改动都写进 Builder**,手改的场景会在下次重建时被覆盖。
