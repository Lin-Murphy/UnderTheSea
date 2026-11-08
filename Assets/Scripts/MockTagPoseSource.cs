using UnityEngine;

namespace UnderTheSea
{
    /// <summary>
    /// 编辑器里用鼠标模拟 AprilTag:按住某个鼠标键,对应 tag"被识别",
    /// 位置由鼠标在屏幕上的位置映射到肚子区域上方的平面。
    ///   左键 = Tag 1(小鱼/探头)   右键 = Tag 0(水草)   中键 = Tag 2(水母)
    /// </summary>
    public class MockTagPoseSource : TagPoseSource
    {
        public Transform bellyPoint;
        public Vector3 area = new Vector3(0.45f, 0f, 0.35f);

        public override bool TryGetPose(int tagId, out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = Quaternion.identity;

            int button;
            switch (tagId)
            {
                case TagIds.Fish:    button = 0; break;
                case TagIds.Seaweed: button = 1; break;
                case TagIds.Jelly:   button = 2; break;
                default: return false;
            }
            if (!Input.GetMouseButton(button) || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) return false;

            float nx = Mathf.Clamp01(Input.mousePosition.x / Screen.width) * 2f - 1f;
            float ny = Mathf.Clamp01(Input.mousePosition.y / Screen.height) * 2f - 1f;
            position = bellyPoint.position + new Vector3(nx * area.x, 0.05f, ny * area.z);
            return true;
        }
    }
}
