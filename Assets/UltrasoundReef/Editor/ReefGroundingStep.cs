using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UltrasoundReef;

public static class ReefGroundingStep {
 const string D="Assets/UltrasoundReef/Environment/";
 static float Sand(float x,float z)=>Mathf.PerlinNoise(x*.09f+10,z*.09f+10)*.65f-.7f;
 static float Ground(Vector3 p,Transform rocks){
  float y=Sand(p.x,p.z);
  if(rocks)foreach(Transform rock in rocks){
   if(rock.name!="Reef rock terrace")continue;
   Vector3 c=rock.position,s=rock.lossyScale*.5f;
   float r=Mathf.Pow((p.x-c.x)/s.x,2)+Mathf.Pow((p.z-c.z)/s.z,2);
   if(r<1)y=Mathf.Max(y,c.y+s.y*Mathf.Sqrt(1-r));
  }
  return y;
 }
 [MenuItem("Ultrasound Reef/Fix Grounding and Rooted Gel Reveal")]
 public static void Apply(){
  if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play before grounding the scene.");return;}
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  string original=EditorSceneManager.GetActiveScene().path;
  foreach(string path in new[]{"Assets/Scenes/UnderwaterGarden.unity","Assets/Scenes/UnderwaterGelPreview.unity"}){
   if(!System.IO.File.Exists(path))continue;var scene=EditorSceneManager.OpenScene(path);
   var terrain=GameObject.Find("Terrain and layered rocks");Transform rocks=terrain?terrain.transform:null;
   foreach(var f in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None)){
    if(f.name.StartsWith("Branching coral")||f.name.StartsWith("Ribbon grass")){
     Vector3 p=f.transform.position;p.y=Ground(p,rocks)-.02f;f.transform.position=p;
    }
   }
   var patient=GameObject.Find("Patient coral");var mirror=GameObject.Find("Mirror coral - reserved interaction");
   if(patient){var p=patient.transform.position;p.y=Sand(p.x,p.z)-.02f;patient.transform.position=p;}
   if(mirror){
    var support=GameObject.Find("Mirror coral supporting rock");
    if(!support){support=GameObject.CreatePrimitive(PrimitiveType.Sphere);support.name="Mirror coral supporting rock";Object.DestroyImmediate(support.GetComponent<Collider>());support.transform.SetParent(rocks);}
    support.transform.position=new Vector3(mirror.transform.position.x,.65f,mirror.transform.position.z);support.transform.localScale=new Vector3(2,2.7f,1.8f);
    support.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(D+"Rock ocean blue.mat");
    var p=mirror.transform.position;p.y=2-.02f;mirror.transform.position=p;
   }
   var flow=Object.FindAnyObjectByType<GelSeaweedPreview>();
   if(flow){
    flow.contactCoral=patient?patient.transform:null;flow.mirrorCoral=mirror?mirror.transform:null;flow.gelActive=false;flow.paused=false;
    Vector3 p=new Vector3(-.65f,0,.45f);p.y=Sand(p.x,p.z)-.02f;flow.seaweed.position=p;flow.seaweed.rotation=Quaternion.identity;
    // The mirror is a visual duplicate supported on its own rock, with rooted kelp.
    flow.mirrorSeaweed.position=new Vector3(-1.1f,Sand(-1.1f,5.1f)-.02f,5.1f);flow.mirrorSeaweed.localScale=Vector3.one*1.35f;flow.mirrorSeaweed.rotation=Quaternion.identity;
   }
   // Keep foreground decor away from the desktop camera's near view.
   if(Camera.main)foreach(var f in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None)){
    if(!f.name.StartsWith("Branching coral"))continue;
    Vector3 delta=f.transform.position-Camera.main.transform.position;
    if(new Vector2(delta.x,delta.z).magnitude<2.5f){Vector3 p=f.transform.position;p.x=p.x<0?-3.2f:3.2f;p.y=Ground(p,rocks)-.02f;f.transform.position=p;}
   }
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   var root=GameObject.Find("UNDERWATER GARDEN - editable environment");if(root&&path.Contains("UnderwaterGarden"))PrefabUtility.SaveAsPrefabAsset(root,D+"UnderwaterGarden.prefab");
  }
  AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
  Debug.Log("Grounding complete: coral and grass placed on sand/rock; gel bends leaves only, roots remain fixed. Coral bounds clearance enabled.");
 }
}
