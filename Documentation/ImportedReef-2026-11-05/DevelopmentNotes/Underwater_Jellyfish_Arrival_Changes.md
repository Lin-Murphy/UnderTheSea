# 水母与天黑入场过渡

工程：`/Users/winky/Documents/2 工作/一息微光AbreathofLight/Ultrasound_Under the Sea`

## 使用
当前检查场景 `Assets/Scenes/UnderwaterExamPreview.unity` 中：
- Play → Space 进入 gel → Space 进入 Probe → Space 开始天黑、水母入场。
- 快速测试：3 进入 Probe，再按 Space（或 J）。
- 主阶段仍为 Probe。入场不是新增编号阶段；完成后等待操作，不自动进入吸气/屏息。
- P 暂停；R 在入场后重播水母靠近；1 / Escape 返回待机并平滑恢复白天。
- [ / ] 手动减小 / 增大 Breath 目标值，平滑控制伞盖、标记亮度与内光。它不读取真实呼吸。

## 新增
- `Assets/UltrasoundReef/Runtime/JellyfishArrival.cs`：4.5 秒渐暗、延迟 1.2 秒后开始的 6 秒曲线入场；固定世界空间停靠点，暂停、重播、平滑返回白天。
- `Assets/UltrasoundReef/Runtime/JellyfishBreath.cs`：唯一 Breath 目标参数（0–1），SmoothDamp 平滑、手动按键、每个模型部分的动画属性更新。
- `Assets/UltrasoundReef/Editor/JellyfishSetup.cs`：生成模型、材质、Prefab，接入现有检查场景。
- `Assets/UltrasoundReef/Shaders/MoonJellyfish.shader`：透明伞盖 Fresnel 边缘、淡粉标记、飘带/触须的延迟波动；透明混合、关闭深度写入，无新实时光源。
- `Assets/UltrasoundReef/Environment/MoonJellyBell.asset`：960 三角面，带轻微波状伞缘的半球伞盖。
- `Assets/UltrasoundReef/Environment/MoonJellyOralArms.asset`：224 三角面，四条扭曲飘带。
- `Assets/UltrasoundReef/Environment/MoonJellyTentacles.asset`：1760 三角面，十条细触须。
- `Assets/UltrasoundReef/Environment/MoonJellyFourPetals.asset`：3072 三角面，四个粉色圆润椭圆组成的标记。
- `Assets/UltrasoundReef/Environment/Jelly translucent bell.mat`
- `Assets/UltrasoundReef/Environment/Jelly soft ribbons.mat`
- `Assets/UltrasoundReef/Environment/Jelly fine tentacles.mat`
- `Assets/UltrasoundReef/Environment/Jelly pink four petals.mat`
- `Assets/UltrasoundReef/Environment/MoonJellyfish.prefab`
- `Documentation/Backups/UnderwaterExamPreview_before_jellyfish.unity`：首次接入前的场景备份，不放在 Assets 中。
- 对应自动生成的 `.meta` 文件。

## 修改
- `Assets/UltrasoundReef/Runtime/GelSeaweedPreview.cs`：恢复运行中脚本热重载后丢失的 MaterialPropertyBlock 缓存，防止空引用报错。
- `Assets/UltrasoundReef/Runtime/ProbeExamFlow.cs`：Probe 后的 Space/J 触发入场，保留原枚举及所有现有公开字段、探头输入和鱼群逻辑；UI 标示过渡状态及 Breath 调试按键。
- `Assets/UltrasoundReef/Shaders/CoralSunlight.shader`
- `Assets/UltrasoundReef/Shaders/ReefSurface.shader`
- `Assets/UltrasoundReef/Shaders/RootedKelp.shader`
- `Assets/UltrasoundReef/Shaders/ReefFish.shader`
- `Assets/UltrasoundReef/Shaders/SoftLightShaft.shader`
- `Assets/UltrasoundReef/Shaders/UnderwaterSky.shader`
  - 上述 shader 增加统一 `_ReefNightBlend`，默认 0 保持白天效果。入场时降低表面亮度、淡出日光束；结束 Play 后复位。
- `Assets/Scenes/UnderwaterExamPreview.unity`：加入水母及控制引用；旧蓝色水母占位对象保留但隐藏。

## 实现边界
- 四个独立网格/材质部分，约 6016 三角面。没有骨骼，伞盖与触须摆动由顶点 shader 实现。
- Breath 引起的伞盖变化为最多 12%，叠加自动微小脉动后低于约 15%。没有屏息计时、奖励或自动呼吸指令。
- 水母为世界空间对象，不跟着每次转头移动。停靠位置约在初始视点右前方 3 米，预览比例 0.72；仍需在真实躺卧 Quest 视角下验证舒适距离。
- 这里使用手写 URP shader，并非 Shader Graph 节点资源；不含真实透射、折射、软体碰撞或实时体积光。
- 四部分透明度与排序仍需在 Quest 实机检查；尚未做帧率/透明叠层 Profiling，不能宣称 72fps 已达标。
- 本轮只增加入场与手动视觉预览，没有接入自动呼吸检测、临床呼吸流程或告别握手追踪。

## 编辑器验证
- 在现有 UnderwaterExamPreview 中完成生成、编译与 Play 检查。
- 验证 Probe 后 Space 触发夜色变化及水母曲线入场；伞盖与长触须完整出现在右前方。
- 测试暂停、手动 Breath 预览及返回 Idle；白天复位正常。
- 四瓣标记向患者方向倾斜，减少侧视压扁；当前仍为程序生成的风格化模型，与参考图的细腻度有差距。
- 最后一轮 Console 为 0 错误、0 警告；已退出 Play，场景保存为白天待机。
- 尚未完成 Quest 3 实机验证。
