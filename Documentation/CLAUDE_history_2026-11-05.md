# UnderTheSea — 超声检查儿童安抚 AR/VR (Quest 3) Unity 项目

> **2026-11-05 资产迁移授权**：用户明确要求保留当前完整体验流程，将收到的 `Ultrasound_Under the Sea` 项目的全部美术、元素与动画接入并替换占位效果。本次美术迁移已获授权，不需要按旧的步骤 7 再次征求许可。后续开发以当前工程及 `Documentation/Migration-2026-11-05/README.md` 为准。

> 本文件是项目的总纲与开发路线图,Claude 每次开发前必读。
> **工作方式:严格按「开发步骤」逐步推进。每完成一步,先向用户汇报并等待确认/修改,再进入下一步。不要提前做后面步骤的内容(例如步骤 1 阶段不要做音效、美术风格)。**

## 1. 项目简介

面向**超声检查中的儿童患者**的情绪安抚体验。AR + VR 混合(AR→VR→AR)的 Unity 项目,运行于 **Meta Quest 3**。

- 患者躺在病床上,**只戴 Quest 3 头显**,无手柄。
- 参与者在故事中**是一株珊瑚**,身处海底珊瑚礁。
- 体验开始与结束时通过转场与现实(AR/Passthrough)衔接,中间为纯 VR 的海底场景。

### 三个角色
| 角色 | 职责 |
|---|---|
| 患者 | 躺在病床上戴 Quest 3,体验场景 |
| 医生 | 操作超声探头(探头上贴 AprilTag) |
| 医生助手 | 手持 Quest 手柄,用**按钮切换场景**;另一只手柄暂时留空。助手还会手持带 AprilTag 的道具(水草 tag、毛绒玩具 tag) |

## 2. 世界观与主场景

整个体验始终在**同一个大场景**内,通过状态/事件切换,不加载新场景。

- 参与者(主角珊瑚)位于世界中心。
- 四周有形态各异的**装饰珊瑚**;近处有小鱼随机游动;远处有鱼群游动。
- ~~镜像珊瑚~~:**已删除(2026-11-05 用户要求)**。参与者自己就是主角珊瑚,相机放在主角珊瑚上方(第一人称),低头就能看到自己这株珊瑚。

### 对象清单

**基础场景对象**:地面、水草、其他(装饰)珊瑚、小鱼

**互动对象**:水母、主体(主角)珊瑚、小鱼、水草

**交互事件 / 场景变化**:天黑、天亮、水草互动、小鱼互动、憋气

## 3. 事件流程(6 大场景)

| # | 场景 | 场景变化 | AprilTag 互动 |
|---|---|---|---|
| 1 | 进入场景 | 过渡 AR→VR,使用**鱼群转场** | — |
| 2 | 涂耦合剂 | **(2026-11-05 与场景3 互换)** 小鱼游进我(珊瑚)体内躲起来 | 无追踪 |
| 3 | 探头检查 | **(2026-11-05 与场景2 互换)** 小鱼离开我回到正常;海草随浪抚摸身体,**跟随探头并有按压感** | **探头 Tag 1**:海草跟随并在按压点被压扁摊开 |
| 4 | 呼吸/憋气探头检查 | 见下方「场景 4 细节」 | 同场景 3 |
| 5 | 结尾交互 | ~~握手/拥抱~~ **已删除(2026-11-05)**:全部水母缓慢游走,不留下任何一只 | ~~水母(tag)~~ 不再使用 |
| 6 | 退出场景 | 最后一只水母游走,背后跟一群鱼群,转场 VR→AR 回归现实 | — |

### 过渡动画(不计入 6 大场景)
- **3→4 之间**:天黑(只到白天 50% 的暗度)+ 水母一上一下地游来(main 近处 3 只 + demo1 远处 3 只),到位后保持天黑、水母环绕、水母不发光。
- **4 内**:
  - **吸气(按住右手柄扳机)**:3 秒内水母放大到 130% 并慢慢发光;珊瑚、海葵、参与者自己的枝叶尖端发微光并冒发光颗粒。
  - **憋气**:一直按住 = 保持最大状态。
  - **呼气(松开扳机)**:3 秒缓慢回到正常。场景 4 中可反复呼吸。之后按 A 进入呼气过渡:变天亮,水母仍悬浮。

### 场景 4 细节(吸气→憋气→呼气)
1. 吸气(按住扳机,3 秒到最大):水母变大到 130% 并发光;珊瑚/海葵/参与者尖端微光 + 颗粒;探头区小鱼回到珊瑚体内。主角珊瑚不再自动变大。
2. 憋气:按住扳机保持最大状态。
3. 呼气:松开扳机,3 秒回到正常;可反复。助手按 A 结束场景 4 → 呼气过渡(天亮)→ 场景 5 全部水母离开。

