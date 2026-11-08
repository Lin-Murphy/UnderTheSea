using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
namespace UltrasoundReef {
 [DisallowMultipleComponent]
 public class ReefDesktopLook : MonoBehaviour {
  [Range(.02f,.5f)] public float sensitivity=.12f;
  public float pitchLimit=85;
  Quaternion initial;float yaw,pitch,stagePitch,targetStagePitch;bool dragging;
  void OnEnable(){initial=transform.localRotation;yaw=pitch=0;stagePitch=targetStagePitch=0;dragging=false;}
  void OnApplicationFocus(bool focus){dragging=false;}
  public void SetStagePitch(float degrees){targetStagePitch=Mathf.Clamp(degrees,-20,20);}
  void Update(){
   if(XRSettings.isDeviceActive)return;
   var m=Mouse.current;var k=Keyboard.current;
   if(k!=null&&k.homeKey.wasPressedThisFrame){yaw=pitch=0;}
   if(m==null)return;bool down=m.leftButton.isPressed||m.rightButton.isPressed;
   if(!down){dragging=false;return;}
   if(!dragging){var p=m.position.ReadValue();if(p.x<580&&p.y>Screen.height-270)return;dragging=true;}
   Vector2 delta=m.delta.ReadValue();yaw+=delta.x*sensitivity;pitch=Mathf.Clamp(pitch-delta.y*sensitivity,-pitchLimit,pitchLimit);
  }
  void LateUpdate(){if(XRSettings.isDeviceActive)return;stagePitch=Mathf.Lerp(stagePitch,targetStagePitch,1-Mathf.Exp(-Time.deltaTime*1.8f));transform.localRotation=initial*Quaternion.Euler(Mathf.Clamp(pitch+stagePitch,-pitchLimit,pitchLimit),yaw,0);}
 }
}
