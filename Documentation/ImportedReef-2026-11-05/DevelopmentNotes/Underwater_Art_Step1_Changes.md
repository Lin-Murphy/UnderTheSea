# 海底环境美术：第一步改动清单

工程根目录：`/Users/winky/Documents/2 工作/一息微光AbreathofLight/Ultrasound_Under the Sea`

## 本轮新增
- `Assets/UltrasoundReef/Editor/CoralPaletteStep.cs`：五种低面数粗枝珊瑚、统一配色和覆盖水草。
- `Assets/UltrasoundReef/Editor/ReefGroundingStep.cs`：珊瑚/水草落点修正、镜像珊瑚支撑岩石、相机近处装饰物避让。
- `Assets/UltrasoundReef/Environment/RoundedCoral_0.asset`：鹿角型，384 三角面。
- `Assets/UltrasoundReef/Environment/RoundedCoral_1.asset`：扇形，432 三角面。
- `Assets/UltrasoundReef/Environment/RoundedCoral_2.asset`：粗指型，384 三角面。
- `Assets/UltrasoundReef/Environment/RoundedCoral_3.asset`：小簇型，336 三角面。
- `Assets/UltrasoundReef/Environment/RoundedCoral_4.asset`：高分枝型，384 三角面。
- Unity 为新脚本/模型自动生成对应 `.meta` 文件。

## 本轮修改
- `Assets/UltrasoundReef/Shaders/CoralSunlight.shader`：根部到奶油尖端的高度渐变、柔和分段明暗、暖边缘光、`_GlowIntensity`；保留 `_Glow` 与 shader 名称。
- `Assets/UltrasoundReef/Runtime/GelSeaweedPreview.cs`：保留全部原有公开字段及 `SetGel` 方法、UI、按键；增加珊瑚引用用于叶片边界避让。开场覆盖，触发 gel 后仅弯曲叶片，根部不平移/旋转。叶片统一向世界空间外侧展开。脚本重载时重新建立运行时网格。
- `Assets/UltrasoundReef/Environment/Peach coral.mat`：#F6B58F。
- `Assets/UltrasoundReef/Environment/Golden coral.mat`：#FBE3A8。
- `Assets/UltrasoundReef/Environment/Lavender coral.mat`：保留旧文件名和引用，颜色改为 #E88F8A，无紫色。
- `Assets/UltrasoundReef/Environment/Sand turquoise.mat`：保留旧文件名和引用，改暖沙色 #E9DDBA。
- `Assets/UltrasoundReef/Environment/Rock ocean blue.mat`：#3F8F9A。
- `Assets/UltrasoundReef/Environment/Wide kelp jade.mat`：#8FA77E。
- `Assets/UltrasoundReef/Environment/Seaweed jade.mat`：#8FA77E。
- `Assets/UltrasoundReef/Environment/UnderwaterGarden.prefab`：更新珊瑚与落点。
- `Assets/Scenes/UnderwaterGarden.unity`：更新环境珊瑚、配色和落点。
- `Assets/Scenes/UnderwaterGelPreview.unity`：上述环境修改，加覆盖水草及固定根部的互动引用。

## 使用
打开 `UnderwaterGelPreview`，Play 后 1=覆盖待机，2=海浪拨开，Space=切换，P=暂停，R=重播，鼠标拖动转头，Home=复位。

## 本步限制与后续
- 使用手写 URP shader，未生成 Shader Graph 节点文件。
- 根部固定、初始覆盖和叶片拨开是人工触发预览；没有 AprilTag 或真实凝胶检测。
- 叶片使用珊瑚包围盒进行保守避让，不是完整的软体/网格碰撞系统。仍需检查不同视角的局部交叠。
- 水草仍使用现有 CPU 网格变形；文本要求的 GPU 顶点摆动属于第 4 步。
- 岩石重建、柔和焦散/水雾/光束、水母与 Breath 参数、鱼群与粒子按指定后续步骤处理。本步只调整这些对象的基础配色，没有完成其重建。
- 尚未在 Quest 实机使用 Profiler 测量帧率、draw calls 或透明叠层，不能宣称达到 72fps。