## 4. 需要 AprilTag 追踪的互动

| Tag ID | 名称 | Tag 所在位置 | 效果 | 场景 |
|---|---|---|---|---|
| **0** | 水草 Seaweed | 助手手持的道具 | **2026-11-05 起暂不使用**(海草改为跟随探头 Tag 1;`SoftSeaweedStrands.trackTagId` 可改回 0) | — |
| **1** | 探头 Probe(代码名 `TagIds.Fish`) | 超声探头 | 场景3:海草跟随探头、按压处被压扁并向四周摊开;追踪不到时海草按默认海浪抚摸 | 3 |
| **2** | 水母 Jellyfish | 助手手持的毛绒玩具 | ~~水母触手跟随玩具~~ 握手环节已删除(2026-11-05),Tag 2 暂不使用 | — |

代码中对应 `Assets/Scripts/TagIds.cs`(`TagIds.Seaweed=0 / Fish=1 / Jelly=2`),修改编号时两处同步。
Tag 位姿通过抽象 `TagPoseSource` 获取:`CompositeTagPoseSource` 先用真机检测 `QuestAprilTagPoseSource`(2026-11-05 从 3D-101 移植,**tagStandard41h12**,5 cm 打印,见第 17 节),再退回编辑器 `MockTagPoseSource`(按住鼠标 左键=Tag1 / 右键=Tag0 / 中键=Tag2 模拟)。

(AprilTag 追踪为步骤 2 起才引入;步骤 1 不需要。)

## 5. 操作方式

- 助手使用右手柄:**A = 下一阶段 / Start,B = 随时回到 Start 按钮状态**(左手柄留空,预留)。
- 编辑器内键盘:空格 = 下一阶段 / Start,Backspace = 上一阶段,B 或 R = 回到 Start;鼠标点击 Start 按钮也可开始。
- 右手柄**扳机按住** = 场景 4 吸气/憋气,松开 = 呼气(编辑器内:按住 **F** 键)。

## 6. 美术与音效

- **美术风格**:卡通,但不要太脱离现实。(最后阶段统一调整,前期用 placeholder 即可。)
- **音效**:匹配海洋风格。2026-11-05 已按用户提供的文件接入(见第 19 节)。

## 7. 开发步骤(逐步确认)

每步完成后停下,让用户确认/提修改意见,再进行下一步。

1. **可切换的纯 VR 场景大纲**:按空格在 6 大场景(+过渡状态)之间切换,可用 placeholder 物体/占位表现。搭好场景状态机框架。
2. **场景 3 互动**:小鱼群跟随探头上的 AprilTag,在肚子区域移动。
3. **场景 2 互动**:水草跟随助手手上的 AprilTag 移动。
4. **场景 5 互动**:参与者与水母握手,水母触手跟随助手手中毛绒玩具上的 AprilTag 移动。
5. **转场**:开头 AR→VR 转场(鱼群),结尾 VR→AR 转场。
6. **加入音效**。
7. **更改美术风格**。

