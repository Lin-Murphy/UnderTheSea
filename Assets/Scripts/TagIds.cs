using UnityEngine;

namespace UnderTheSea
{
    /// <summary>
    /// AprilTag 编号分配(与 CLAUDE.md 保持一致)。
    /// </summary>
    public static class TagIds
    {
        /// <summary>Tag 0 — 水草:助手手持,场景2 水草抚摸身体。</summary>
        public const int Seaweed = 0;
        /// <summary>Tag 1 — 小鱼:贴在超声探头上,场景3/4 小鱼群跟随探头。</summary>
        public const int Fish = 1;
        /// <summary>Tag 2 — 水母:贴在毛绒玩具上,场景5 水母握手/拥抱。</summary>
        public const int Jelly = 2;

        public static string NameOf(int id)
        {
            switch (id)
            {
                case Seaweed: return "水草 Seaweed";
                case Fish:    return "小鱼(探头)Fish";
                case Jelly:   return "水母(毛绒玩具)Jellyfish";
                default:      return "未知 Unknown";
            }
        }
    }

    /// <summary>
    /// AprilTag 位姿来源的抽象。表现层只依赖这个接口,
    /// 编辑器里用 MockTagPoseSource(鼠标模拟),真机用检测器实现(待定)。
    /// 返回的位姿为 Unity 世界坐标。tag 当前未被识别时返回 false。
    /// </summary>
    public abstract class TagPoseSource : MonoBehaviour
    {
        public abstract bool TryGetPose(int tagId, out Vector3 position, out Quaternion rotation);
    }
}
