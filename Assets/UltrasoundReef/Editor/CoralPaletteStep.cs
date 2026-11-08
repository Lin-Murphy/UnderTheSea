using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UltrasoundReef;
public static class CoralPaletteStep {
 const string D="Assets/UltrasoundReef/Environment/";
 static Color C(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
 static void Stem(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,float radius){
  Vector3 dir=(b-a).normalized,u=Vector3.Cross(dir,Vector3.forward).normalized;if(u.sqrMagnitude<.1f)u=Vector3.right;Vector3 w=Vector3.Cross(dir,u);int start=v.Count;
  // Six radial sides, four rings including a rounded shoulder and narrowed crown.
  for(int j=0;j<4;j++){
   float f=new[]{0f,.45f,.86f,1f}[j];float r=radius*new[]{1f,.95f,.72f,.26f}[j];
   Vector3 center=Vector3.Lerp(a,b,f)+u*Mathf.Sin(f*Mathf.PI)*radius*.25f;
   for(int k=0;k<6;k++){float angle=k*Mathf.PI/3;v.Add(center+(u*Mathf.Cos(angle)+w*Mathf.Sin(angle))*r);}
  }
  for(int j=0;j<3;j++)for(int k=0;k<6;k++){int x=start+j*6+k,y=start+j*6+(k+1)%6;t.AddRange(new[]{x,x+6,y,y,x+6,y+6});}
  int tip=v.Count;v.Add(b+dir*radius*.1f);for(int k=0;k<6;k++)t.AddRange(new[]{start+18+k,tip,start+18+(k+1)%6});
  int bottom=v.Count;v.Add(a);for(int k=0;k<6;k++)t.AddRange(new[]{start+k,start+(k+1)%6,bottom});
 }
 static Mesh Coral(int type){
  var v=new List<Vector3>();var t=new List<int>();
  float height=type==4?2.15f:type==3?.9f:1.5f;
  Stem(v,t,Vector3.zero,new Vector3(.06f,height*.7f,0),type==2?.28f:.21f);
  int branches=type==1?8:type==3?6:7;
  for(int i=0;i<branches;i++){
   float side=i%2==0?-1:1,h=(i/(float)branches)*.6f+.25f;
   Vector3 a=new Vector3(.04f,height*h*.7f,0);
   float spread=type==1?.95f:type==2?.38f:.66f;
   Vector3 b=a+new Vector3(side*spread*(.6f+i%3*.2f),height*(.35f+i%2*.12f),type==1?0:Mathf.Sin(i*2.4f)*.3f);
   Stem(v,t,a,b,type==2?.18f:.135f);
  }
  var m=new Mesh();m.name=new[]{"Staghorn rounded","Fan coral","Fat fingers","Small clump","Tall branching"}[type];m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
 }
 static Mesh Save(Mesh mesh,string path){var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);return old;}AssetDatabase.CreateAsset(mesh,path);return mesh;}
 static void ColorMat(string name,string hex){var mat=AssetDatabase.LoadAssetAtPath<Material>(D+name+".mat");if(!mat)return;mat.color=C(hex);EditorUtility.SetDirty(mat);}
 static void Cover(Transform plant){
  if(!plant)return;plant.localScale=Vector3.one*.92f;
  var source=plant.GetComponent<MeshFilter>();var renderer=plant.GetComponent<MeshRenderer>();
  for(int i=0;i<3;i++){
   string name="Cover fronds "+i;var child=plant.Find(name);
   if(!child){child=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)).transform;child.SetParent(plant,false);}
   child.localPosition=new Vector3((i-1)*.28f,.05f,(i%2)*.22f);child.localRotation=Quaternion.Euler(0,45+i*65,0);
   child.GetComponent<MeshFilter>().sharedMesh=source.sharedMesh;child.GetComponent<MeshRenderer>().sharedMaterial=renderer.sharedMaterial;
  }
 }
 [MenuItem("Ultrasound Reef/Step 1 - Rounded Coral and Palette")]
 public static void Apply(){
  if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play before applying art changes.");return;}
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  string original=EditorSceneManager.GetActiveScene().path;
  var meshes=new Mesh[5];for(int i=0;i<5;i++){meshes[i]=Save(Coral(i),D+"RoundedCoral_"+i+".asset");Debug.Log(meshes[i].name+": "+meshes[i].triangles.Length/3+" triangles");}
  string[] names={"Peach coral","Golden coral","Lavender coral"};string[] colors={"#F6B58F","#FBE3A8","#E88F8A"};
  var mats=new Material[3];for(int i=0;i<3;i++){
   mats[i]=AssetDatabase.LoadAssetAtPath<Material>(D+names[i]+".mat");mats[i].shader=Shader.Find("UltrasoundReef/CoralSunlight");mats[i].SetColor("_BaseColor",C(colors[i]));mats[i].SetColor("_TipColor",C("#FBE3A8"));mats[i].SetFloat("_Glow",.12f);mats[i].SetFloat("_GlowIntensity",.02f);mats[i].SetFloat("_Height",2);mats[i].enableInstancing=true;EditorUtility.SetDirty(mats[i]);
  }
  ColorMat("Sand turquoise","#E9DDBA");ColorMat("Rock ocean blue","#3F8F9A");ColorMat("Wide kelp jade","#8FA77E");ColorMat("Seaweed jade","#8FA77E");
  foreach(string path in new[]{"Assets/Scenes/UnderwaterGarden.unity","Assets/Scenes/UnderwaterGelPreview.unity"}){
   if(!System.IO.File.Exists(path))continue;var scene=EditorSceneManager.OpenScene(path);int index=0;
   foreach(var f in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None)){
    bool hero=f.name=="Patient coral"||f.name.StartsWith("Mirror coral");
    if(!hero&&!f.name.StartsWith("Branching coral"))continue;
    int type=hero?4:index%5;f.sharedMesh=meshes[type];f.GetComponent<Renderer>().sharedMaterial=mats[hero?0:index%3];index++;
   }
   var flow=Object.FindAnyObjectByType<GelSeaweedPreview>();if(flow){Cover(flow.seaweed);Cover(flow.mirrorSeaweed);flow.mirrorSeaweed.localScale=Vector3.one*.53f;flow.seaweed.position=new Vector3(0,-.7f,.55f);flow.mirrorSeaweed.position=new Vector3(0,1.9f,5.75f);}
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   var root=GameObject.Find("UNDERWATER GARDEN - editable environment");if(root&&path.Contains("UnderwaterGarden"))PrefabUtility.SaveAsPrefabAsset(root,D+"UnderwaterGarden.prefab");
  }
  AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
  Debug.Log("Step 1 complete: five rounded coral variants, pastel palette, initial kelp coverage. Gel reveals coral; public controls retained.");
 }
}