> 当前进度:**步骤 1–5 已完成(编辑器内模拟版)；2026-11-05 已按用户授权接入来源项目的美术与程序动画，实际编译和 17 项流程检查通过。步骤 6 音效暂未制作**(真机 AprilTag 检测、真机 Passthrough 效果均未实测)。本次迁移详情见第 10 节;2026-11-05 第二轮修改(第一人称珊瑚视角、demo1 鱼、柔软海草、场景3 鱼躲进珊瑚)见第 11 节。
>
> 流程循环(步骤 5 之后新增):
> - 程序初始 = `Standby`:现实世界,面前显示 **Start 按钮**(`StartButton`)。鼠标点击按钮 / 空格 / 右手柄 A 键 = 开始。
> - `Exit` 转场彻底完成(VR 退尽、鱼群散去)后自动进入 `Finished`(现实),停留 3 秒(`ExperienceController.finishedHold`)后自动回到 `Standby`(Start 按钮未点击状态)。
> - **右手柄 B 键(键盘 B/R)随时**回到 `Standby`;VR 约 1.5 秒内快速退去。
> - 阶段顺序:Standby→Enter→ApplyGel→Probe→DuskTransition→BreathHold→ExhaleTransition→Finale→Exit→Finished→Standby。
> - 真机上 Start 按钮目前只能用 A 键触发(没做手柄射线点击)。
>
> 步骤 5(AR↔VR 转场,在 Enter / Exit 阶段内部完成):
> - 进入:完全现实(Passthrough)→ 现实空间中出现一群转场鱼群(`transitionFish`,绕用户前方盘旋)→ 约 2.5s 后 VR 环境从近到远逐步"长出来"(地面扩张、珊瑚/水草/鱼按距离依次出现,背景 alpha 渐变)→ 约 7s 后鱼群向外散去。
> - 退出:鱼群聚拢 → VR 环境反向退去 → 鱼群散去,回到现实。
> - 参数在 `OceanStageView` 的 "AR↔VR 转场" 一栏(enterFishHold / vrStartDelay / vrRevealSeconds / fishFadeSeconds)。
> - 相机改为 Meta `OVRCameraRig`(OVRManager 开启 Passthrough + Underlay `OVRPassthroughLayer`),`PassthroughBridge.IsLive`(戴头显且 Passthrough 开启)时背景 alpha=VR 显现度;编辑器中用灰色 `realityColor` 代表现实。Builder 同时把 OVRProjectConfig 的 Passthrough 支持设为 Required。
> - 待办/风险:Android 构建目标与 OpenXR/Meta Quest feature 是否已配置未检查;患者躺姿下头部到肚子的位置标定未做(现为固定坐标)。
>
> 步骤 3:场景 2 水草跟随 Tag 0(鼠标右键模拟),丢失则自动来回抚摸。(镜像珊瑚已删除;场景 2 海草已于 2026-11-05 重做,见第 11 节)
> 步骤 4:场景 5 留下的水母飘到患者身边并伸出分节触手,触手尖跟随 Tag 2(鼠标中键模拟),丢失则伸向 `HandPoint`。(镜像珊瑚已删除)握手与拥抱目前不区分,都只是触手跟随毛绒玩具。
>
> 输入:空格 或 右手柄 A 键(`XRControllerStageInput`,UnityEngine.XR primaryButton)= 下一阶段。
>
> 步骤 2 产出:`TagIds.cs`、`MockTagPoseSource.cs`;`OceanStageView` 在 Probe/Dusk 阶段让小鱼围绕 Tag 1 位置(限制在肚子区域 `bellyHalfExtents` 内)游动,tag 丢失超过 `tagLostGrace` 秒则散开。(镜像珊瑚已删除)项目现已装 Meta XR SDK(all 207.0.1)+ OpenXR,输入处理为 Both。
>
> 步骤 1 产出(`Assets/`):
> - `Scenes/Main.unity`:当前为接入美术后的主场景，由 `Editor/BuildReefMainScene.cs` 生成。原 `Editor/BuildStep1Scene.cs` 已改为只生成独立的 `LegacyStep1Preview.unity`，不覆盖 Main。
> - `Scripts/ExperienceController.cs`:状态机(`Stage` 枚举:Standby→Enter→ApplyGel→Probe→DuskTransition→BreathHold→ExhaleTransition→Finale→Exit→Standby),过渡阶段按时长自动前进。
> - `Scripts/KeyboardStageInput.cs`:空格=下一阶段,Backspace=上一阶段,R=回待机。输入与状态机解耦。
> - `Scripts/OceanStageView.cs`:placeholder 表现层(天黑/亮、主角珊瑚变大、小鱼、水草、水母)。假探头/假水草是占位,后续步骤换成 AprilTag。
> - `Scripts/StageHud.cs`:编辑器内 IMGUI 提示。
> - 步骤 1 初始版本使用 Built-in 和旧 Input Manager；当前已接入 Meta XR / OpenXR，并迁移到 URP 17.3.0，输入处理保留 Both，详见第 10 节。

## 8. 开发约定(建议)

- 平台:Meta Quest 3,Passthrough 用于 AR 部分。
- 场景流程用**状态机**管理(6 大场景 + 过渡状态),输入层(空格键 / 手柄按钮)与状态机解耦,便于后期替换输入。
- 主角珊瑚(= 参与者)的状态/动画由同一数据驱动;镜像珊瑚已删除,不要再加回。
- Placeholder 阶段优先保证流程正确,不追求美术效果。

## 9. 待确认的问题(进入对应步骤前向用户确认)

- 场景 4 中憋气的持续时间:由助手按钮手动结束,还是自动计时?
- AprilTag 追踪方案(Quest 3 passthrough 相机访问 + 哪个 AprilTag 库/16h5 或 36h11 家族、tag 尺寸)。
- 场景 5 中「握手」与「拥抱」是否为两个独立子步骤,各自如何触发?
- 使用的 Unity 版本与 XR 包(Meta XR SDK / OpenXR)——如项目中已有则以现状为准。

## 10. 2026-11-05 海底资产迁移

