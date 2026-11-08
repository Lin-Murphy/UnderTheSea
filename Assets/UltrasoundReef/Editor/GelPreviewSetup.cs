using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using UltrasoundReef;
public static class GelPreviewSetup {
 const string Dir="Assets/UltrasoundReef/Environment/";
 static Mesh Kelp(){
  var v=new List<Vector3>();var t=new List<int>();
  for(int leaf=0;leaf<7;leaf++){
   float angle=leaf*2.39996f, height=1.1f+(leaf%4)*.42f;
   var rotation=Quaternion.Euler(0,angle*Mathf.Rad2Deg,0);
   for(int side=0;side<2;side++){
    int start=v.Count;
    for(int j=0;j<=24;j++){
     float h=j/24f;float width=Mathf.Pow(Mathf.Max(0,Mathf.Sin(h*Mathf.PI)),.8f)*(.13f+(leaf%3)*.035f)+.003f;
     Vector3 center=new Vector3(Mathf.Sin(h*2.6f)*height*.3f,height*h,Mathf.Sin(h*4)*.16f);
     for(int edge=-1;edge<=1;edge+=2)v.Add(rotation*(center+new Vector3(0,-Mathf.Abs(edge)*width*.12f,width*edge)));
     if(j>0){int a=start+(j-1)*2;if(side==0)t.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2});else t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
    }
   }
  }
  var m=new Mesh();m.name="Wide curved kelp - seven leaves";m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
 }
 [MenuItem("Ultrasound Reef/Create Gel Interaction Preview")]
 public static void Build(){
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/UnderwaterGarden.unity");
  var mesh=Kelp();var old=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"WideKelp.asset");
  if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,Dir+"WideKelp.asset");
  var mat=AssetDatabase.LoadAssetAtPath<Material>(Dir+"Wide kelp jade.mat");
  if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,Dir+"Wide kelp jade.mat");}
  mat.color=new Color(.28f,.58f,.43f);mat.SetFloat("_Smoothness",.36f);EditorUtility.SetDirty(mat);
  var root=new GameObject("Gel interaction - desktop preview");
  var controller=root.AddComponent<GelSeaweedPreview>();
  System.Func<string,Vector3,Vector3,Transform> make=(name,p,s)=>{
   var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(root.transform);o.transform.position=p;o.transform.localScale=s;o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=mat;return o.transform;
  };
  controller.seaweed=make("Interactive kelp - simulated applicator",new Vector3(-1.6f,-.4f,.9f),Vector3.one*.65f);
  controller.mirrorSeaweed=make("Mirror kelp - synchronized illustration",new Vector3(-.9f,1.7f,6),Vector3.one*.42f);
  controller.bodyContact=new GameObject("Body contact anchor - calibrate for real use").transform;controller.bodyContact.SetParent(root.transform);controller.bodyContact.position=new Vector3(-.35f,-.35f,1);
  var garden=GameObject.Find("Sea grass");if(garden){int i=0;foreach(Transform plant in garden.transform){plant.GetComponent<MeshFilter>().sharedMesh=mesh;plant.GetComponent<Renderer>().sharedMaterial=mat;plant.localScale*=.65f;if(i++%2==0)plant.gameObject.SetActive(false);}}
  if(Camera.main)Camera.main.gameObject.AddComponent<ReefDesktopLook>();
  PrefabUtility.SaveAsPrefabAsset(controller.seaweed.gameObject,Dir+"WideKelp.prefab");
  EditorSceneManager.SaveScene(scene,"Assets/Scenes/UnderwaterGelPreview.unity");AssetDatabase.SaveAssets();
  Debug.Log("Gel preview saved. Play, drag mouse to look. Press 2 to simulate gel sweep. No tracking connected.");
 }
}
