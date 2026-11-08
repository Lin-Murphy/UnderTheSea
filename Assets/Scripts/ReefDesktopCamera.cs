using UnityEngine;
using UnityEngine.XR;

namespace UnderTheSea
{
    // Run after OVRCameraRig's desktop anchor update. Real headset poses are never overwritten.
    [DefaultExecutionOrder(1000)]
    public sealed class ReefDesktopCamera : MonoBehaviour
    {
        public Transform cameraTransform;
        public ExperienceController controller;
        public Quaternion homeRotation = Quaternion.identity;
        public float sensitivity = .12f;
        float yaw, pitch, stagePitch;

        void LateUpdate()
        {
            if (XRSettings.isDeviceActive || !cameraTransform) return;
            if (Input.GetKeyDown(KeyCode.Home)) yaw = pitch = 0f;
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetMouseButton(0))
            {
                yaw += Input.GetAxisRaw("Mouse X") * sensitivity * 10f;
                pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * sensitivity * 10f, -75f, 75f);
            }
            float target = controller.Current == Stage.Probe || controller.Current == Stage.DuskTransition ? 14f : 0f;
            stagePitch = Mathf.Lerp(stagePitch, target, 1f - Mathf.Exp(-Time.deltaTime * 1.8f));
            cameraTransform.localRotation = homeRotation * Quaternion.Euler(pitch + stagePitch, yaw, 0f);
        }
    }
}
