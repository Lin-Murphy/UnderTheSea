# Ultrasound Wonders: Under the Sea 🪸

**Real scan moments, made meaningful.** 面向超声检查中儿童患者的 AR → VR → AR 安抚体验(Meta Quest 3)。孩子戴上头显变成海底的一株珊瑚,检查中的每种感受都变成故事的一部分。

Team 2 在 [ARR Hackathon](https://www.scivr.co.uk/arr-hackathon)(2026.11.4–6,Goldsmiths, University of London)的作品,获 ARR 项目负责人颁发的🏅**提名奖**。

https://github.com/user-attachments/assets/56a70fdd-ee4f-47b5-a6c8-9050c689c04b

[1080p 视频](https://github.com/Lin-Murphy/UnderTheSea/releases/download/v0.1-cs15/Ultrasound-Wonders-Demo-1080p.mp4) · [Quest 3 APK](https://github.com/Lin-Murphy/UnderTheSea/releases/tag/v0.1-cs15)

## 设计

| 检查中的感受 | 在故事里变成 |
|---|---|
| 冰凉的耦合剂 | 一群小鱼游来,轻触你这株珊瑚 |
| 探头按压 | 海草和小丑鱼跟随探头(AprilTag)轻抚身体 |
| 吸气憋气 | 水母变大、珊瑚发光、世界变亮;呼气时恢复 |

由人而不是传感器连接检查与大海:**医生**主导检查和呼吸指令,**助手**用右手柄操作 VR 世界,**患者**戴头显参与其中。

## 运行

- Unity **6000.3.27f2**、URP 17.3、Meta XR SDK 207.0.1、OpenXR 1.19.0,目标 Android / Quest 3
- 打开 `Assets/Scenes/Main.unity` 点 Play(支持 Quest Link,无头显时退回桌面预览)
- 探头 tag:**tagStandard41h12 ID 1**,5×5 cm,打印页见 [`Documentation/AprilTag/`](Documentation/AprilTag/)
- `Main.unity` 由 `UnderTheSea > Migration > Build Main With Imported Reef Art` 生成,改动请写进 [`BuildReefMainScene.cs`](Assets/Editor/BuildReefMainScene.cs)

| 操作 | 作用 |
|---|---|
| 右手柄 A / 空格 | 开始 / 下一阶段 |
| 右手柄 B / 键盘 B | 回到现实 |
| 按住扳机 / 键盘 F | 吸气憋气,松开呼气 |

## 制作人

Renee Wen · Murphy Lin · Kevin Zheng · Haoxin Liang

## 许可证

源代码以 [MIT License](LICENSE) 开源。[`Packages/jp.keijiro.apriltag`](Packages/jp.keijiro.apriltag) 为 BSD 2-Clause;美术与音频素材版权归各自作者所有。本项目为原型,不是医疗器械。