- 当前版本：Unity **6000.3.27f2**，渲染管线 **URP 17.3.0**；保留 Meta XR SDK 207.0.1、OpenXR 和 Both 输入处理。
- 主入口仍为 `Assets/Scenes/Main.unity`，由 `Assets/Editor/BuildReefMainScene.cs` 通过 Unity Editor API 接入来源最终检查场景的美术；完整状态机、A/B 按键、Tag 0/1/2 抽象和 AR↔VR 进出流程保留。
- 来源资源位于 `Assets/UltrasoundReef`，来源四个预览场景保留在 `Assets/Scenes`；预览场景使用各自的桌面控制，Main 使用当前项目的统一控制。
- `ReefArtDriver` 驱动导入的 GPU 水草、背景鱼群、夜色、延迟曲线入场和四部分水母；`ReefHandRibbon` 为 Tag 2 触手生成连续飘带。
- 编辑器用 Alt+左键拖动转头，Home 复位；左/右/中键仍分别模拟 Tag 1/0/2。真实 XR 活跃时不覆盖头显位姿。
- 原 `BuildStep1Scene` 菜单改为 `UnderTheSea > Legacy > Build Placeholder Preview`，输出独立 `LegacyStep1Preview.unity`；**不要把它改回覆盖 Main**。
- 来源 Unity 6.6 的便利 API 和全局渲染设置已适配到 6.3。所有原始文件另存于 `Migration~/2026-11-05/SourceProject`，迁移前目标项目快照为同目录 `UnderTheSea-before-migration.zip`。
- 验证以 `Documentation/Migration-2026-11-05/editor-validation.json`、`flow-validation.json` 的实际结果为准。真实 AprilTag、Quest Passthrough、患者躺姿标定和实机性能仍需单独验证。
- **协作版素材替换(2026-11-05)**:水母、珊瑚(主角/镜像/装饰)、小鱼已换成 `Assets/UltrasoundReef/CollabArt/ReefScene_Unity.fbx` 的模型与贴图,由 `Assets/Editor/CollabArtSwap.cs` 在 `BuildReefMainScene` 中烘焙并替换网格/材质,位置与流程不变。详见 `Documentation/Migration-2026-11-05/README.md` 末节。(之后:镜像珊瑚已删除;小鱼改用 demo1 的原始 Fish_* 网格,见第 11 节。)

## 11. 2026-11-05 第二轮修改(用户逐条提出,当前做完前三条,后续还会追加)

版本库:Unity Version Control(Plastic),工作区在 `/main` 分支。`/main/demo1` 是另一条分支(只读参考,不要往里提交)。

- **机位 / 删除镜像珊瑚**:相机(OVRCameraRig)放在主角珊瑚顶部上方约 0.55m、稍靠后,视线约 45° 向下,低头即看到自己这株珊瑚;OVRManager 追踪原点改为 `EyeLevel`。镜像珊瑚、其支撑岩石、镜像小鱼/镜像水草/镜像触手及 `OceanStageView` 中的全部镜像逻辑已删除。Start 按钮与转场鱼群改用水平朝前方向(`OceanStageView.FlatForward()`)。
- **场景 1 鱼群 = demo1 的鱼**:近处 + 远处的鱼全部换成 `/main/demo1` 用的 `ReefScene_Unity.fbx` 原始 `Fish_*` 网格与材质(远处 36 条 Fish_101..136,近处 Fish_137/138 + 10 条复制;探头鱼、转场鱼也用同一模型),游动方式为 demo1 的轨道游动(近处半径 1.5–4.5m,远处 14–22m,头朝游动方向),以主角珊瑚为中心。由 `Assets/Scripts/ReefFishSchool.cs` 驱动;外壳物体 +Z = 鱼头。
- **场景 2 海草重做**:旧的整片式 GPU 涂胶海草(`GelSeaweedPreview`)已从 Main 移除,换成 `Assets/Scripts/SoftSeaweedStrands.cs` + `Assets/Shaders/SoftSeaweed.shader`:24 根独立的 Verlet 软体叶片围绕参与者根部,每根有自己的长度/宽度/相位/频率,叠加依次经过的海浪;涂耦合剂时上半段随浪一波一波贴到珊瑚身体上滑过,Tag 0 被识别时抚摸位置跟随 tag。不穿过珊瑚、不扫到相机。
- **场景 3 鱼躲进珊瑚**:**只在天黑时**(天黑过渡 + 场景4),约 80% 的环境小鱼(`hideFraction`)游向附近 10 株珊瑚,其中约 30%(`onMeFraction`)贴到参与者自己身上;它们缩小(`hideScale`)钻进枝杈之间的空隙(由珊瑚网格自动找出),在空隙之间来回穿梭。一条放大的"天敌"鱼在前方礁石间缓慢 8 字巡游,靠近时附近小鱼更快换到下一个空隙。呼气(天亮)后小鱼回到轨道游动。原有 Tag 1 探头小鱼逻辑不变。
- **构建**:`UnderTheSea/Migration/Build Main With Imported Reef Art` 重新生成 Main。回滚保护改为"迁移前压缩包存在 **或** 处于 Plastic 工作区"(本机没有 `Migration~` 压缩包)。
- **验证**:编辑器静态检查通过(`editor-validation.json`);鱼群/海草在编辑器内逐帧模拟验证(12s 后 48 条中 40 条为躲藏鱼、36 条已在空隙中、13 条在参与者身上;涂胶时海草贴身比例约 0.3)。**Play 模式完整流程检查(`Run Complete Flow Smoke Check`)本轮未能运行**(MCP 不允许进入 Play 模式),需要在编辑器里手动跑一次。真机仍未测试。

