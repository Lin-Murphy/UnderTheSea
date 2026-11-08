using UnityEngine;

namespace UnderTheSea
{
    /// <summary>按顺序询问多个 tag 来源,返回第一个识别到的(真机 AprilTag 检测优先,编辑器里再用鼠标模拟)。</summary>
    public sealed class CompositeTagPoseSource : TagPoseSource
    {
        public TagPoseSource[] sources;

        public override bool TryGetPose(int tagId, out Vector3 position, out Quaternion rotation)
        {
            if (sources != null)
                foreach (var s in sources)
                    if (s && s.isActiveAndEnabled && s.TryGetPose(tagId, out position, out rotation)) return true;
            position = default; rotation = Quaternion.identity;
            return false;
        }
    }
}
