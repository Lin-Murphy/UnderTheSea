# 海底资产迁移记录（2026-11-05）

目标：保留 UnderTheSea 的完整体验流程，将收到的 Ultrasound_Under the Sea 项目的美术、元素和程序动画接入 Main，替换占位物体。

## 项目检查与迁移边界

迁移前目标项目的 Assets、Packages、ProjectSettings 共 128 个文件，约 1 MB：10 个 C# 脚本、14 个占位材质、1 个 Main 场景；使用 Unity 6000.3.27f2、Built-in、Meta XR SDK 207.0.1、OpenXR，Input Handling 为 Both。

已检查状态机、键盘/手柄输入、Tag 位姿抽象与鼠标模拟、表现层、Start 按钮、Passthrough 封装、场景生成器、Main 的序列化引用、包依赖及项目/XR 配置。

来源使用 Unity 6000.6.7f1、URP 17.6.0。来源清单中 224 个文件的 SHA-256 已全部核验。Assets 中没有独立 `.anim` / Animator Controller；动画由运行时脚本和顶点着色器驱动。

两个 Assets 目录只有 `Scenes.meta` 同路径；没有跨项目 GUID 冲突。迁入时保留当前 `Scenes.meta`，其他资源保留来源 `.meta` / GUID。

## 实现

- 目标继续使用 Unity 6000.3.27f2，使用 URP 17.3.0；通过 Unity Package Manager Client API 安装，保留当前 Meta、OpenXR、AI 等既有依赖。
- 不覆盖来源的 ProjectSettings 或用其桌面配置替换当前 XR / Android 配置。
- 来源全部 Assets、四个预览场景和开发文档保留；完整原件另存于 `Migration~/2026-11-05/SourceProject`。
- Main 以来源最终的 `UnderwaterExamPreview` 场景为美术基础，通过 Unity Editor API 生成、保存；保留当前 `Main.unity.meta`。
- 继续由 `ExperienceController`、`KeyboardStageInput`、`XRControllerStageInput` 统一推进阶段；Main 移除来源独立桌面阶段控制器，防止按键重复响应。
- `ReefArtDriver` 将胶水拨草、夜色、背景鱼群路线、延迟曲线入场和水母呼吸参数接到当前状态机。
- `ReefHandRibbon` 将现有 Tag 2 分节点表现为连续的水母飘带；患者与镜像保持同步。
- 原占位生成器改为 `UnderTheSea > Legacy > Build Placeholder Preview`，只生成 `LegacyStep1Preview.unity`。

| 当前阶段 | 接入的来源元素与行为 |
|---|---|
| Standby / Finished | 现实/AR、Start 按钮；隐藏海底环境 |
| Enter | 新鱼模型的转场鱼群；沙地、岩石、拱门、珊瑚、水草、光束逐步显现 |
| ApplyGel | GPU 固定根部水草覆盖/拨开；Tag 0 驱动叶片弯曲，丢失后回到原自动拨草动画 |
| Probe | 新鱼模型与鱼尾动画；Tag 1 跟随、丢失宽限和镜像同步 |
| DuskTransition | 4.5 秒夜色渐变、1.2 秒延迟加 6 秒水母曲线入场；阶段总时长 7.2 秒 |
| BreathHold | 原珊瑚同步增大、鱼回体内；四部分水母模型的伞盖、触须、粉色标记与 Breath 参数 |
| ExhaleTransition | 天亮、珊瑚复原、水母离去，留下一只 |
| Finale | 留下的水母靠近，Tag 2 控制连续触手飘带与镜像 |
| Exit | 转场鱼群、新环境反向退去，Finished 后回到 Start |

源代码兼容适配：显式传入 Unity 6.3 所需的 `FindObjectsSortMode.None`；水母动画增加 `handleInput` 开关（原预览场景默认仍支持手动输入）；探头鱼保持导入网格的基础缩放；动态显现对象取消静态批处理标记。

模板 Readme 的生命周期和 GUID 查询 API 已回退到 6.3 支持的写法。来源 `UniversalRenderPipelineGlobalSettings.asset` 中仅 6.6 支持的渲染资源类型已通过 Unity 的全局设置创建 API 重建为 6.3 配置，保留原 GUID；当前项目使用独立的 `Quest_GlobalSettings.asset`。6.6 原始配置完整保留于 `Migration~`，未修改微信收到的项目目录。

## 使用

入口为 `Assets/Scenes/Main.unity`。空格 / 右手柄 A 开始及推进，Backspace 返回上一阶段，B / R / 右手柄 B 回到 Start。

编辑器模拟：左键 Tag 1、右键 Tag 0、中键 Tag 2；Alt + 左键拖动转头，Home 复位。真实 XR 活跃时不会用桌面旋转覆盖头显位姿。