## 12. 2026-11-05 第三轮修改

- **鱼躲藏只在天黑**:天黑过渡 + 场景 4;探头检查(白天)时不躲。
- **天黑过渡**:暗度只到白天的 50%(`OceanStageView.nightDarkness = .5`)。水母 = main 近处 3 只 + demo1 远处 3 只(FBX `Jelly_98/99/100`,按 demo1 布局放置,高约 0.87m)。水母以"收缩推进 + 滑行下沉"的脉冲方式一上一下地游来(`ReefArtDriver.UpdateJellies`),过渡结束后保持天黑、水母环绕、不发光。
- **场景 4 呼吸**:`ExperienceController.Breath`(按住右手柄扳机 / F 键 3 秒升到 1,松开 3 秒回到 0,可反复)。驱动:水母放大到 130%(`breathJellyScale`)并发光;`BreathGlow` 设置全局 `_ReefBreathGlow` → `CollabCoral` shader 让珊瑚/海葵/参与者的尖端(网格高度 70% 以上)发微光,并从尖端冒发光颗粒(`BreathSparkle` shader)。主角珊瑚不再自动变大。
- **海葵**:Main 原来没有;现在按 demo1 布局加入 FBX 的 10 只海葵(烘焙为单位高度网格 + CollabCoral 材质,贴地放置)。
- **场景 5**:全部水母约 14 秒内缓慢游走并淡出,不留一只;握手/触手(Tag 2)已删除。
- **场景 6 / 进入转场的鱼群**:每条转场鱼是有速度、有限转向的个体,从 7–11m 外游过来绕圈,散去时掉头游走,不再横移。
- **环境**:新增海平面(`UnderTheSea/SeaSurface` shader,光柱顶部上方,缓慢起伏 + 流动的水纹光 + 头顶更亮)。
- **AprilTag 追踪不到时的默认动画**:Tag 1 丢失 → 探头小鱼沿默认的"探头路线"在身体上游动;Tag 0 丢失 → 海草按默认海浪轻抚。
- **海草 Tag 0 修复**:原来 tag 位置被投影到身体表面、且一开始会误报"已追踪",看起来不跟随。现在附近的海草叶尖直接追 tag 位置,并显示一个标记小球;丢失后回到默认海浪。
- **验证**:编译通过、编辑器静态检查通过;在编辑器内逐帧模拟并截图确认:水母脉冲式到达、呼吸 130%、场景 5 全部离开;转场鱼 4 秒内游近、头朝游动方向;海草跟随 Tag 0。Play 模式流程检查已更新但本轮仍未能运行(MCP 不能进入 Play 模式);真机未测。

## 13. 2026-11-05 第四轮修改

- **场景2 / 场景3 互动互换**(各自的效果不变,只是换了时机):
  - 涂耦合剂:无追踪。"我这一组"小鱼(12 条身体小鱼 + 约 30% 环境小鱼)游进我体内的空隙躲着(`ReefFishSchool.IsGelStage`,`bodyFishStart`,`slotsOnMe = 16`)。
  - 探头检查:这些小鱼离开我回到正常;海草(`SoftSeaweedStrands`)随浪抚摸身体,并跟随**探头 Tag 1**(`trackTagId`)。加强了追踪:追踪更快更紧(`tagFollowStrength = 24`),并有**按压感**:探头在身体上的投影处,海草被压向身体、向四周摊开(`pressStrength`,`pressRadius`)。追踪不到时按默认海浪。
  - 天黑过渡:海草回到正常;我这一组小鱼躲回我体内,其余躲藏鱼躲进附近珊瑚(和上一轮相同),天敌出现。
