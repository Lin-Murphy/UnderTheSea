using UnityEngine;
using UnityEngine.InputSystem;
namespace UltrasoundReef {
 public class GelSeaweedPreview : MonoBehaviour {
  public Transform seaweed,mirrorSeaweed;
  [Tooltip("Optional world-space target from a calibrated tracking adapter; no AprilTag detector is included.")]
  public Transform externalGelTarget;
  public Transform bodyContact;
  public Transform contactCoral,mirrorCoral;
  public bool gelActive,paused;
  public bool handleInput=true,showUI=true,useGpuDeformation;
  Renderer[] gpuRenderers; MaterialPropertyBlock gpuBlock;
  public float waveSpeed=.7f;
  float clock,blend;
  Vector3 home,mirrorHome;Mesh[] meshes;Vector3[][] originals;MeshFilter[] filters;
  void OnEnable(){if(Application.isPlaying && ((useGpuDeformation && (gpuRenderers==null || gpuBlock==null))||(!useGpuDeformation && meshes==null)) && seaweed && mirrorSeaweed)Awake();}
  void Awake(){
   home=seaweed.position;mirrorHome=mirrorSeaweed.position;
   filters=GetComponentsInChildren<MeshFilter>(true);
   gpuRenderers=GetComponentsInChildren<Renderer>(true);gpuBlock=new MaterialPropertyBlock();
   if(useGpuDeformation)return;
   meshes=new Mesh[filters.Length];originals=new Vector3[filters.Length][];
   for(int i=0;i<filters.Length;i++){meshes[i]=Instantiate(filters[i].sharedMesh);filters[i].sharedMesh=meshes[i];originals[i]=meshes[i].vertices;}
  }
  public void SetGel(bool active){gelActive=active;paused=false;clock=0; if(active)blend=0;}
  void Update(){
   var k=Keyboard.current;if(handleInput && k!=null){
    if(k.digit1Key.wasPressedThisFrame)SetGel(false);
    if(k.digit2Key.wasPressedThisFrame)SetGel(true);
    if(k.spaceKey.wasPressedThisFrame)SetGel(!gelActive);
    if(k.pKey.wasPressedThisFrame)paused=!paused;
    if(k.rKey.wasPressedThisFrame)SetGel(true);
    if(k.escapeKey.wasPressedThisFrame)SetGel(false);
   }
   if(paused)return;clock+=Time.deltaTime;
   blend=Mathf.MoveTowards(blend,gelActive?1:0,Time.deltaTime*.4f);
   // Idle = coverage. Only the operator gel event releases the fronds.
   float reveal=Mathf.SmoothStep(0,1,blend);
   // Roots stay fixed in world space. Only leaf vertices part during gel.
   seaweed.position=home;mirrorSeaweed.position=mirrorHome;
   if(useGpuDeformation){
    if(gpuBlock==null)gpuBlock=new MaterialPropertyBlock();
    if(gpuRenderers==null)gpuRenderers=GetComponentsInChildren<Renderer>(true);
    foreach(var renderer in gpuRenderers){
     bool mirrored=renderer.transform==mirrorSeaweed || renderer.transform.IsChildOf(mirrorSeaweed);
     Transform coral=mirrored?mirrorCoral:contactCoral;Renderer cr=coral?coral.GetComponent<Renderer>():null;
     renderer.GetPropertyBlock(gpuBlock);gpuBlock.SetFloat("_Interactive",1);gpuBlock.SetFloat("_Reveal",reveal);gpuBlock.SetFloat("_MotionTime",clock);
     gpuBlock.SetFloat("_Bend",externalGelTarget?Mathf.Clamp(externalGelTarget.position.x-home.x,-1.2f,1.2f):-2.2f);
     if(cr){Bounds b=cr.bounds;b.Expand(.06f);gpuBlock.SetVector("_ClearCenter",b.center);gpuBlock.SetVector("_ClearSize",b.size);}
     renderer.SetPropertyBlock(gpuBlock);
    }
    return;
   }
   for(int i=0;i<meshes.Length;i++){
    var source=originals[i];var v=new Vector3[source.Length];
    bool mirrored=filters[i].transform==mirrorSeaweed || filters[i].transform.IsChildOf(mirrorSeaweed);
    Transform coral=mirrored?mirrorCoral:contactCoral;
    Renderer coralRenderer=coral?coral.GetComponent<Renderer>():null;
    for(int j=0;j<v.Length;j++){
     float h=Mathf.Clamp01(source[j].y/2.4f);
     float bend=externalGelTarget?Mathf.Clamp(externalGelTarget.position.x-home.x,-1.2f,1.2f):-2.2f;
     Vector3 bentWorld=filters[i].transform.TransformPoint(source[j])+new Vector3(h*h*(.65f*(1-reveal)+bend*reveal+Mathf.Sin(clock*waveSpeed+h*3)*.12f*reveal),-h*h*.65f*reveal,-h*h*.14f);
     v[j]=filters[i].transform.InverseTransformPoint(bentWorld);
     if(coralRenderer && h>.08f){
      Vector3 world=filters[i].transform.TransformPoint(v[j]);Bounds bounds=coralRenderer.bounds;bounds.Expand(.06f);
      // Conservative clearance: leaves cover in front, never pass through the coral volume.
      if(bounds.Contains(world)){world.z=bounds.min.z-.035f;v[j]=filters[i].transform.InverseTransformPoint(world);}
     }
    }
    meshes[i].vertices=v;meshes[i].RecalculateNormals();meshes[i].RecalculateBounds();
   }
  }
  void OnDestroy(){if(meshes!=null)foreach(var m in meshes)if(m)Destroy(m);}
  void OnGUI(){
   if(!showUI)return;
   GUI.Box(new Rect(12,12,520,184),"");GUI.Label(new Rect(25,22,490,24),"UNDERWATER GARDEN | GEL INTERACTION PREVIEW");
   GUI.Label(new Rect(25,48,490,24),gelActive?"2. GEL: waves part the kelp and reveal coral":"1. IDLE: kelp covers the patient coral");
   GUI.Label(new Rect(25,74,490,24),"Drag LMB / RMB: look around | Home: reset view");
   GUI.Label(new Rect(25,98,490,24),"1: idle | 2: gel | Space: toggle | P: pause | R: replay");
   GUI.Label(new Rect(25,122,490,24),externalGelTarget?"External target assigned (tracking validity not verified)":"SIMULATION: preset contact path; no AprilTag input");
   if(GUI.Button(new Rect(25,153,145,28),"Start / replay gel"))SetGel(true);
   if(GUI.Button(new Rect(183,153,110,28),"Return idle"))SetGel(false);
   if(paused)GUI.Label(new Rect(310,154,150,28),"PAUSED");
  }
 }
}