来源桌面预览场景仍保留原来的 Space、1/2/3、P、R 等控制。它们与 Main 是独立入口。

## 验证状态

已完成源文件清单校验、GUID 冲突检查、接入变换的临时副本演练和 34 个 C# 文件的 Unity 6.3 静态编译预检。

已在 Unity 6000.3.27f2 中实际导入、编译、生成并重载 Main。保存后的场景检查通过：URP/全部 Quality 分配、Renderer/PostProcessData、全局设置、模型引用、Volume、着色器和构建入口均有效。来源四个场景的脚本丢失数量均为 0。

完整流程检查 **17/17 通过**，报告结束时运行帧数为 **8606**；包含 Tag 0/1 跟随与丢失宽限、镜像同步、珊瑚/水母 Breath、夜色与入场、留下一只水母、Tag 2 触手、退出循环和复位。已查看实际白天、夜间画面。

补充检查确认：从完全显现的 VR 状态复位后，海底隐藏并重新显示 Start；Main 没有烘焙 lightmap、Reflection Probe 或 Light Probe Group，方向光为 Realtime，无需迁移旧烘焙输出。最终控制台采样为 0 错误、0 警告（历史导入日志中仍保留 SDK 示例资源的提示）；Main 已退出 Play，恢复原本关闭的 Run In Background 设置。

Unity 实际验证结果分别写入同目录 `editor-validation.json`、`flow-validation.json`；原预览场景检查为 `source-scene-validation.txt`；截图写入 `Screenshots/`。这些文件不存在时，不能将“接入代码已准备”表述为“编辑器或运行验证通过”。

检查菜单：`UnderTheSea > Migration > Validate Saved Main` 与 `Run Complete Flow Smoke Check`。流程检查临时创建编辑器专用的合成 Tag 源；它不会保存到场景或包含在构建中。此结果只证明软件流程，不能证明真实 AprilTag、Quest Passthrough、躺姿标定、手柄硬件输入或实机性能通过。

## 备份与回退

迁移前完整源项目快照：`Migration~/2026-11-05/UnderTheSea-before-migration.zip`，SHA-256 为 `de76a32a2f7ef3a941dd4c70ddd3a9887ec1eac11a408d9eaf20797401a90b17`。

需要完整回退时，关闭 Unity，把该 ZIP 解压到一个新的空目录，以该目录打开迁移前项目。不要仅把 ZIP 覆盖解压到当前 Assets 中，因为那会留下新增资源。

`Migration~/2026-11-05/inventory.json` 保存迁移前文件与来源资产哈希，`asset-import.json` 保存实际迁入清单。迁移没有执行 Unity Version Control check-in 或远程推送。

## 补充：协作版水母 / 珊瑚 / 小鱼素材（2026-11-05）

来源：微信收到的 `海底世界_Unity协作版(1)`（`ReefScene_Unity.fbx` + `Textures/`）。原件另存于 `Migration~/2026-11-05-collab/SourceProject`，替换前的 `Main.unity`、`BuildReefMainScene.cs`、`ReefMigrationChecks.cs` 同目录备份。

只替换三类对象的网格与材质；位置、尺寸外框、状态机、动画脚本、Tag 交互均不变。沙地、岩石、拱门、水草、海葵等来源模型**未接入**。

| 对象 | 来源 | 处理 |
|---|---|---|
| 主角 / 镜像珊瑚 | `Player_Coral_Downloaded` + 贴图 `10.png` | 适配原外框（最大尺寸、底部中心对齐）；镜像用同贴图加深色调 `MirrorCoralArt.mat` |
| 装饰珊瑚（Coral gardens 150 个） | `Coral7/21/34_*`、`Plate_*`，烘焙为 9 种网格 + 8 张底色图集 | 按原位置/尺寸依次分配并随机朝向 |
| 小鱼（近处、鱼群、探头、镜像、转场） | `Fish_*`（身体/鳍/眼三材质） | 头朝 +Z，尺寸同原 PastelReefFish；`CollabFish` 着色器保留尾部摆动和夜色 |
| 水母（3 只） | `Interactive_Jellyfish` | 旧四部件隐藏；`CollabJelly` 着色器沿用 `_Breath` / `_Visibility` / `_JellyTime`，吸气变大发光、入场淡入不变 |

文件：`Assets/UltrasoundReef/CollabArt/`（FBX、贴图、`Shaders/`、`Baked/`、`Materials/`）；接入代码 `Assets/Editor/CollabArtSwap.cs`，由 `BuildReefMainScene` 调用。`CollabArtRunner.cs` 允许外部通过 `Temp/collab-build.request` / `Temp/collab-flow.request` 触发重建与流程检查。

验证：重建后 `editor-validation.json` 通过，流程检查 17/17 通过；截图已更新。注意：装饰珊瑚中有两种网格约 2.4 万顶点，Quest 实机帧率未测。
