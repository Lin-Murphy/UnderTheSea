using System;
using System.Collections;
using System.Collections.Generic;
using AprilTag;
using Meta.XR;
using UnityEngine;
using UnityEngine.Android;

namespace UnderTheSea
{
    /// <summary>
    /// 真机 AprilTag 检测(移植自 3D-101 项目的 QuestAprilTagTracker):
    /// Quest 3 / 3S 透视相机(Meta <see cref="PassthroughCameraAccess"/>)→ Keijiro AprilTag 检测器(tagStandard41h12)
    /// → 用 SDK 给的相机位姿与标定内参(fx/fy/cx/cy)换算成 Unity 世界坐标位姿。
    ///
    /// - 标记族:tagStandard41h12(不是 tag36h11)。打印 5 × 5 cm(含白边)时检测角点跨度 = 0.02777778 m。
    /// - 需要 Android 权限 horizonos.permission.HEADSET_CAMERA(启动时请求)和 OVRProjectConfig 的 Passthrough Camera Access。
    /// - 只在 <see cref="activeStages"/> 指定的阶段检测(默认只在场景3 探头检查),其余时间不占 CPU。
    /// - 同步读取图像 + CPU 检测,默认 10 次/秒;位姿不做平滑(海草本身会平滑跟随)。
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public sealed class QuestAprilTagPoseSource : TagPoseSource
    {
        [Tooltip("AprilTag 检测角点之间的距离(米),不是纸张大小。5 cm 打印 = 0.02777778")]
        [Min(.001f)] public float tagSizeMetres = .05f * 5f / 9f;
        [Range(1, 30)] public int detectionHz = 10;
        [Range(1, 4)] public int decimation = 2;
        [Tooltip("超过多少秒没看到就认为丢失")]
        [Min(.05f)] public float lostTimeout = .4f;
        public PassthroughCameraAccess cameraAccess;
        [Tooltip("可选:只在这些阶段检测(为空 = 一直检测)")]
        public ExperienceController controller;
        public Stage[] activeStages = { Stage.Probe };

        struct Observation { public Pose pose; public float time; }
        readonly Dictionary<int, Observation> observations = new Dictionary<int, Observation>();

        TagDetector detector;
        Color32[] pixels;
        Vector2Int detectorSize;
        float nextDetection;
        bool failed;
        const string CameraPermission = "horizonos.permission.HEADSET_CAMERA";

        /// <summary>当前状态说明(调试用)。</summary>
        public string Status { get; private set; } = "Starting camera…";
        /// <summary>最近一次检测到的 tag ID 列表(调试用)。</summary>
        public IReadOnlyCollection<int> SeenIds => observations.Keys;

        public override bool TryGetPose(int tagId, out Vector3 position, out Quaternion rotation)
        {
            if (isActiveAndEnabled && !failed && observations.TryGetValue(tagId, out var value) &&
                Time.unscaledTime - value.time <= lostTimeout)
            {
                position = value.pose.position; rotation = value.pose.rotation;
                return true;
            }
            position = default; rotation = Quaternion.identity;
            return false;
        }

        IEnumerator Start()
        {
            if (!cameraAccess) { SetFailure("No PassthroughCameraAccess assigned."); yield break; }
            cameraAccess.enabled = false;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(CameraPermission))
            {
                bool finished = false;
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += _ => finished = true;
                callbacks.PermissionDenied += _ => finished = true;
                Permission.RequestUserPermission(CameraPermission, callbacks);
                while (!finished) yield return null;
                if (!Permission.HasUserAuthorizedPermission(CameraPermission))
                {
                    SetFailure("Camera permission denied. Enable it in app permissions and restart.");
                    yield break;
                }
            }
#endif
            if (!PassthroughCameraAccess.IsSupported)
            {
                // 编辑器桌面预览 / 不支持的设备:静默退出,由组合来源里的鼠标模拟接管
                SetFailure("Passthrough camera not supported here (needs Quest 3 / 3S).");
                yield break;
            }
            // Link 上相机可能在 XR 会话启动后才可用;SDK 在 CameraPlay 失败时会禁用组件,所以显式重试。
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                Status = $"Connecting camera ({attempt}/3)…";
                yield return new WaitForSecondsRealtime(attempt == 1 ? 1f : 2f);
                cameraAccess.enabled = false;
                cameraAccess.enabled = true;
                float deadline = Time.unscaledTime + 5f;
                while (cameraAccess.enabled && !cameraAccess.IsPlaying && Time.unscaledTime < deadline)
                    yield return null;
                if (cameraAccess.IsPlaying) { Status = "Camera ready"; yield break; }
            }
            Status = "CAMERA UNAVAILABLE - no images received (check Passthrough Camera API permission).";
            Debug.LogWarning("[AprilTag] " + Status);
        }

        bool StageActive()
        {
            if (!controller || activeStages == null || activeStages.Length == 0) return true;
            foreach (var s in activeStages) if (controller.Current == s) return true;
            return false;
        }

        void Update()
        {
            if (failed || !cameraAccess || !cameraAccess.IsPlaying || !cameraAccess.IsUpdatedThisFrame) return;
            if (!StageActive() || Time.unscaledTime < nextDetection) return;
            nextDetection = Time.unscaledTime + 1f / detectionHz;
            try { Detect(); }
            catch (Exception e)
            {
                Debug.LogException(e, this);
                SetFailure("Detection failed: " + e.Message);
            }
        }

        void Detect()
        {
            var size = cameraAccess.CurrentResolution;
            if (detector == null || detectorSize != size)
            {
                detector?.Dispose();
                detector = new TagDetector(size.x, size.y, decimation);
                detectorSize = size;
                pixels = new Color32[size.x * size.y];
            }
            // 先缓存这张图对应的相机位姿,再做 CPU 处理
            var cameraPose = cameraAccess.GetCameraPose();
            var colors = cameraAccess.GetColors();
            if (!colors.IsCreated) return;
            colors.CopyTo(pixels);

            // 用 SDK 的视口射线求内参(包含传感器裁剪、分辨率与主点偏移)
            var originRay = cameraAccess.ViewportPointToRay(Vector2.zero, cameraPose);
            var endRay = cameraAccess.ViewportPointToRay(Vector2.one, cameraPose);
            var inverse = Quaternion.Inverse(cameraPose.rotation);
            var a = inverse * originRay.direction;
            var b = inverse * endRay.direction;
            a /= a.z; b /= b.z;
            float fx = size.x / (b.x - a.x), fy = size.y / (b.y - a.y);
            float cx = -a.x * fx, cy = size.y + a.y * fy;
            // 插件会把 Unity 自下而上的像素翻转为自上而下再检测
            detector.ProcessImage(pixels, fx, fy, cx, cy, tagSizeMetres);
            int count = 0;
            foreach (var tag in detector.DetectedTags)
            {
                if (tag.Position.z <= 0 || !float.IsFinite(tag.Position.sqrMagnitude)) continue;
                var position = cameraPose.position + cameraPose.rotation * tag.Position;
                var rotation = cameraPose.rotation * tag.Rotation;
                observations[tag.ID] = new Observation { pose = new Pose(position, rotation), time = Time.unscaledTime };
                count++;
            }
            Status = count > 0 ? $"Tracking {count} tag(s)" : "Looking for tagStandard41h12…";
        }

        void SetFailure(string message)
        {
            failed = true;
            Status = message;
            observations.Clear();
            if (cameraAccess) cameraAccess.enabled = false;
        }

        void OnDisable()
        {
            detector?.Dispose();
            detector = null;
            observations.Clear();
        }
    }

}
