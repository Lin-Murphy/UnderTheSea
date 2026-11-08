using UnityEngine;
using UnityEngine.InputSystem;
namespace UltrasoundReef {
 public class JellyfishBreath : MonoBehaviour {
  [Range(0,1)] public float Breath;
  public bool paused;
  public bool handleInput=true;
  [Range(0,1)] public float Visibility=1;
  public float SmoothedBreath=>smoothed;
  float smoothed,velocity,clock;Renderer[] parts;MaterialPropertyBlock block;
  void OnEnable(){parts=GetComponentsInChildren<Renderer>(true);block=new MaterialPropertyBlock();}
  void Update(){
   if(paused)return;var k=Keyboard.current;
   if(handleInput && k!=null){if(k.leftBracketKey.isPressed)Breath=Mathf.Clamp01(Breath-Time.deltaTime*.35f);if(k.rightBracketKey.isPressed)Breath=Mathf.Clamp01(Breath+Time.deltaTime*.35f);}
   smoothed=Mathf.SmoothDamp(smoothed,Mathf.Clamp01(Breath),ref velocity,.75f);clock+=Time.deltaTime;
   foreach(var part in parts){part.GetPropertyBlock(block);block.SetFloat("_Breath",smoothed);block.SetFloat("_Visibility",Visibility);block.SetFloat("_JellyTime",clock);part.SetPropertyBlock(block);}
  }
 }
}
