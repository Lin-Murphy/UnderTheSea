using UnityEngine;
using UnityEngine.InputSystem;

namespace UltrasoundReef
{
    // Human-operated preview. No breath, pressure or tracking measurements.
    public class ReefFlow : MonoBehaviour
    {
        public enum Phase { Enter, Gel, Probe, Breathing, FarewellAndWipe, Exit }
        public enum Breath { Inhale, Hold, Exhale }
        public Phase phase;
        public Breath breath;
        public bool paused;
        Transform body, mirror, jelly, fish, grass, world;
        Camera view;
        Light sun;
        Material coralMaterial, jellyMaterial;
        float clock, growth = 1, glow, darkness;
        Vector3 jellyHome = new Vector3(0, 2.6f, 3.4f);
        readonly string[] titles = { "1 / 6  ENTER REEF", "2 / 6  APPLY GEL", "3 / 6  PROBE EXAMINATION", "4 / 6  BREATHING + EXAMINATION", "5 / 6  FAREWELL + WIPE GEL", "6 / 6  EXIT REEF" };

        void Awake()
        {
            world = new GameObject("Reef environment").transform;
            world.SetParent(transform);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = .035f;
            RenderSettings.ambientLight = new Color(.38f, .65f, .72f);
            view = Camera.main;
            if (!view) { var c = new GameObject("Patient preview camera", typeof(Camera), typeof(AudioListener)); view = c.GetComponent<Camera>(); c.tag = "MainCamera"; }
            view.transform.SetPositionAndRotation(new Vector3(0, 1.25f, 0), Quaternion.Euler(-16, 0, 0));
            view.clearFlags = CameraClearFlags.SolidColor;
            view.nearClipPlane = .05f;
            var l = new GameObject("Sun", typeof(Light)); l.transform.SetParent(world);
            sun = l.GetComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.4f;
            l.transform.rotation = Quaternion.Euler(45, -35, 0);
            Shape("Sand", PrimitiveType.Cube, new Vector3(0,-.22f,6), new Vector3(30,.4f,30), Mat(new Color(.54f,.76f,.72f)), world);
            coralMaterial = Mat(new Color(1,.42f,.44f));
            body = Coral("Patient coral", new Vector3(0,.15f,.85f), coralMaterial);
            mirror = Coral("Mirror coral - synchronized proxy", new Vector3(0,2,5.5f), Mat(new Color(.65f,.24f,.36f)));
            for (int i = 0; i < 18; i++)
            {
                float angle = i * 2.39996f;
                float radius = 4 + i % 4;
                var p = new Vector3(Mathf.Sin(angle)*radius,0,5+Mathf.Cos(angle)*radius);
                if (Mathf.Abs(p.x)<2 && p.z<7) p.x += 3;
                var c = Coral("Decorative coral " + i,p,Mat(Color.Lerp(new Color(1,.63f,.58f),new Color(.7f,.6f,.88f),i%5/4f)));
                c.localScale *= .5f + i%3*.3f;
            }
            jelly = new GameObject("Companion jellyfish").transform; jelly.SetParent(world); jelly.position = jellyHome;
            jellyMaterial = Mat(new Color(.72f,.64f,1));
            Shape("Bell",PrimitiveType.Sphere,Vector3.zero,new Vector3(.8f,.45f,.8f),jellyMaterial,jelly,true);
            for(int i=0;i<6;i++) Shape("Tentacle",PrimitiveType.Capsule,new Vector3((i-2.5f)*.1f,-.48f,0),new Vector3(.045f,.32f,.045f),jellyMaterial,jelly,true);
            fish = new GameObject("Probe fish group - animated placeholder").transform; fish.SetParent(world);
            var gold = Mat(new Color(1,.85f,.32f));
            for(int i=0;i<7;i++) Shape("Fish",PrimitiveType.Sphere,new Vector3((i%3-1)*.26f,i*.07f,i*.15f),new Vector3(.12f,.09f,.25f),gold,fish,true);
            grass = new GameObject("Gel seaweed - animated placeholder").transform; grass.SetParent(world); grass.position = new Vector3(-.6f,.5f,1.1f);
            var green = Mat(new Color(.3f,.8f,.67f));
            for(int i=0;i<5;i++) Shape("Frond",PrimitiveType.Capsule,new Vector3(i*.15f,0,0),new Vector3(.08f,.55f,.08f),green,grass,true);
            SetPhase(Phase.Enter);
        }
        Material Mat(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) shader = Shader.Find("Standard");
            var m = new Material(shader); m.color = color; return m;
        }
        Transform Shape(string label,PrimitiveType type,Vector3 position,Vector3 scale,Material mat,Transform parent,bool local=false)
        {
            var o=GameObject.CreatePrimitive(type); o.name=label; o.transform.SetParent(parent);
            if(local) o.transform.localPosition=position; else o.transform.position=position;
            o.transform.localScale=scale; o.GetComponent<Renderer>().sharedMaterial=mat;
            Destroy(o.GetComponent<Collider>()); return o.transform;
        }
        Transform Coral(string label,Vector3 position,Material mat)
        {
            var root=new GameObject(label).transform; root.SetParent(world); root.position=position;
            Shape("Stem",PrimitiveType.Capsule,new Vector3(0,.45f,0),new Vector3(.2f,.5f,.2f),mat,root,true);
            for(int i=0;i<6;i++)
            {
                float sign=i%2==0?-1:1;
                var b=Shape("Branch",PrimitiveType.Capsule,new Vector3(sign*(.2f+i*.035f),.5f+i*.14f,0),new Vector3(.13f,.35f,.13f),mat,root,true);
                b.localRotation=Quaternion.Euler(0,0,sign*-42);
            }
            return root;
        }
        public void SetPhase(Phase next)
        {
            phase=next; breath=Breath.Inhale;
            grass.gameObject.SetActive(phase==Phase.Gel);
            fish.gameObject.SetActive(phase==Phase.Probe || phase==Phase.Breathing);
            jelly.gameObject.SetActive(phase==Phase.Breathing || phase==Phase.FarewellAndWipe);
            Debug.Log("Reef stage: " + phase + " (manual preview)");
        }
        public void Next()
        {
            if(paused) return;
            if(phase==Phase.Breathing && breath!=Breath.Exhale) { breath=(Breath)((int)breath+1); return; }
            if(phase!=Phase.Exit) SetPhase((Phase)((int)phase+1));
        }
        void Update()
        {
            var k=Keyboard.current;
            if(k!=null)
            {
                if(k.pKey.wasPressedThisFrame) paused=!paused;
                if(k.escapeKey.wasPressedThisFrame) { paused=false; SetPhase(Phase.Exit); }
                if(k.rKey.wasPressedThisFrame) { paused=false; SetPhase(Phase.Enter); }
                if(k.spaceKey.wasPressedThisFrame) Next();
                if(k.leftArrowKey.wasPressedThisFrame && !paused && phase!=Phase.Enter) SetPhase((Phase)((int)phase-1));
            }
            if(paused) return;
            clock+=Time.deltaTime;
            bool breathing=phase==Phase.Breathing && breath!=Breath.Exhale;
            growth=Mathf.MoveTowards(growth,breathing?1.3f:1,Time.deltaTime*.15f);
            glow=Mathf.MoveTowards(glow,breathing?1:0,Time.deltaTime*.5f);
            darkness=Mathf.MoveTowards(darkness,breathing?1:0,Time.deltaTime*.5f);
            body.localScale=mirror.localScale=Vector3.one*growth;
            coralMaterial.SetColor("_EmissionColor",new Color(.8f,.25f,.1f)*glow); coralMaterial.EnableKeyword("_EMISSION");
            var mm=mirror.GetChild(0).GetComponent<Renderer>().sharedMaterial;
            mm.EnableKeyword("_EMISSION"); mm.SetColor("_EmissionColor",new Color(.8f,.25f,.1f)*glow);
            jellyMaterial.EnableKeyword("_EMISSION"); jellyMaterial.SetColor("_EmissionColor",new Color(.4f,.3f,1)*glow);
            view.backgroundColor=Color.Lerp(new Color(.08f,.56f,.7f),new Color(.015f,.06f,.18f),darkness);
            RenderSettings.fogColor=view.backgroundColor; sun.intensity=Mathf.Lerp(1.4f,.25f,darkness);
            grass.localRotation=Quaternion.Euler(0,0,Mathf.Sin(clock)*20);
            fish.position=phase==Phase.Breathing?new Vector3(0,.65f,1.1f):new Vector3(Mathf.Sin(clock*.6f)*.6f,.85f,1.4f+Mathf.Cos(clock*.6f)*.2f);
            jelly.position=Vector3.Lerp(jelly.position,(phase==Phase.FarewellAndWipe?new Vector3(.5f,1.7f,1.8f):jellyHome)+Vector3.up*Mathf.Sin(clock)*.06f,Time.deltaTime*2);
        }
        void OnGUI()
        {
            GUI.Box(new Rect(12,12,490,phase==Phase.Breathing?180:155),"");
            GUI.Label(new Rect(26,22,460,24),"ULTRASOUND: UNDER THE SEA | STEP 1");
            GUI.Label(new Rect(26,50,460,24),titles[(int)phase]+(paused?"  [PAUSED]":""));
            GUI.Label(new Rect(26,76,460,24),"SPACE: next | Left: previous | P: pause | R: restart | Esc: end");
            GUI.Label(new Rect(26,101,460,24),"Manual placeholders. No AR / tracking / breath sensing.");
            if(phase==Phase.Breathing) GUI.Label(new Rect(26,126,460,24),"Breath: "+breath+" | advance only on operator instruction");
            if(GUI.Button(new Rect(26,phase==Phase.Breathing?154:127,140,24),"Next / Space")) Next();
        }
    }
}
