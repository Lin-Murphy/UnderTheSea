using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using UltrasoundReef;
public static class JellyfishSetup {
 const string D="Assets/UltrasoundReef/Environment/";
 static Mesh Store(Mesh m,string name){m.name=name;var old=AssetDatabase.LoadAssetAtPath<Mesh>(D+name+".asset");if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);return old;}AssetDatabase.CreateAsset(m,D+name+".asset");return m;}
 static Mesh Bell(){
  var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();int segments=40,rings=12;
  for(int j=0;j<=rings;j++)for(int k=0;k<=segments;k++){
   float h=j/(float)rings,a=k/(float)segments*Mathf.PI*2,theta=h*Mathf.PI*.5f;
   float rim=Mathf.Pow(h,6),r=Mathf.Sin(theta)*(.58f+.018f*Mathf.Cos(a*8)*rim);
   v.Add(new Vector3(r*Mathf.Cos(a),Mathf.Cos(theta)*.35f-.022f*(1-Mathf.Cos(a*8))*rim,r*Mathf.Sin(a)));uv.Add(new Vector2(k/(float)segments,h));
   if(j<rings&&k<segments){int x=j*(segments+1)+k;t.AddRange(new[]{x,x+1,x+segments+1,x+1,x+segments+2,x+segments+1});}
  }
  var m=new Mesh();m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.bounds=new Bounds(new Vector3(0,.15f,0),new Vector3(1.6f,.8f,1.6f));return m;
 }
 static Mesh Arms(){
  var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
  for(int arm=0;arm<4;arm++){
   int start=v.Count;float a=arm*Mathf.PI*.5f;
   for(int j=0;j<=28;j++){
    float h=j/28f;float twist=a+h*3.5f;
    Vector3 center=new Vector3(Mathf.Cos(a)*.23f+Mathf.Sin(h*4.4f+a)*h*.21f,-.03f-h*(1.1f+arm%2*.17f),Mathf.Sin(a)*.23f+Mathf.Cos(h*4+a)*h*.12f);
    // Wide, twisting oral arms taper into a curled tip.
    float width=(.055f+Mathf.Sin(h*Mathf.PI)*.025f)*(1-h*.8f);Vector3 across=new Vector3(Mathf.Cos(twist),.12f*Mathf.Sin(h*3),Mathf.Sin(twist));
    v.Add(center-across*width);v.Add(center+across*width);uv.Add(new Vector2(arm,h));uv.Add(new Vector2(arm,h));
    if(j<28){int x=start+j*2;t.AddRange(new[]{x,x+1,x+2,x+1,x+3,x+2});}
   }
  }
  var m=new Mesh();m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.bounds=new Bounds(new Vector3(0,-.65f,0),new Vector3(1.4f,1.9f,1.4f));return m;
 }
 static Mesh Tentacles(){
  var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
  for(int tentacle=0;tentacle<10;tentacle++){
   int start=v.Count;float a=tentacle*Mathf.PI*.2f;
   for(int j=0;j<=22;j++)for(int k=0;k<4;k++){
    float h=j/22f,angle=k*Mathf.PI*.5f;float radius=Mathf.Lerp(.004f,.0015f,h);
    var center=new Vector3(Mathf.Cos(a)*(.48f+h*.12f)+Mathf.Sin(h*5+a)*h*.06f,-.025f-h*(1.35f+tentacle%3*.15f),Mathf.Sin(a)*(.48f+h*.12f));
    v.Add(center+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));uv.Add(new Vector2(tentacle,h));
    if(j<22){int x=start+j*4+k,y=start+j*4+(k+1)%4;t.AddRange(new[]{x,y,x+4,y,y+4,x+4});}
   }
  }
  var m=new Mesh();m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.bounds=new Bounds(new Vector3(0,-.8f,0),new Vector3(1.8f,2.2f,1.8f));return m;
 }
 static Mesh Flower(){
  var parts=new List<CombineInstance>();var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);var source=sphere.GetComponent<MeshFilter>().sharedMesh;
  for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;var p=new Vector3(Mathf.Cos(a)*.11f,0,Mathf.Sin(a)*.11f);parts.Add(new CombineInstance{mesh=source,transform=Matrix4x4.TRS(new Vector3(0,.14f,0),Quaternion.Euler(-55,0,0),Vector3.one)*Matrix4x4.TRS(p,Quaternion.Euler(0,-a*Mathf.Rad2Deg,0),new Vector3(.22f,.045f,.13f))});}
  var m=new Mesh();m.CombineMeshes(parts.ToArray());Object.DestroyImmediate(sphere);return m;
 }
 static Material Mat(string name,int part,Color color){
  var m=AssetDatabase.LoadAssetAtPath<Material>(D+name+".mat");if(!m){m=new Material(Shader.Find("UltrasoundReef/MoonJellyfish"));AssetDatabase.CreateAsset(m,D+name+".mat");}
  m.shader=Shader.Find("UltrasoundReef/MoonJellyfish");m.SetColor("_BaseColor",color);m.SetFloat("_Part",part);m.SetFloat("_Visibility",1);EditorUtility.SetDirty(m);return m;
 }
 [MenuItem("Ultrasound Reef/Add Jellyfish Arrival to Exam Preview")]
 public static void Build(){
  if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play before adding the jellyfish.");return;}
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  string path="Assets/Scenes/UnderwaterExamPreview.unity";
  System.IO.Directory.CreateDirectory("Documentation/Backups");string backup="Documentation/Backups/UnderwaterExamPreview_before_jellyfish.unity";
  if(!System.IO.File.Exists(backup))System.IO.File.Copy(path,backup);
  var scene=EditorSceneManager.OpenScene(path);var flow=Object.FindAnyObjectByType<ProbeExamFlow>();if(!flow){Debug.LogError("Examination flow missing.");return;}
  var oldHero=GameObject.Find("Companion jellyfish - art proxy");if(oldHero)oldHero.SetActive(false);
  var old=flow.jellyfishArrival;if(old&&old.jellyfishRoot)Object.DestroyImmediate(old.jellyfishRoot.gameObject);
  var arrival=old?old:flow.gameObject.AddComponent<JellyfishArrival>();arrival.flow=flow;flow.jellyfishArrival=arrival;
  var hero=new GameObject("Moon jellyfish - translucent companion");hero.transform.position=Camera.main.transform.position+new Vector3(1,.15f,2.8f);hero.transform.localScale=Vector3.one*.72f;
  var breath=hero.AddComponent<JellyfishBreath>();arrival.jellyfishRoot=hero.transform;arrival.breath=breath;
  Color blue=new Color(.866f,.953f,.969f),pink=new Color(.949f,.722f,.878f);
  Mesh[] meshes={Store(Bell(),"MoonJellyBell"),Store(Arms(),"MoonJellyOralArms"),Store(Tentacles(),"MoonJellyTentacles"),Store(Flower(),"MoonJellyFourPetals")};
  string[] names={"Jelly translucent bell","Jelly soft ribbons","Jelly fine tentacles","Jelly pink four petals"};
  for(int i=0;i<4;i++){
   var o=new GameObject(names[i],typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(hero.transform,false);o.GetComponent<MeshFilter>().sharedMesh=meshes[i];var r=o.GetComponent<MeshRenderer>();r.sharedMaterial=Mat(names[i],i,i==3?pink:blue);r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
  }
  PrefabUtility.SaveAsPrefabAsset(hero,D+"MoonJellyfish.prefab");hero.SetActive(false);
  Shader.SetGlobalFloat("_ReefNightBlend",0);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  Debug.Log("Moon jellyfish added. Probe stage + Space starts a night/arrival transition, without adding a numbered main stage. [ and ] preview Breath. No breathing instructions or sensors.");
 }
}
