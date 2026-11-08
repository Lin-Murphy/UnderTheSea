using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UltrasoundReef;
public static class ReefAtmosphereStep {
 const string D="Assets/UltrasoundReef/Environment/";
 static Color Hex(string s){ColorUtility.TryParseHtmlString(s,out var c);return c;}
 static Mesh Save(Mesh m,string name){m.name=name;var old=AssetDatabase.LoadAssetAtPath<Mesh>(D+name+".asset");if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);return old;}AssetDatabase.CreateAsset(m,D+name+".asset");return m;}
 static Mesh FacetedRock(int seed){
  var rng=new System.Random(seed);var verts=new List<Vector3>();var tris=new List<int>();
  for(int j=0;j<4;j++)for(int k=0;k<10;k++){
   float a=k*Mathf.PI/5;float radius=new[]{.69f,1f,.9f,.58f}[j]*(.94f+(float)rng.NextDouble()*.12f)*.5f;
   verts.Add(new Vector3(Mathf.Cos(a)*radius,new[]{-.5f,-.27f,.27f,.5f}[j],Mathf.Sin(a)*radius));
  }
  for(int j=0;j<3;j++)for(int k=0;k<10;k++){int a=j*10+k,b=j*10+(k+1)%10;tris.AddRange(new[]{a,a+10,b,b,a+10,b+10});}
  verts.Add(new Vector3(0,.5f,0));verts.Add(new Vector3(0,-.5f,0));
  for(int k=0;k<10;k++)tris.AddRange(new[]{30+k,40,30+(k+1)%10,k,(k+1)%10,41});
  var mesh=new Mesh();mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }
 static Mesh Arch(){
  var v=new List<Vector3>();var t=new List<int>();int count=16;
  for(int j=0;j<=count;j++){
   float a=j/(float)count*Mathf.PI;
   float irregular=1+Mathf.Sin(j*2.3f)*.035f;
   v.Add(new Vector3(Mathf.Cos(a)*4*irregular,Mathf.Sin(a)*5.4f*irregular,-.9f));
   v.Add(new Vector3(Mathf.Cos(a)*2.7f,Mathf.Sin(a)*3.7f,-.7f));
   v.Add(new Vector3(Mathf.Cos(a)*4*irregular,Mathf.Sin(a)*5.4f*irregular,.9f));
   v.Add(new Vector3(Mathf.Cos(a)*2.7f,Mathf.Sin(a)*3.7f,.7f));
   if(j<count){int x=j*4; t.AddRange(new[]{x,x+4,x+1,x+1,x+4,x+5,x+2,x+3,x+6,x+3,x+7,x+6,x,x+2,x+4,x+2,x+6,x+4,x+1,x+5,x+3,x+3,x+5,x+7});}
  }
  t.AddRange(new[]{0,1,2,1,3,2,count*4,count*4+2,count*4+1,count*4+1,count*4+2,count*4+3});
  var m=new Mesh();m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
 }
 static Mesh Shaft(){var m=new Mesh();m.vertices=new[]{new Vector3(-.6f,10,0),new Vector3(.6f,10,0),new Vector3(-2.3f,-1,0),new Vector3(2.3f,-1,0)};m.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)};m.triangles=new[]{0,2,1,1,2,3};m.RecalculateNormals();m.RecalculateBounds();return m;}
 static Texture2D Caustics(){
  const int n=256;var tex=new Texture2D(n,n,TextureFormat.RGBA32,true,true);var sites=new Vector2[16];var rng=new System.Random(7);
  for(int i=0;i<16;i++)sites[i]=new Vector2((i%4+(float)rng.NextDouble()*.65f)/4,(i/4+(float)rng.NextDouble()*.65f)/4);
  var pixels=new Color[n*n];for(int y=0;y<n;y++)for(int x=0;x<n;x++){
   var p=new Vector2(x/(float)n,y/(float)n);float first=10,second=10;
   foreach(var site in sites){Vector2 d=p-site;d.x-=Mathf.Round(d.x);d.y-=Mathf.Round(d.y);float dist=d.magnitude;if(dist<first){second=first;first=dist;}else if(dist<second)second=dist;}
   float c=Mathf.Exp(-(second-first)*95);pixels[y*n+x]=new Color(c,c,c,1);
  }
  tex.SetPixels(pixels);tex.wrapMode=TextureWrapMode.Repeat;tex.filterMode=FilterMode.Bilinear;tex.Apply();return tex;
 }
 static void Profile<T>(VolumeProfile p,System.Action<T> configure)where T:VolumeComponent{
  if(!p.TryGet<T>(out var item)){item=p.Add<T>();AssetDatabase.AddObjectToAsset(item,p);}configure(item);EditorUtility.SetDirty(item);
 }
 [MenuItem("Ultrasound Reef/Step 2 - Rocks Sand and Atmosphere")]
 public static void Apply(){
  if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play before applying step 2.");return;}
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;string original=EditorSceneManager.GetActiveScene().path;
  var surface=Shader.Find("UltrasoundReef/SoftReefSurface");var rayShader=Shader.Find("UltrasoundReef/SoftLightShaft");
  if(!surface||!rayShader){Debug.LogError("Step 2 shaders are not ready.");return;}
  var generated=Caustics();System.IO.File.WriteAllBytes(D+"SoftCaustics.png",generated.EncodeToPNG());Object.DestroyImmediate(generated);AssetDatabase.ImportAsset(D+"SoftCaustics.png");
  var importer=(TextureImporter)AssetImporter.GetAtPath(D+"SoftCaustics.png");importer.sRGBTexture=false;importer.wrapMode=TextureWrapMode.Repeat;importer.mipmapEnabled=true;importer.maxTextureSize=256;importer.SaveAndReimport();
  var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(D+"SoftCaustics.png");
  var rockmat=AssetDatabase.LoadAssetAtPath<Material>(D+"Rock ocean blue.mat");rockmat.shader=surface;rockmat.SetColor("_BaseColor",Hex("#3F8F9A"));rockmat.SetFloat("_TopLight",.14f);rockmat.SetTexture("_Caustics",texture);rockmat.SetFloat("_CausticStrength",.075f);rockmat.enableInstancing=true;EditorUtility.SetDirty(rockmat);
  var sand=AssetDatabase.LoadAssetAtPath<Material>(D+"Sand turquoise.mat");sand.shader=surface;sand.SetColor("_BaseColor",Hex("#E9DDBA"));sand.SetFloat("_TopLight",.02f);sand.SetTexture("_Caustics",texture);sand.SetFloat("_CausticStrength",.18f);EditorUtility.SetDirty(sand);
  var rays=AssetDatabase.LoadAssetAtPath<Material>(D+"Soft sunlight shafts.mat");if(!rays){rays=new Material(rayShader);AssetDatabase.CreateAsset(rays,D+"Soft sunlight shafts.mat");}rays.SetColor("_BaseColor",new Color(.749f,.937f,.941f,.055f));rays.enableInstancing=true;EditorUtility.SetDirty(rays);
  Mesh[] rocks={Save(FacetedRock(1),"SoftRock_A"),Save(FacetedRock(8),"SoftRock_B"),Save(FacetedRock(19),"SoftRock_C")};var arch=Save(Arch(),"NaturalReefArch");var shaft=Save(Shaft(),"SoftShaftPlane");
  foreach(string path in new[]{"Assets/Scenes/UnderwaterGarden.unity","Assets/Scenes/UnderwaterGelPreview.unity"}){
   if(!System.IO.File.Exists(path))continue;var scene=EditorSceneManager.OpenScene(path);var terrain=GameObject.Find("Terrain and layered rocks");int index=0;
   foreach(Transform r in terrain.transform){
    if(r.name=="Distant arch"){r.gameObject.SetActive(false);continue;}
    if(r.name=="Reef rock terrace"||r.name=="Mirror coral supporting rock"){r.GetComponent<MeshFilter>().sharedMesh=rocks[index++%3];r.GetComponent<Renderer>().sharedMaterial=rockmat;GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic);}
   }
   var a=GameObject.Find("Natural reef arch");if(!a){a=new GameObject("Natural reef arch",typeof(MeshFilter),typeof(MeshRenderer));a.transform.SetParent(terrain.transform);}a.transform.position=new Vector3(6.5f,-.4f,17);a.GetComponent<MeshFilter>().sharedMesh=arch;a.GetComponent<Renderer>().sharedMaterial=rockmat;GameObjectUtility.SetStaticEditorFlags(a,StaticEditorFlags.BatchingStatic);
   // Exact editor-only mesh raycasts re-seat vegetation after rock replacement.
   var colliders=new List<MeshCollider>();
   foreach(var f in terrain.GetComponentsInChildren<MeshFilter>()){var c=f.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;colliders.Add(c);}
   Physics.SyncTransforms();
   foreach(var f in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None)){
    if(!(f.name.StartsWith("Branching coral")||f.name.StartsWith("Ribbon grass")||f.name=="Patient coral"||f.name.StartsWith("Mirror coral -")))continue;
    Vector3 p=f.transform.position;float h=-100;
    foreach(var c in colliders)if(c.Raycast(new Ray(new Vector3(p.x,30,p.z),Vector3.down),out var hit,60))h=Mathf.Max(h,hit.point.y);
    if(h>-99){p.y=h-.015f;f.transform.position=p;}
   }
   foreach(var c in colliders)Object.DestroyImmediate(c);
   var old=GameObject.Find("Soft water light shafts");if(old)Object.DestroyImmediate(old);
   var group=new GameObject("Soft water light shafts");
   for(int i=0;i<5;i++){var o=new GameObject("Light shaft "+i,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(group.transform);o.transform.position=new Vector3(-5+i*2.6f,0,7+i*2);o.transform.rotation=Quaternion.Euler(0,i%2==0?15:-18,-14);o.GetComponent<MeshFilter>().sharedMesh=shaft;var renderer=o.GetComponent<Renderer>();renderer.sharedMaterial=rays;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
   RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=Hex("#2BA6C8");RenderSettings.fogDensity=.025f;
   RenderSettings.ambientSkyColor=Hex("#BFEFF0")*.7f;RenderSettings.ambientEquatorColor=Hex("#2BA6C8")*.6f;
   foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)){if(l.type==LightType.Directional){l.intensity=1.45f;l.shadows=LightShadows.None;}}
   var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(D+"UnderwaterVolume.asset");
   Profile<Bloom>(profile,b=>{b.intensity.Override(.1f);b.threshold.Override(1.5f);b.scatter.Override(.5f);});
   Profile<ColorAdjustments>(profile,c=>{c.postExposure.Override(-.15f);c.saturation.Override(2);c.contrast.Override(3);});EditorUtility.SetDirty(profile);
   var root=GameObject.Find("UNDERWATER GARDEN - editable environment");if(root&&path.Contains("UnderwaterGarden"))PrefabUtility.SaveAsPrefabAsset(root,D+"UnderwaterGarden.prefab");
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
  AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);Debug.Log("Step 2 saved: bevelled rocks, continuous arch, projected texture caustics, exp-squared fog, five mesh rays. Vegetation reseated on meshes. Quest profiling pending.");
 }
}