- **主角珊瑚吸气变大 1.35 倍:恢复**,与水母放大、尖端发光共用同一个呼吸值(按住扳机 3 秒到最大)。
- **吸气时海草包裹主角**:海草沿身体螺旋缠绕(`wrapStrength`),松开扳机后松开。
- **场景4 + 场景5 合并**:场景4 后助手按 A → "场景4·5:天亮,水母游走"(不再自动前进),天亮的同时全部水母游走;再按 A 退出。`Finale` 阶段保留在枚举里但被跳过。
- **开头 / 结尾鱼群 = 沙丁鱼风暴**:60 条更小的鱼(0.45×)从 360° 四面八方、9–13m 外螺旋游来(大部分是绕我的切向速度,不是直冲向我),在我周围 2.3–4.2m、眼睛上下 ±1.3m 绕成一个松散的同向旋转环,离眼睛至少 1.6m,不挡视野;散去时螺旋向外。
- **场景4 天黑时全图闪烁粒子**:`BreathGlow` 新增"Night twinkle":天黑时在我周围 20×5×20m 内缓慢一闪一闪;憋气时更亮、闪得更快、更多。
- **验证**:编译、编辑器静态检查通过;编辑器内逐帧模拟确认:涂胶时 21 条小鱼在我体内、探头时 0 条;探头 tag 追踪 + 按压深度 0.99;天黑时 23 条在我体内、45 条在珊瑚空隙;吸气包裹程度 1.0;风暴鱼群前后均匀、离眼睛 ≥1.6m。夜间闪烁粒子与 Play 模式流程检查需要在编辑器里运行 `Run Complete Flow Smoke Check`(已按新流程更新)。真机未测。

## 14. 2026-11-05 修复:点 Play 后自动终止

- **原因**(Editor.log):编辑器 Play 时 Standalone 设置会自动初始化 OpenXR;电脑上的 OpenXR 运行时是 Meta Horizon Link(Oculus 1.208),没有通过 Link 连着头显时会话立刻 IDLE→EXITING,`OVRManager` 检测到 `OVRPlugin.shouldQuit` 后执行 `EditorApplication.isPlaying = false`。不是项目脚本的错误。
- **修复**:`Assets/Editor/XRPlayModeToggle.cs`。默认关闭 Standalone 的 "Initialize XR on Startup",编辑器 Play = 桌面预览(键盘/鼠标模拟);需要戴头显通过 Quest Link 预览时,打开菜单 **UnderTheSea > XR > Play In Headset (Quest Link)**(需先连好 Link)。Android(Quest 真机构建)仍在启动时初始化 XR,不受影响。
- **另外**:`Packages/manifest.json` 中 OpenXR 曾在本地被改成 1.16.1(导致 OpenXR 编辑器 NullReferenceException),已按用户要求恢复为版本库中的 1.19.0,报错消失。
- **头显(Quest Link)进不去**:开启 "Play In Headset" 后,OpenXR 实例创建失败(`xrEnumerateInstanceExtensionProperties: XR_ERROR_FUNCTION_UNSUPPORTED`)或会话立即退出,日志都经过 `[MetaXROperator]`。原因是 Meta SDK 的 **Meta XR Operator**(AI agent 工具,`Window > Meta > Meta XR Operator`)处于激活状态,每次 Play 都强制注入 OpenXR API 层 `XR_APILAYER_METAX_operator`(`Assets/XR/APILayers~`)。已通过其菜单 **Deactivate**(EditorPrefs `Meta.XR.MetaXROperator.Activated = false`,并从 OpenXR 设置移除该层)。注意:Unity 的 Meta AI Tools 设置可能会再次激活它;若头显又进不去,先检查这里。
- 用 Link 预览的步骤:头显里先开启 Quest Link(Meta Horizon Link 显示已连接)→ Unity 菜单勾选 `UnderTheSea > XR > Play In Headset (Quest Link)` → Play。
- **头显与桌面同时预览(默认)**:编辑器 Play 默认启动 XR。头显通过 Quest Link 连接时,头显里是 VR,Game 视图同步显示头显画面;没有头显时,`Assets/Scripts/XRLinkFallback.cs`(执行顺序 -32000,早于 OVRManager 的 -100,编辑器专用,运行时自动创建)在同一帧检测到 `OVRPlugin.shouldQuit`,停用 OVRManager 并关闭 XR,Play 继续为桌面预览,不再被终止。菜单 `UnderTheSea > XR > Play In Headset (Quest Link)` 默认勾选;取消勾选 = 只用桌面预览、不启动 XR。真机构建不受影响。

## 15. 2026-11-05 修复:Passthrough 现实画面蒙一层半透明蓝色

- URP 管线 `Quest_URP` 关闭 HDR(HDR 颜色缓冲 B10G11R11 没有 alpha,Underlay Passthrough 需要 alpha);`BuildReefMainScene.ConfigurePipeline` 中 `supportsHDR = false`、相机 `allowHDR = false`,重建 Main 不会再打开。
- `OceanStageView.UpdateLighting` 真机分支背景改为预乘 alpha:`new Color(water.r*_vr, water.g*_vr, water.b*_vr, _vr)`,Standby 时为 (0,0,0,0)。
- 已重新构建 `Builds/UnderTheSea.apk` 并安装到 Quest(`com.DefaultCompany.UnderTheSea`),启动无异常、Passthrough API 正常启用。现实画面颜色与"按 A 后海底逐渐显现"需要戴头显目视确认(Quest 截图不包含 Passthrough 画面)。
- 注意:HDR 关闭后 Bloom 等后处理在沉浸阶段的效果会弱一些。

