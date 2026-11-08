using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
namespace UltrasoundReef {
 public class ProbeExamFlow : MonoBehaviour {
  public enum Stage{Idle,Gel,Probe}
  public Stage stage;
  public GelSeaweedPreview gel;
  public ReefDesktopLook cameraLook;
  public Transform probeTarget;
  public Transform externalProbeTarget;
  public bool externalTrackingValid;
  public Mesh fishMesh;
  public Material fishMaterial;
  public ParticleSystem motes;
  public bool paused;
  public JellyfishArrival jellyfishArrival;
  public int backgroundFishCount=32,contactFishCount=12;
  public float probeMoveSpeed=.35f;
  Material liveFishMaterial;Matrix4x4[] matrices;Vector3[] positions;Quaternion[] rotations;
  Vector3[] homes;Renderer patient,mirror;float clock,approach,standOff=.12f;
  Vector3 contact;int activeCount;Renderer probeMarker;
  static readonly int MotionTime=Shader.PropertyToID("_ReefMotionTime"),MotionActive=Shader.PropertyToID("_ReefMotionActive");
  void Awake(){
   patient=gel.contactCoral.GetComponent<Renderer>();mirror=gel.mirrorCoral.GetComponent<Renderer>();
   liveFishMaterial=new Material(fishMaterial){enableInstancing=true};
   int count=backgroundFishCount+contactFishCount*2;matrices=new Matrix4x4[count];positions=new Vector3[count];rotations=new Quaternion[count];homes=new Vector3[count];
   for(int i=0;i<count;i++){float a=i*2.39996f;homes[i]=new Vector3(Mathf.Sin(a)*3.5f,2.7f+(i%5)*.28f,7+(i%7)*.4f);positions[i]=homes[i];rotations[i]=Quaternion.identity;}
   // Approach from the front of each coral, never through its body from behind.
   for(int i=backgroundFishCount;i<count;i++){
    bool mirrored=i>=backgroundFishCount+contactFishCount;var b=mirrored?mirror.bounds:patient.bounds;int id=(i-backgroundFishCount)%contactFishCount;
    homes[i]=new Vector3(b.center.x-1.8f-id%3*.12f,b.center.y+.45f+(id%4)*.08f,b.min.z-.9f-id%3*.1f);positions[i]=homes[i];
   }
   probeMarker=probeTarget.GetComponentInChildren<Renderer>();
   gel.handleInput=false;gel.showUI=false;SetStage(Stage.Idle);
  }
  void OnEnable(){Shader.SetGlobalFloat(MotionActive,1);}
  void OnDisable(){Shader.SetGlobalFloat(MotionActive,0);}
  void OnDestroy(){if(liveFishMaterial)Destroy(liveFishMaterial);}
  public void SetStage(Stage next){
   if(jellyfishArrival)jellyfishArrival.ResetToDay();
   stage=next;paused=false;approach=0;
   if(next==Stage.Idle)gel.SetGel(false);else if(next==Stage.Gel)gel.SetGel(true);else if(!gel.gelActive)gel.SetGel(true);
   gel.paused=false;standOff=.12f;
   if(probeMarker)probeMarker.enabled=next==Stage.Probe && !externalProbeTarget;
   if(next==Stage.Probe){var b=patient.bounds;probeTarget.position=new Vector3(b.center.x,b.center.y,b.min.z-standOff);for(int i=backgroundFishCount;i<positions.Length;i++)positions[i]=homes[i];}
   if(cameraLook)cameraLook.SetStagePitch(next==Stage.Probe?14:0);
   if(motes && !motes.isPlaying)motes.Play();
   Debug.Log("Ultrasound preview stage: "+next+". Probe input is simulated unless a valid external target is supplied.");
  }
  public void Next(){if(paused)return;if(stage==Stage.Idle)SetStage(Stage.Gel);else if(stage==Stage.Gel)SetStage(Stage.Probe);else if(jellyfishArrival)jellyfishArrival.Begin();}
  void Update(){
   var k=Keyboard.current;
   if(k!=null){
    if(k.spaceKey.wasPressedThisFrame)Next();
    if(k.digit1Key.wasPressedThisFrame||k.escapeKey.wasPressedThisFrame)SetStage(Stage.Idle);
    if(k.digit2Key.wasPressedThisFrame)SetStage(Stage.Gel);
    if(k.digit3Key.wasPressedThisFrame)SetStage(Stage.Probe);
    if(k.jKey.wasPressedThisFrame&&stage==Stage.Probe&&jellyfishArrival)jellyfishArrival.Begin();
    if(k.rKey.wasPressedThisFrame){if(jellyfishArrival&&jellyfishArrival.IsActive)jellyfishArrival.Replay();else SetStage(stage);}
    if(k.pKey.wasPressedThisFrame){paused=!paused;gel.paused=paused;if(motes){if(paused)motes.Pause();else motes.Play();}}
   }
   if(!paused){
    clock+=Time.deltaTime;
    if(stage==Stage.Probe && !externalProbeTarget && k!=null){
     float x=(k.rightArrowKey.isPressed||k.dKey.isPressed?1:0)-(k.leftArrowKey.isPressed||k.aKey.isPressed?1:0);
     float y=(k.upArrowKey.isPressed||k.wKey.isPressed?1:0)-(k.downArrowKey.isPressed||k.sKey.isPressed?1:0);
     standOff=Mathf.Clamp(standOff+((k.qKey.isPressed?1:0)-(k.eKey.isPressed?1:0))*Time.deltaTime*.15f,.08f,.35f);
     var b=patient.bounds;Vector3 p=probeTarget.position+new Vector3(x,y,0)*probeMoveSpeed*Time.deltaTime;
     p.x=Mathf.Clamp(p.x,b.center.x-b.extents.x*.65f,b.center.x+b.extents.x*.65f);p.y=Mathf.Clamp(p.y,b.min.y+b.size.y*.25f,b.max.y-b.size.y*.15f);p.z=b.min.z-standOff;probeTarget.position=p;
    }
    bool valid=stage==Stage.Probe && (!externalProbeTarget || externalTrackingValid);
    contact=externalProbeTarget&&externalTrackingValid?externalProbeTarget.position:probeTarget.position;
    var pb=patient.bounds;
    if(externalProbeTarget && (Mathf.Abs(contact.x-pb.center.x)>pb.extents.x+.5f || contact.y<pb.min.y || contact.y>pb.max.y+.3f))valid=false;
    contact.z=Mathf.Min(contact.z,pb.min.z-.08f);
    approach=Mathf.MoveTowards(approach,valid?1:0,Time.deltaTime*.45f);
    activeCount=backgroundFishCount+(stage==Stage.Probe?contactFishCount*2:0);
    for(int i=0;i<activeCount;i++){
     Vector3 goal;
     if(i<backgroundFishCount){float a=clock*.09f+i*.25f;goal=new Vector3(-3.5f+Mathf.Sin(a)*3.4f,3.6f+Mathf.Sin(a*.65f+i)*.5f,10+Mathf.Cos(a)*2);}
     else {
      int id=(i-backgroundFishCount)%contactFishCount;bool mirrored=i>=backgroundFishCount+contactFishCount;
      float a=id*2.39996f;Vector3 offset=new Vector3((id%4-1.5f)*.26f,(id/4-1)*.19f,-.15f-(id%2)*.16f);
      Vector3 dest=contact;
      if(mirrored){var mb=mirror.bounds;dest=new Vector3(mb.center.x+(contact.x-pb.center.x)*mb.size.x/pb.size.x,mb.center.y+(contact.y-pb.center.y)*mb.size.y/pb.size.y,mb.min.z-.09f);}
      goal=Vector3.Lerp(homes[i],dest+offset,Mathf.SmoothStep(0,1,approach));
     }
     Vector3 move=goal-positions[i];float step=1-Mathf.Exp(-Time.deltaTime*2.2f);positions[i]=Vector3.Lerp(positions[i],goal,step);
     if(move.sqrMagnitude>.00005f)rotations[i]=Quaternion.Slerp(rotations[i],Quaternion.LookRotation(move.normalized),step);
     matrices[i]=Matrix4x4.TRS(positions[i],rotations[i],Vector3.one*(i<backgroundFishCount?.65f:.38f));
    }
   }
   Shader.SetGlobalFloat(MotionTime,clock);Shader.SetGlobalFloat(MotionActive,1);
  }
  void LateUpdate(){
   if(!liveFishMaterial||!fishMesh||activeCount==0)return;
   var rp=new RenderParams(liveFishMaterial){worldBounds=new Bounds(new Vector3(0,4,10),new Vector3(40,20,40)),shadowCastingMode=ShadowCastingMode.Off,receiveShadows=false};
   Graphics.RenderMeshInstanced(rp,fishMesh,0,matrices,activeCount);
  }
  void OnGUI(){
   GUI.Box(new Rect(12,12,565,242),"");GUI.Label(new Rect(25,20,540,24),"ULTRASOUND | GEL > PROBE EXAMINATION");
   GUI.Label(new Rect(25,46,540,24),"Stage: "+stage+(jellyfishArrival&&jellyfishArrival.IsActive?(jellyfishArrival.IsComplete?" | jellyfish ready":" | transition: night + jellyfish"):"")+(paused?" [PAUSED]":""));
   GUI.Label(new Rect(25,71,540,24),"Space: idle > gel > probe > jellyfish transition");
   GUI.Label(new Rect(25,96,540,24),"Probe: arrows / WASD | Q: lift | E: closer to surface");
   GUI.Label(new Rect(25,121,540,24),"Mouse drag: look | Home: reset view | P: pause | R: replay");
   GUI.Label(new Rect(25,146,540,24),externalProbeTarget?(externalTrackingValid?"EXTERNAL WORLD TARGET (calibration not verified)":"EXTERNAL TRACKING INVALID: fish retreat"):"SIMULATED PROBE: keyboard target; no AprilTag detector");
   GUI.Label(new Rect(25,171,540,24),jellyfishArrival&&jellyfishArrival.IsActive?"[ / ]: preview Breath | R: replay arrival | 1: return to day":"1 / 2 / 3: select stage | J: arrival from probe stage");
   if(GUI.Button(new Rect(25,207,120,28),"Next / Space"))Next();
   if(GUI.Button(new Rect(157,207,130,28),"Start / replay gel"))SetStage(Stage.Gel);
   if(GUI.Button(new Rect(299,207,120,28),"Return idle"))SetStage(Stage.Idle);
  }
 }
}
