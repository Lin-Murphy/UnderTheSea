using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ReefEnvironmentBuilder
{
    const string Dir="Assets/UltrasoundReef/Environment";
    static System.Random random;
    static float R(float a,float b)=>a+(b-a)*(float)random.NextDouble();
    static Material Material(string name,Color color,float emission=0)
    {
        string path=Dir+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Smoothness",.32f);
        if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);}
        EditorUtility.SetDirty(m);return m;
    }
    static Mesh Save(Mesh m,string name)
    {
        m.name=name; string path=Dir+"/"+name+".asset";
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){EditorUtility.CopySerialized(m,old);return old;}
        AssetDatabase.CreateAsset(m,path);return m;
    }
    // Tapered curved tubes form a reusable single-mesh branching coral.
    static Mesh CoralMesh(int seed)
    {
        random=new System.Random(seed);var v=new List<Vector3>();var t=new List<int>();
        System.Action<Vector3,Vector3,float,float> tube=(a,b,ra,rb)=>{
            int start=v.Count;Vector3 d=(b-a).normalized;Vector3 u=Vector3.Cross(d,Vector3.forward).normalized;
            if(u.sqrMagnitude<.1f)u=Vector3.right;Vector3 w=Vector3.Cross(d,u);
            for(int j=0;j<=5;j++)for(int k=0;k<7;k++){
                float f=j/5f,angle=k*Mathf.PI*2/7;
                Vector3 p=Vector3.Lerp(a,b,f)+u*Mathf.Sin(f*Mathf.PI)*(b-a).magnitude*.08f;
                v.Add(p+(u*Mathf.Cos(angle)+w*Mathf.Sin(angle))*Mathf.Lerp(ra,rb,f));
            }
            for(int j=0;j<5;j++)for(int k=0;k<7;k++){
                int x=start+j*7+k,y=start+j*7+(k+1)%7;
                t.AddRange(new[]{x,y,x+7,y,y+7,x+7});
            }
            int cap=v.Count;v.Add(b);for(int k=0;k<7;k++)t.AddRange(new[]{start+35+k,start+35+(k+1)%7,cap});
        };
        System.Action<Vector3,Vector3,float,int> branch=null;
        branch=(a,d,r,depth)=>{
            Vector3 b=a+d;tube(a,b,r,r*.55f);
            if(depth==0)return;
            for(int i=0;i<2;i++){
                Vector3 dir=new Vector3((i==0?-1:1)*R(.35f,.7f),R(.45f,.85f),R(-.3f,.3f));
                branch(b,dir*d.magnitude*.8f,r*.59f,depth-1);
            }
        };
        branch(Vector3.zero,new Vector3(.04f,1,0),.16f,4);
        var m=new Mesh();m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
    }
    static Mesh SeaweedMesh()
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int j=0;j<=12;j++){
            float h=j/12f;float x=Mathf.Sin(h*4)*.15f;float width=Mathf.Sin(h*Mathf.PI)*.14f+.008f;
            v.Add(new Vector3(x-width,h*1.8f,Mathf.Sin(h*3)*.1f));v.Add(new Vector3(x+width,h*1.8f,Mathf.Sin(h*3)*.1f));
            if(j>0){int k=j*2;t.AddRange(new[]{k-2,k,k-1,k-1,k,k+1,k-1,k,k-2,k+1,k,k-1});}
        }
        var m=new Mesh();m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();return m;
    }
    static GameObject Place(string name,Mesh mesh,Material mat,Vector3 p,Vector3 scale,Transform parent)
    {
        var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent);o.transform.localPosition=p;o.transform.localScale=scale;
        o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=mat;return o;
    }
    static GameObject Primitive(string name,PrimitiveType type,Vector3 p,Vector3 s,Material mat,Transform parent)
    {
        var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent);o.transform.position=p;o.transform.localScale=s;
        Object.DestroyImmediate(o.GetComponent<Collider>());o.GetComponent<Renderer>().sharedMaterial=mat;return o;
    }
    [MenuItem("Ultrasound Reef/Build Underwater Art Scene")]
    public static void Build()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        System.IO.Directory.CreateDirectory(Dir);AssetDatabase.Refresh();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("UNDERWATER GARDEN - editable environment");
        var terrain=new GameObject("Terrain and layered rocks").transform;terrain.SetParent(root.transform);
        var corals=new GameObject("Coral gardens").transform;corals.SetParent(root.transform);
        var plants=new GameObject("Sea grass").transform;plants.SetParent(root.transform);
        var fauna=new GameObject("Fish schools").transform;fauna.SetParent(root.transform);
        var pink=Material("Peach coral",new Color(1,.57f,.5f));
        var lavender=Material("Lavender coral",new Color(.71f,.55f,.85f));
        var cream=Material("Golden coral",new Color(1,.83f,.48f));
        var teal=Material("Seaweed jade",new Color(.16f,.55f,.43f));
        var sand=Material("Sand turquoise",new Color(.48f,.73f,.69f));
        var stone=Material("Rock ocean blue",new Color(.15f,.35f,.4f));
        var fishmat=Material("Fish golden",new Color(1,.81f,.35f));
        var pale=Material("Pearl jelly",new Color(.6f,.8f,.95f),.4f);
        Mesh[] meshes={Save(CoralMesh(12),"BranchCoral_A"),Save(CoralMesh(37),"BranchCoral_B"),Save(CoralMesh(89),"BranchCoral_C")};
        var weed=Save(SeaweedMesh(),"SeaweedRibbon");random=new System.Random(42);
        // Gently undulating sandy terrain.
        var verts=new List<Vector3>();var tris=new List<int>();int n=40;
        for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){
            float px=(x-n/2)*1.5f,pz=(z-n/2)*1.5f;
            verts.Add(new Vector3(px,Mathf.PerlinNoise(px*.09f+10,pz*.09f+10)*.65f-.7f,pz));
            if(z<n&&x<n){int k=z*(n+1)+x;tris.AddRange(new[]{k,k+n+1,k+1,k+1,k+n+1,k+n+2});}
        }
        var ground=new Mesh();ground.SetVertices(verts);ground.SetTriangles(tris,0);ground.RecalculateNormals();
        Place("Sculpted sand",Save(ground,"SandTerrain"),sand,Vector3.zero,Vector3.one,terrain);
        for(int side=-1;side<=1;side+=2)for(int tier=0;tier<5;tier++){
            float x=side*(4+tier*.9f),z=3+tier*3.2f;
            Primitive("Reef rock terrace",PrimitiveType.Sphere,new Vector3(x,-.4f+tier*.18f,z),new Vector3(R(3,5),R(1,2.7f),R(3,5)),stone,terrain);
        }
        // A distant arch built from weathered rounded rock masses.
        for(int i=0;i<9;i++){
            float a=i/8f*Mathf.PI;
            Primitive("Distant arch",PrimitiveType.Sphere,new Vector3(7+Mathf.Cos(a)*3,Mathf.Sin(a)*5,19),new Vector3(2.2f,2.4f,2),stone,terrain);
        }
        for(int i=0;i<165;i++){
            float x=R(-13,13),z=R(-4,23);
            if(Mathf.Abs(x)<2.8f&&z>0&&z<12)continue;
            var m=i%3==0?cream:i%3==1?pink:lavender;
            var c=Place("Branching coral "+i,meshes[i%3],m,new Vector3(x,-.3f,z),Vector3.one*R(.35f,1.15f),corals);
            c.transform.localRotation=Quaternion.Euler(0,R(0,360),R(-8,8));
        }
        for(int i=0;i<100;i++){
            float x=R(-12,12),z=R(-3,23);if(Mathf.Abs(x)<2&&z<10)continue;
            var w=Place("Ribbon grass "+i,weed,teal,new Vector3(x,-.4f,z),Vector3.one*R(.4f,1.5f),plants);w.transform.localRotation=Quaternion.Euler(0,R(0,360),R(-12,12));
        }
        Place("Patient coral",meshes[0],pink,new Vector3(0,-.65f,.8f),Vector3.one*.65f,root.transform);
        Place("Mirror coral - reserved interaction",meshes[0],lavender,new Vector3(0,2,6),Vector3.one*.6f,root.transform);
        // Fish silhouettes with separate tail fins, grouped for later animation.
        for(int i=0;i<50;i++){
            Vector3 p=new Vector3(R(-9,9),R(2.8f,6),R(9,22));
            var f=new GameObject("Reef fish "+i).transform;f.SetParent(fauna);f.position=p;
            Primitive("Body",PrimitiveType.Sphere,p,new Vector3(.24f,.13f,.09f),fishmat,f);
            var tail=Primitive("Tail",PrimitiveType.Cube,p+Vector3.left*.17f,new Vector3(.11f,.15f,.025f),fishmat,f);tail.transform.localRotation=Quaternion.Euler(0,0,45);
        }
        var jelly=new GameObject("Companion jellyfish - art proxy").transform;jelly.SetParent(root.transform);jelly.position=new Vector3(.8f,2.8f,4);
        Primitive("Pearl bell",PrimitiveType.Sphere,jelly.position,new Vector3(.8f,.45f,.8f),pale,jelly);
        for(int i=0;i<8;i++)Primitive("Soft tentacle",PrimitiveType.Capsule,jelly.position+new Vector3(Mathf.Cos(i*.785f)*.23f,-.55f,Mathf.Sin(i*.785f)*.23f),new Vector3(.035f,.4f,.035f),pale,jelly);
        RenderSettings.skybox=null;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogColor=new Color(.08f,.49f,.62f);RenderSettings.fogDensity=.035f;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.5f,.8f,.86f);RenderSettings.ambientEquatorColor=new Color(.23f,.47f,.52f);RenderSettings.ambientGroundColor=new Color(.15f,.3f,.33f);
        var sun=new GameObject("Filtered sunlight",typeof(Light));sun.transform.rotation=Quaternion.Euler(65,-25,0);var light=sun.GetComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.94f,.8f);light.intensity=2;light.shadows=LightShadows.Soft;
        var cam=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cam.tag="MainCamera";cam.transform.position=new Vector3(0,1.6f,-3.5f);cam.transform.rotation=Quaternion.Euler(-8,0,0);
        var ccam=cam.GetComponent<Camera>();ccam.clearFlags=CameraClearFlags.SolidColor;ccam.backgroundColor=RenderSettings.fogColor;ccam.fieldOfView=65;ccam.nearClipPlane=.05f;ccam.farClipPlane=85;ccam.allowHDR=true;
        ccam.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        var volume=new GameObject("Underwater color and bloom",typeof(Volume)).GetComponent<Volume>();volume.isGlobal=true;
        string vp=Dir+"/UnderwaterVolume.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(vp);
        if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,vp);}
        if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}bloom.intensity.Override(.3f);bloom.threshold.Override(1);
        volume.sharedProfile=profile;
        PrefabUtility.SaveAsPrefabAsset(root,Dir+"/UnderwaterGarden.prefab");
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/UnderwaterGarden.unity");AssetDatabase.SaveAssets();
        Selection.activeGameObject=root;
        Debug.Log("Underwater garden built: saved mesh assets, materials, editable prefab and scene.");
    }
}
