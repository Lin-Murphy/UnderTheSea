#if UNITY_EDITOR
using UnityEngine;

namespace UnderTheSea
{
    /// <summary>Created only by the Editor migration check; never saved to a scene or built.</summary>
    public sealed class ReefVerificationTagSource : TagPoseSource
    {
        public bool valid;
        public Vector3 position;
        public System.Action tick;
        void Update() { tick?.Invoke(); }
        public override bool TryGetPose(int tagId, out Vector3 target, out Quaternion rotation)
        {
            target = position; rotation = Quaternion.identity; return valid;
        }
    }
}
#endif