## 16. 2026-11-05 第五轮:水母发光、水母位置、海水渐变

- **水母发光更夸张**:`CollabJelly` shader 呼吸发光时整体提亮、轮廓光加强、透明度提高,并减弱水雾(远处也清楚)。
- **水母发光粒子**:`BreathGlow.EmitJellies`(`jellies`、`art`、`jellySparkleRate`、`jellyHaloSize`):每只可见水母伞盖周围一团随呼吸变大的柔光 + 从伞盖/触手飘散的小颗粒。
- **水母位置**:新增"Moon jellyfish - front left"(主角左前方约 2.4m、前方 3.6m、高 2.4m);视线正前方的那只水母(构建时按水平夹角自动选出,目前是 demo1 远处水母 2)下移 1m。水母共 7 只。
- **浅海→深海渐变**:`UnderwaterSky` shader 改为竖直渐变:抬头 → 浅海青蓝(`_Shallow`),地平线 = 水雾颜色(`unity_FogColor`),低头 → 深海蓝(`_Deep`);远处珊瑚礁淡入水雾时与背景无缝衔接。
- **海平面升高**:光柱顶部上方 4.5m(约 y=14.8)。
- **验证**:编译、Main 重建与静态检查通过;编辑器渲染截图确认白天渐变/远处过渡和夜间水母发光。水母粒子需在 Play 中查看;APK 未重新构建。

## 17. 2026-11-05 AprilTag 真机检测(从 3D-101 项目移植)

- **来源**:`C:\Users\47632\3D-101`(`Assets/AprilTagPrototype/QuestAprilTagTracker.cs` + 内嵌包 `Packages/jp.keijiro.apriltag`)。用法:Meta `PassthroughCameraAccess`(左相机)取图 → Keijiro AprilTag(带 3D-101 的标定内参改动)检测 → 用 SDK 相机位姿和视口射线算出 fx/fy/cx/cy → 世界坐标位姿;启动时申请 `horizonos.permission.HEADSET_CAMERA`,Link 下重试 3 次打开相机。
- **移植到本项目**:
  - 内嵌包复制到 `Packages/jp.keijiro.apriltag`(1.0.3,BSD-2-Clause,仅支持 **tagStandard41h12**)。
  - `Assets/Scripts/QuestAprilTagPoseSource.cs`(实现 `TagPoseSource`,默认只在场景3 `Probe` 检测,10 次/秒,tag 尺寸 0.02777778 m)+ `Assets/Scripts/CompositeTagPoseSource.cs`(真机优先,编辑器鼠标模拟兜底)。`BuildReefMainScene` 中创建 "AprilTag tracking (Quest passthrough camera)" 并设为 `view.tagSource`。
  - `OVRProjectConfig.isPassthroughCameraAccessEnabled = true`;`Assets/Plugins/Android/AndroidManifest.xml` 加入 `HEADSET_CAMERA` 权限与 `com.oculus.feature.PASSTHROUGH`。
- **与水草绑定**:`SoftSeaweedStrands.trackTagId = 1`(`TagIds.Fish` = 探头)。**使用 tagStandard41h12 的 ID 1**,打印 `Documentation/AprilTag/PrintProbeTag.html`(实际大小 100%,含白边 5×5 cm),贴在超声探头上。
- **验证**:编辑器自检 `UnderTheSea > AprilTag > Self Test (synthetic frame)`:合成画面中 ID 1 / ID 0 均被识别,深度 0.1389 m 与理论值一致,空白画面 0 误检。Main 重建 + 静态检查通过。**真机未测**(需要构建 APK、首次启动在头显里允许相机权限)。
- **待办**:患者躺姿下"真实肚子位置 ↔ 虚拟珊瑚身体"的标定尚未做;目前 tag 世界坐标被限制在珊瑚身体周围的范围内(`SoftSeaweedStrands.ClampToRegion`),探头离得远时会贴在范围边缘。

## 18. 2026-11-05 水母呼吸:亮度峰值 -50%,大小峰值 +50%

- `CollabJelly` shader:吸气时附加的亮度(整体提亮、轮廓光、额外白光、透明度提升、穿透水雾)全部减半;伞盖鼓胀 `b*.12 → b*.18`。
- `BreathGlow` 水母光晕透明度 `.32 → .16`(亮度减半;小发光颗粒不变)。
- `ReefArtDriver.breathJellyScale` `1.3 → 1.45`(放大幅度 0.3 → 0.45)。
- Main 已重建并通过静态检查;APK 未重新构建。

