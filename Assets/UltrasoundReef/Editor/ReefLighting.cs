using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ReefLighting
{
 const string D="Assets/UltrasoundReef/Environment/";
 [MenuItem("Ultrasound Reef/Apply Sunlit Water Lighting")]
 public static void Apply()
 {
  if(EditorSceneManager.GetActiveScene().name!="UnderwaterGarden") { Debug.LogWarning("Open UnderwaterGarden before applying lighting.");return; }
  var shader=Shader.Find("UltrasoundReef/CoralSunlight");var skyShader=Shader.Find("UltrasoundReef/SunlitWaterSky");
  if(!shader||!skyShader){Debug.LogError("Lighting shaders not imported yet.");return;}
  foreach(string name in new[]{"Peach coral","Lavender coral","Golden coral"}){
   var m=AssetDatabase.LoadAssetAtPath<Material>(D+name+".mat"); if(!m)continue;
   Color color=m.color;m.shader=shader;m.SetColor("_BaseColor",color);m.SetFloat("_Glow",.85f);m.enableInstancing=true;EditorUtility.SetDirty(m);
  }
  var sky=AssetDatabase.LoadAssetAtPath<Material>(D+"Sunlit water sky.mat");
  if(!sky){sky=new Material(skyShader);AssetDatabase.CreateAsset(sky,D+"Sunlit water sky.mat");}
  RenderSettings.skybox=sky;RenderSettings.fogColor=new Color(.14f,.62f,.7f);RenderSettings.fogDensity=.018f;
  RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.6f,.85f,.85f);RenderSettings.ambientEquatorColor=new Color(.28f,.52f,.58f);RenderSettings.ambientGroundColor=new Color(.12f,.28f,.32f);
  var sun=GameObject.Find("Filtered sunlight");if(sun){sun.transform.rotation=Quaternion.LookRotation(-new Vector3(-.28f,.63f,.72f));var l=sun.GetComponent<Light>();l.intensity=2.3f;l.color=new Color(1,.91f,.73f);l.shadowStrength=.55f;RenderSettings.sun=l;}
  foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) {c.clearFlags=CameraClearFlags.Skybox;c.allowHDR=true;c.GetUniversalAdditionalCameraData().renderPostProcessing=true;}
  var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(D+"UnderwaterVolume.asset");
  if(profile){
   if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}bloom.intensity.Override(.65f);bloom.threshold.Override(1.05f);bloom.scatter.Override(.75f);
   if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>();AssetDatabase.AddObjectToAsset(tone,profile);}tone.mode.Override(TonemappingMode.ACES);
   if(!profile.TryGet<ColorAdjustments>(out var color)){color=profile.Add<ColorAdjustments>();AssetDatabase.AddObjectToAsset(color,profile);}color.postExposure.Override(.35f);color.saturation.Override(8);color.contrast.Override(8);
   EditorUtility.SetDirty(profile);
  }
  EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();Debug.Log("Sunlit water lighting applied and saved.");
 }
}
