# 海底环境：第二步 岩石、沙地与水下光照

工程根目录：`/Users/winky/Documents/2 工作/一息微光AbreathofLight/Ultrasound_Under the Sea`

## 新增文件
- `Assets/UltrasoundReef/Editor/ReefAtmosphereStep.cs`：第二步编辑器生成及应用工具。
- `Assets/UltrasoundReef/Shaders/ReefSurface.shader`：柔和表面明暗、浅色顶面、世界坐标投影的慢速焦散。
- `Assets/UltrasoundReef/Shaders/SoftLightShaft.shader`：双面低透明加色光束；无体积采样，关闭深度写入。
- `Assets/UltrasoundReef/Environment/SoftRock_A.asset`
- `Assets/UltrasoundReef/Environment/SoftRock_B.asset`
- `Assets/UltrasoundReef/Environment/SoftRock_C.asset`
- `Assets/UltrasoundReef/Environment/NaturalReefArch.asset`
- `Assets/UltrasoundReef/Environment/SoftShaftPlane.asset`
- `Assets/UltrasoundReef/Environment/SoftCaustics.png`：256×256 可平铺焦散贴图，线性采样、mipmap。
- `Assets/UltrasoundReef/Environment/Soft sunlight shafts.mat`
- 对应 Unity 自动生成的 `.meta` 文件。

## 修改文件
- `Assets/UltrasoundReef/Shaders/UnderwaterSky.shader`：上下水色渐变、降低太阳光晕；移除旧天空中的放射纹理，使用场景中的五片网格光束。
- `Assets/UltrasoundReef/Environment/Rock ocean blue.mat`：中青色、较浅顶面、微弱焦散。
- `Assets/UltrasoundReef/Environment/Sand turquoise.mat`：暖沙色、低对比焦散。
- `Assets/UltrasoundReef/Environment/UnderwaterVolume.asset`：Bloom 0.1、threshold 1.5，曝光 -0.15，饱和度 2，对比度 3；同时更新其嵌入的 VolumeComponent 子资源。
- `Assets/UltrasoundReef/Environment/UnderwaterGarden.prefab`：替换岩石、保留旧拱门对象但关闭显示；加入连续拱门、更新植被落点。
- `Assets/Scenes/UnderwaterGarden.unity`
- `Assets/Scenes/UnderwaterGelPreview.unity`

## 场景变化
- 保留现有脚本、公开字段、UI、快捷键、鼠标视角逻辑；本步没有改动运行时交互脚本。
- 岩石采用可复用的低面数柔和棱面网格；一个连续拱门替代球体串。
- 更换岩石后，使用编辑器临时 MeshCollider 向下射线重新放置珊瑚及装饰水草；射线使用的临时 Collider 在保存前移除。
- Exponential Squared 雾，基色 #2BA6C8，密度 0.025。雾本身是单色；远近色差由雾、天空渐变和物体本色共同形成。
- 五片光束缓慢、低幅度摆动，使用相同材质。不使用屏幕空间或体积光束。
- 实时方向光关闭阴影；没有增加其它实时光源，也没有执行光照烘焙。

## 验证与限制
- 已在 Unity 内生成、保存，并查看实际 Game 画面。
- 焦散用表面 shader 的世界坐标投影，而非 URP Decal 或主光 Cookie；适配已有管线，无新增 renderer feature。
- 当前仍用手写 URP shader，非 Shader Graph。
- 未在 Quest 3 上运行 Profiler，未验证 72fps、实际 draw calls、批处理或透明叠层开销。静态批处理标记不等于实机已验证合批。
- 原有水母、鱼群及水草模型仍是前一步版本。水母与 Breath、鱼群/水草顶点动画、粒子按后续步骤完成。
