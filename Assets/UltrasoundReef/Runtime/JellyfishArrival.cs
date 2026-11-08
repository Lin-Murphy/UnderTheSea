using UnityEngine;
namespace UltrasoundReef {
 public class JellyfishArrival : MonoBehaviour {
  public ProbeExamFlow flow;
  public Transform jellyfishRoot;
  public JellyfishBreath breath;
  public float darkenSeconds=4.5f,arrivalSeconds=6;
  public bool IsActive=>active;
  public bool IsComplete=>active&&elapsed>=arrivalSeconds+1.2f;
  public float NightBlend=>night;
  bool initialised,active;float elapsed,night,startingNight;
  Color dayFog,dayAmbient;Light sun;float dayIntensity;Vector3 start,end;
  void Init(){if(initialised)return;initialised=true;dayFog=RenderSettings.fogColor;dayAmbient=RenderSettings.ambientSkyColor;sun=RenderSettings.sun;if(!sun){var o=GameObject.Find("Filtered sunlight");if(o)sun=o.GetComponent<Light>();}dayIntensity=sun?sun.intensity:1.45f;}
  public void Begin(){
   Init();if(active)return;var cam=Camera.main;startingNight=night;elapsed=0;active=true;
   Quaternion yaw=Quaternion.Euler(0,cam.transform.eulerAngles.y,0);
   start=cam.transform.position+yaw*new Vector3(3.7f,2.2f,6);end=cam.transform.position+yaw*new Vector3(1,.15f,2.8f);
   jellyfishRoot.position=start;breath.Visibility=0;breath.Breath=0;jellyfishRoot.gameObject.SetActive(true);
   Debug.Log("Jellyfish arrival: transition after probe stage; no breathing instruction is triggered.");
  }
  public void ResetToDay(){Init();active=false;elapsed=0;if(night<=.001f){night=0;jellyfishRoot.gameObject.SetActive(false);ApplyNight();}}
  public void Replay(){active=false;Begin();}
  void Update(){
   Init();breath.paused=flow&&flow.paused;if(flow&&flow.paused)return;
   if(active){
    elapsed+=Time.deltaTime;float d=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/Mathf.Max(.1f,darkenSeconds)));night=Mathf.Lerp(startingNight,1,d);
    float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-1.2f)/Mathf.Max(.1f,arrivalSeconds)));
    Vector3 c1=start+Vector3.up*.35f,c2=end+new Vector3(.65f,.6f,.8f);float u=1-t;
    jellyfishRoot.position=u*u*u*start+3*u*u*t*c1+3*u*t*t*c2+t*t*t*end+Vector3.up*Mathf.Sin(elapsed*.8f)*.025f*t;
    breath.Visibility=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-.6f)/2.2f));
   }else if(night>0){night=Mathf.MoveTowards(night,0,Time.deltaTime/2.5f);breath.Visibility=night;if(night<=0)jellyfishRoot.gameObject.SetActive(false);}
   ApplyNight();
  }
  void ApplyNight(){Shader.SetGlobalFloat("_ReefNightBlend",night);RenderSettings.fogColor=Color.Lerp(dayFog,new Color(.018f,.045f,.095f),night);RenderSettings.ambientSkyColor=Color.Lerp(dayAmbient,new Color(.035f,.07f,.12f),night);if(sun)sun.intensity=Mathf.Lerp(dayIntensity,.16f,night);}
  void OnDisable(){Shader.SetGlobalFloat("_ReefNightBlend",0);if(initialised){RenderSettings.fogColor=dayFog;RenderSettings.ambientSkyColor=dayAmbient;if(sun)sun.intensity=dayIntensity;}}
 }
}
