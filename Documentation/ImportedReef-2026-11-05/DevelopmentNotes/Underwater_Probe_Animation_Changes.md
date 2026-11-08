# 探头检查、鱼群与水草动画更新

工程根目录：`/Users/winky/Documents/2 工作/一息微光AbreathofLight/Ultrasound_Under the Sea`

## 运行
打开 `Assets/Scenes/UnderwaterExamPreview.unity`，点击 Play 并让 Game 窗口获得焦点。
- Space：待机覆盖 → 涂 gel 拨开 → 探头检查。探头阶段再次按 Space 保持当前阶段，不自动进入尚未完成的呼吸流程。
- 1 / 2 / 3：直接选择待机 / gel / 探头阶段。
- 方向键或 WASD：在患者珊瑚前侧移动模拟探头接触位置。
- Q / E：抬离 / 靠近虚拟表面。这是演示控制，不是压力测量。
- 琥珀色小点：模拟探头位置。
- P：暂停鱼群、gel 动画、水草摆动与粒子；R：重播当前阶段；Escape / 1：待机。
- 左键或右键拖动：桌面转头；Home：复位。探头阶段增加平滑的 14°向下视角偏移；XR 设备启用时不应用桌面相机旋转。

## 新增文件
- `Assets/UltrasoundReef/Runtime/ProbeExamFlow.cs`：阶段控制、模拟探头输入、患者/镜像鱼群、背景鱼群、实例渲染与暂停。
- `Assets/UltrasoundReef/Editor/ProbeExamSetup.cs`：生成独立检查场景及资源。
- `Assets/UltrasoundReef/Shaders/RootedKelp.shader`：固定根部的 GPU 水草摆动和 gel 拨开、珊瑚包围盒避让、叶尖浅色。
- `Assets/UltrasoundReef/Shaders/ReefFish.shader`：低幅度鱼尾顶点摆动、银蓝身体和淡黄鱼鳍。
- `Assets/UltrasoundReef/Shaders/ReefMote.shader`：柔边、低透明度漂浮粒子。
- `Assets/UltrasoundReef/Environment/WideKelpGPU.asset`：336 三角面，去掉原网格重复背面；shader 双面显示，扩展包围盒避免动画被过早裁剪。
- `Assets/UltrasoundReef/Environment/PastelReefFish.asset`：56 三角面。
- `Assets/UltrasoundReef/Environment/Rooted kelp GPU.mat`
- `Assets/UltrasoundReef/Environment/Pastel silver fish GPU.mat`
- `Assets/UltrasoundReef/Environment/Soft underwater motes.mat`
- `Assets/UltrasoundReef/Environment/Simulated probe marker.mat`
- `Assets/Scenes/UnderwaterExamPreview.unity`
- 对应 Unity 自动生成的 `.meta` 文件。

## 修改文件
- `Assets/UltrasoundReef/Runtime/GelSeaweedPreview.cs`：保留现有公开字段与 SetGel；新增 handleInput、showUI、useGpuDeformation 选项。新场景通过 GPU 路径执行叶片变形，不逐帧复制顶点和重算网格法线。旧 gel 场景继续使用原有路径和 UI。
- `Assets/UltrasoundReef/Runtime/ReefDesktopLook.cs`：保留 sensitivity / pitchLimit；新增 SetStagePitch，平滑叠加阶段视角，保留鼠标与 Home 复位。
- `Assets/UltrasoundReef/Environment/WideKelp.asset`：首次预览生成时扩展了原模型包围盒；最终场景使用独立 WideKelpGPU 网格。

## 实现范围
- 32 条缓慢游动的背景鱼；探头阶段额外显示患者与镜像处各 12 条鱼，合计最多 56 条。
- 使用 `Graphics.RenderMeshInstanced` 绘制共享鱼网格；鱼群位置由 CPU 更新，鱼尾由顶点 shader 摆动。背景使用平滑群游路径，未实现完整 boids 分离/避障算法。
- 接触鱼群从珊瑚前侧靠近，目标点保持在珊瑚包围盒前方；群内拉开间距。不是精确皮肤/探头碰撞或物理压力模拟。
- 水草根部及世界坐标不平移；仅叶片顶点变形。GPU 包围盒避让用于减少穿模，不等于完整软体碰撞或所有视角无交叠保证。
- 粒子最大 96 个，每秒 3 个，16–22 秒寿命，缓慢漂浮与渐入渐出。
- `externalProbeTarget` 与 `externalTrackingValid` 为后续世界空间追踪输入接口。缺少有效外部数据时不会宣称跟踪真实探头；未实现 AprilTag 检测、相机标定或 Quest 空间对齐。

## 验证
- Unity 成功编译并生成检查预览场景。
- 实测 Space：Idle → Gel → Probe，Probe 的相机平滑向下调整。
- 在 Probe 阶段按方向键，观察患者鱼群、琥珀色接触点及镜像反馈移动。
- 实测 P 暂停、1 返回待机并退出 Play；最终 Console 0 错误、0 警告。
- 资产检查：鱼模型 56 面；原水草 672 面，GPU 版本 336 面。
- 鼠标拖动逻辑保留并更新；自动拖动检查未观察到明确视角变化，需实际鼠标体验确认。
- 未在 Quest 3 上验证 Vulkan / Single Pass Instanced / 帧率或实际 draw calls；不能据此宣称 72fps 已达标。

水母新模型、Breath 参数及天黑入场过渡仍未实现，不属于这一轮修改。