## 19. 2026-11-05 音效接入(步骤 6)

- 来源:`Downloads/VR 音效.7z`(8 个 MP3),原文件名保留在 `Assets/Audio/`。由 `Assets/Scripts/ReefSoundscape.cs` 播放(全部 2D),`BuildReefMainScene` 按文件名分配;长音轨流式 Vorbis,短音效载入时解压。
- 对应关系:
  | 文件 | 时机 |
  |---|---|
  | 背景音 泡泡咕嘟咕嘟（常驻） | 水下全程循环,音量随 VR 显现程度(AR 时静音) |
  | 进入VR世界后的背景乐 搭配开场词 | 场景1 中 VR 完全显现后播放一次 |
  | 播放完背景乐和开场词，播放这个 | 上一条播完后接着播放 |
  | 每结束一个场景  放这个 | 按 A 结束场景 1/2/3/4/4·5 时(进入退场时不播,由退场音效代替) |
  | 小鱼靠近珊瑚 | 涂耦合剂开始、天黑过渡开始(小鱼游进珊瑚) |
  | 憋气时背景音 | 场景4 期间循环,淡入淡出 |
  | 憋气时声音 | 按住扳机播放前半段并停在中点;松开播放后半段;可反复 |
  | 退场开始进入ar世界时播放 | 进入退场(VR→AR)时 |
- B 键 / 回到 Start 时停止所有一次性声音,下一轮重新播放开场。
- 验证:8 个音效均已正确导入并赋值,Main 重建 + 静态检查通过;播放时机需在 Play / 真机中试听。APK 未重新构建。

## 20. 2026-11-05 小丑鱼代替 tag 小黄点(场景3 探头检查)

- 素材:`Clownfish_Unity_Package`(微信文件)→ `Assets/Models/Clownfish/Clownfish_Unity.fbx` + `Textures/Clownfish_BaseColor.png`(URP Lit 材质自动关联);说明与预览图在 `Documentation/Clownfish/`。`.blend` / `.glb` 未导入(会触发 Blender / 需额外导入器)。
- 导入:Generic 骨骼,动画改名 `Swim_Loop` 并开启 Loop Time;`Assets/Models/Clownfish/Clownfish.controller` 只有 Swim_Loop 默认状态。外壳 +Z = 鱼头,缩放 0.55(约 0.22m)。均由 `BuildReefMainScene.BuildClownfish` 自动配置。
- 行为:`Assets/Scripts/TagClownfish.cs`(只在 Probe 出现,0.5s 缩放淡入淡出;游速决定 Swim_Loop 播放速度;不会钻进主角珊瑚):
  - tag(ID 1)被识别且在海草控制区域内(`SoftSeaweedStrands.TagInRegion`):临界阻尼跟随,自检中 0.31–0.38s 追上(要求 0.5s 内)。
  - tag 丢失或离开控制区域:在海草和珊瑚周围随机目标点之间自然游动。
- 海草:`SoftSeaweedStrands.interactor` = 小丑鱼,海草跟随/按压点改为小丑鱼位置(自己游动时按压力度 `wanderPress = .45`);鱼身边叶片被拨开(`fishClearRadius`、`fishPushStrength`),看得到鱼在海草里穿行。原小黄点在有小丑鱼时不再显示。
- 验证:编译、Main 重建 + 静态检查通过;编辑器逐帧模拟 + 截图确认跟随/随机游动/拨开海草。APK 未重新构建。

## 21. 2026-11-05 旁白配音 VO(⚠ 对应关系暂按文件顺序推断)

- 来源:微信文件 `VO配音/`(01、02、03.1、03.2、04–08,AI 生成语音,带 C2PA 标记)→ `Assets/Audio/VO/VO_xx.mp3`。没有附带文字稿;Windows 只有中文语音识别,识别结果是乱码,推断配音是**英文**,所以**文本内容尚未核对**。
- 播放:`Assets/Scripts/ReefVoiceOver.cs`,cue 表(阶段 + 触发 + 延迟)在 Inspector 可直接改;一次只播一条、排队;播放时把背景乐/泡泡/憋气背景音压低到 45%(`ReefSoundscape.duck`);回到 Start 时重置。
- 当前对应(按顺序):01 进入场景 VR 完全显现后 0.5s(和开场背景乐一起)· 02 涂耦合剂开始 1s · 03.1 探头检查开始 1s · 03.2 03.1 播完 4s · 04 天黑过渡开始 · 05 场景4 开始 · 06 05 播完 2s · 07 天亮水母游走开始 · 08 退场开始。
- 待办:拿到文字稿(或允许安装英文识别 / Whisper 转写)后按文本内容核对并调整 cue 表。
