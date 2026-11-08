using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using UltrasoundReef;
public static class ProbeExamSetup {
 const string D="Assets/UltrasoundReef/Environment/";
 static Mesh Fish(){
  var v=new List<Vector3>();var t=new List<int>();var colors=new List<Color>();Color body=new Color(.72f,.86f,.91f),fin=new Color(.98f,.82f,.48f);
  float[] z={-.22f,-.1f,.13f,.26f};float[] r={.035f,.11f,.105f,.015f};
  for(int j=0;j<4;j++)for(int k=0;k<6;k++){float a=k*Mathf.PI/3;v.Add(new Vector3(Mathf.Cos(a)*r[j]*.65f,Mathf.Sin(a)*r[j],z[j]));colors.Add(body);}
  for(int j=0;j<3;j++)for(int k=0;k<6;k++){int a=j*6+k,b=j*6+(k+1)%6;t.AddRange(new[]{a,b,a+6,b,b+6,a+6});}
  v.Add(new Vector3(0,0,z[0]));colors.Add(body);v.Add(new Vector3(0,0,z[3]));colors.Add(body);
  for(int k=0;k<6;k++)t.AddRange(new[]{k,24,(k+1)%6,18+k,18+(k+1)%6,25});
  System.Action<Vector3,Vector3,Vector3> triangle=(a,b,c)=>{int n=v.Count;v.AddRange(new[]{a,b,c});colors.AddRange(new[]{fin,fin,fin});t.AddRange(new[]{n,n+1,n+2,n+2,n+1,n});};
  triangle(new Vector3(0,0,-.19f),new Vector3(0,.13f,-.4f),new Vector3(0,-.13f,-.4f));
  triangle(new Vector3(0,.07f,-.1f),new Vector3(0,.19f,-.1f),new Vector3(0,.09f,.1f));
  triangle(new Vector3(-.045f,0,.06f),new Vector3(-.15f,-.035f,-.07f),new Vector3(-.04f,0,-.1f));
  triangle(new Vector3(.045f,0,.06f),new Vector3(.15f,-.035f,-.07f),new Vector3(.04f,0,-.1f));
  var m=new Mesh{name="Pastel reef fish"};m.SetVertices(v);m.SetColors(colors);m.SetTriangles(t,0);m.RecalculateNormals();m.bounds=new Bounds(Vector3.zero,new Vector3(.5f,.45f,.85f));return m;
 }
 static Mesh KelpForGpu(){
  var source=AssetDatabase.LoadAssetAtPath<Mesh>(D+"WideKelp.asset");
  var mesh=new Mesh{name="WideKelpGPU"};var vertices=new List<Vector3>();var triangles=new List<int>();var src=source.vertices;
  // Original kelp stores a front and back copy per blade. Cull Off renders both sides without duplicate geometry.
  if(src.Length==700){
   for(int leaf=0;leaf<7;leaf++)for(int j=0;j<50;j++)vertices.Add(src[leaf*100+j]);
   var indices=source.triangles;for(int k=0;k<indices.Length;k+=3){int a=indices[k],b=indices[k+1],c=indices[k+2];if(a%100>=50||b%100>=50||c%100>=50)continue;triangles.Add(a/100*50+a%100);triangles.Add(b/100*50+b%100);triangles.Add(c/100*50+c%100);}
   mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
  }else {Object.DestroyImmediate(mesh);mesh=Object.Instantiate(source);mesh.name="WideKelpGPU";}
  mesh.bounds=new Bounds(new Vector3(0,1,0),new Vector3(8,6,4));var old=AssetDatabase.LoadAssetAtPath<Mesh>(D+"WideKelpGPU.asset");
  if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);return old;}AssetDatabase.CreateAsset(mesh,D+"WideKelpGPU.asset");return mesh;
 }
 static Material Mat(string name,string shader){var m=AssetDatabase.LoadAssetAtPath<Material>(D+name+".mat");if(!m){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,D+name+".mat");}m.shader=Shader.Find(shader);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;}
 [MenuItem("Ultrasound Reef/Create Probe Examination Preview")]
 public static void Build(){
  if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play before creating the examination preview.");return;}
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/UnderwaterGelPreview.unity");
  var gel=Object.FindAnyObjectByType<GelSeaweedPreview>();if(!gel){Debug.LogError("Gel controller missing.");return;}
  var kelp=Mat("Rooted kelp GPU","UltrasoundReef/RootedKelp");kelp.SetColor("_BaseColor",new Color(.561f,.655f,.494f));kelp.SetColor("_TipColor",new Color(.7f,.77f,.59f));kelp.SetFloat("_Interactive",0);kelp.SetFloat("_MotionTime",-1);
  var gpuKelp=KelpForGpu();
  foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)){
   var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)continue;
   if(f.sharedMesh.name.Contains("Kelp")||f.sharedMesh.name.Contains("kelp")||f.sharedMesh.name=="SeaweedRibbon"){
    r.sharedMaterial=kelp;f.sharedMesh=gpuKelp;
   }
  }
  gel.useGpuDeformation=true;gel.handleInput=false;gel.showUI=false;gel.gelActive=false;gel.paused=false;
  var originalFish=GameObject.Find("Fish schools");if(originalFish)originalFish.SetActive(false);
  var root=new GameObject("Probe examination and ambient life");var flow=root.AddComponent<ProbeExamFlow>();flow.gel=gel;flow.cameraLook=Camera.main.GetComponent<ReefDesktopLook>();
  flow.probeTarget=new GameObject("Simulated probe contact - keyboard").transform;flow.probeTarget.SetParent(root.transform);
  var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);marker.name="Simulated probe position marker";Object.DestroyImmediate(marker.GetComponent<Collider>());marker.transform.SetParent(flow.probeTarget,false);marker.transform.localScale=Vector3.one*.045f;
  var markerMat=Mat("Simulated probe marker","Universal Render Pipeline/Unlit");markerMat.color=new Color(1,.73f,.3f);marker.GetComponent<Renderer>().sharedMaterial=markerMat;marker.GetComponent<Renderer>().enabled=false;
  var fish=Fish();var existing=AssetDatabase.LoadAssetAtPath<Mesh>(D+"PastelReefFish.asset");if(existing){EditorUtility.CopySerialized(fish,existing);Object.DestroyImmediate(fish);fish=existing;}else AssetDatabase.CreateAsset(fish,D+"PastelReefFish.asset");
  flow.fishMesh=fish;flow.fishMaterial=Mat("Pastel silver fish GPU","UltrasoundReef/PastelFish");
  var particles=new GameObject("Slow underwater motes",typeof(ParticleSystem));particles.transform.SetParent(root.transform);particles.transform.position=new Vector3(0,2.8f,7);
  var ps=particles.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var main=ps.main;main.maxParticles=96;main.startLifetime=new ParticleSystem.MinMaxCurve(16,22);main.startSpeed=.015f;main.startSize=new ParticleSystem.MinMaxCurve(.025f,.045f);main.simulationSpace=ParticleSystemSimulationSpace.World;main.startColor=new Color(.75f,.94f,.95f,.25f);main.prewarm=true;main.loop=true;
  var emission=ps.emission;emission.rateOverTime=3;
  var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(12,5,10);
  var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=new ParticleSystem.MinMaxCurve(.008f);velocity.y=new ParticleSystem.MinMaxCurve(.02f);velocity.z=new ParticleSystem.MinMaxCurve(.005f);
  var col=ps.colorOverLifetime;col.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(.75f,.94f,.95f),0),new GradientColorKey(new Color(.75f,.94f,.95f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.3f,.2f),new GradientAlphaKey(.3f,.8f),new GradientAlphaKey(0,1)});col.color=gradient;
  var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Mat("Soft underwater motes","UltrasoundReef/SoftMote");renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.maxParticleSize=.015f;flow.motes=ps;
  EditorSceneManager.SaveScene(scene,"Assets/Scenes/UnderwaterExamPreview.unity");AssetDatabase.SaveAssets();
  Debug.Log("Probe examination preview created: idle > gel > probe; 56 instanced fish maximum, 56-triangle fish mesh, rooted GPU kelp and <=96 motes. No real tracking connected.");
 }
}
