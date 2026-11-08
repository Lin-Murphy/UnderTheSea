using System.IO;
using System.Linq;
using AprilTag;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 不需要头显的 AprilTag 自检:把打印用的 tag 图片画进一张合成的 640×480 "相机画面",
/// 用已知内参跑移植过来的检测器,检查 ID 和深度是否正确,并确认空白画面不会误检。
/// 菜单:UnderTheSea > AprilTag > Self Test (synthetic frame)
/// </summary>
public static class AprilTagSelfTest
{
    [MenuItem("UnderTheSea/AprilTag/Self Test (synthetic frame)")]
    static void Menu() => Debug.Log(Run(1));

    public static string Run(int tagId)
    {
        string path = $"Documentation/AprilTag/tag41_12_{tagId:00000}.png";
        if (!File.Exists(path)) return "Missing " + path;
        var tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(path));
        int W = 640, H = 480, s = 20;
        float fx = 500, fy = 500, cx = 320, cy = 240, tagSize = .05f * 5f / 9f;
        var px = Enumerable.Repeat(new Color32(255, 255, 255, 255), W * H).ToArray();
        int n = tex.width, ox = W / 2 - n * s / 2, oy = H / 2 - n * s / 2;
        for (int y = 0; y < n * s; y++)
            for (int x = 0; x < n * s; x++)
                px[(oy + y) * W + ox + x] = tex.GetPixel(x / s, y / s);   // both bottom-up
        Object.DestroyImmediate(tex);
        float expected = fx * tagSize / (5 * s);
        string report;
        using (var det = new TagDetector(W, H, 1))
        {
            det.ProcessImage(px, fx, fy, cx, cy, tagSize);
            var tags = det.DetectedTags.ToList();
            report = $"[AprilTag self-test] tag {tagId}: {tags.Count} detection(s) " +
                     string.Join(", ", tags.Select(t => $"ID {t.ID} pos {t.Position.x:F4},{t.Position.y:F4},{t.Position.z:F4}")) +
                     $" | expected depth {expected:F4} m";
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            det.ProcessImage(px, fx, fy, cx, cy, tagSize);
            report += $" | blank frame detections: {det.DetectedTags.Count()}";
        }
        return report;
    }
}
