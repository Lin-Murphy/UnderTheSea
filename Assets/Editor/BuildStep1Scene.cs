using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnderTheSea;

/// <summary>一键生成占位场景(步骤 1,含步骤 2 的探头跟随):菜单 UnderTheSea/Build Step1 Scene。保存为独立的 LegacyStep1Preview.unity，不覆盖主场景。</summary>
public static class BuildStep1Scene
{
    const string ScenePath = "Assets/Scenes/LegacyStep1Preview.unity";

    [MenuItem("UnderTheSea/Legacy/Build Placeholder Preview")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var rng = new System.Random(7);

        // 材质
        var mGround = Mat("Ground", new Color(0.85f, 0.78f, 0.55f));
        var mHero = Mat("HeroCoral", new Color(1f, 0.55f, 0.45f));
        var mDeco = new[] { Mat("Deco1", new Color(1f, 0.75f, 0.4f)), Mat("Deco2", new Color(0.7f, 0.5f, 0.95f)), Mat("Deco3", new Color(0.95f, 0.5f, 0.7f)) };
        var mWeed = Mat("Seaweed", new Color(0.2f, 0.65f, 0.3f));
        var mFishNear = Mat("FishNear", new Color(1f, 0.6f, 0.2f));
        var mFishSchool = Mat("FishSchool", new Color(0.4f, 0.7f, 1f));
        var mFishInteract = Mat("FishInteract", new Color(1f, 0.9f, 0.2f));
        var mJelly = Mat("Jelly", new Color(0.55f, 0.8f, 1f), emission: true);
        var mProbe = Mat("Probe", Color.white);

        // 摄像机(患者视角,占位;接入 XR 后由 XR Origin 替换)
        // 摄像机:Meta 的 OVRCameraRig(开启 Passthrough,带 Underlay 的 OVRPassthroughLayer)
        var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab");
        var rigGo = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
        rigGo.name = "OVRCameraRig";
        rigGo.transform.position = new Vector3(0, 1.0f, 0);   // 编辑器里的占位头部高度
        var ovrManager = rigGo.GetComponentInChildren<OVRManager>();
        if (ovrManager == null) ovrManager = rigGo.AddComponent<OVRManager>();
        ovrManager.isInsightPassthroughEnabled = true;
        var ptLayer = rigGo.GetComponent<OVRPassthroughLayer>();
        if (ptLayer == null) ptLayer = rigGo.AddComponent<OVRPassthroughLayer>();
        var cam = rigGo.GetComponentInChildren<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.nearClipPlane = 0.05f;

        // 工程层面开启 Passthrough 支持(写入 AndroidManifest 的功能声明)
        var cfg = OVRProjectConfig.CachedProjectConfig;
        cfg.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Required;
        OVRProjectConfig.CommitProjectConfig(cfg);

        // 光
        var sunGo = new GameObject("Sun");
        sunGo.transform.rotation = Quaternion.Euler(50, -30, 0);
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;

        // 雾
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.02f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

        // 水下根节点(待机阶段隐藏)
        var root = new GameObject("UnderwaterRoot").transform;

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(root);
        ground.transform.localScale = new Vector3(8, 1, 8);
        ground.GetComponent<Renderer>().sharedMaterial = mGround;
        Object.DestroyImmediate(ground.GetComponent<Collider>());

        // 主角珊瑚
        var hero = MakeCoral("HeroCoral", root, mHero, new Vector3(0, 0, 0), 1.0f);

        // 装饰珊瑚
        var decoCorals = new List<Transform>();
        for (int i = 0; i < 16; i++)
        {
            float a = (float)(rng.NextDouble() * Mathf.PI * 2);
            float r = 3f + (float)rng.NextDouble() * 8f;
            var p = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            if ((p - new Vector3(0, 0, 4f)).magnitude < 2f) continue; // 给镜像珊瑚让位
            decoCorals.Add(MakeCoral($"DecoCoral{i}", root, mDeco[i % 3], p, 0.6f + (float)rng.NextDouble() * 1.0f));
        }

        // 环境水草
        var weeds = new List<Transform>();
        for (int i = 0; i < 22; i++)
        {
            float a = (float)(rng.NextDouble() * Mathf.PI * 2);
            float r = 1.5f + (float)rng.NextDouble() * 8f;
            weeds.Add(MakeSeaweed($"Seaweed{i}", root, mWeed, new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), 0.8f + (float)rng.NextDouble() * 0.8f));
        }

        // 场景2 用的"抚摸水草"(占位,步骤3 换成 AprilTag 驱动)
        var stroke = new GameObject("StrokeSeaweed").transform;
        stroke.SetParent(root);
        for (int i = 0; i < 3; i++)
            MakeSeaweed($"StrokeWeed{i}", stroke, mWeed, new Vector3((i - 1) * 0.15f, 0, 0), 0.9f);


        // 患者"手"的位置(握手触手没有 tag 时伸向这里)
        var handPoint = new GameObject("HandPoint").transform;
        handPoint.SetParent(root);
        handPoint.position = new Vector3(0.5f, 0.45f, 0.3f);

        // 肚子位置标记 + 假探头
        var belly = new GameObject("BellyPoint").transform;
        belly.SetParent(root);
        belly.position = new Vector3(0, 0.4f, 0.3f);

        var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        probe.name = "ProbeTagMarker";
        probe.transform.SetParent(root);
        probe.transform.localScale = new Vector3(0.06f, 0.06f, 0.2f);
        probe.GetComponent<Renderer>().sharedMaterial = mProbe;
        Object.DestroyImmediate(probe.GetComponent<Collider>());

        // 小鱼
        var near = MakeFish("NearFish", root, mFishNear, 12, 0.25f);
        var school = MakeFish("SchoolFish", root, mFishSchool, 40, 0.35f);
        var interact = MakeFish("InteractFish", root, mFishInteract, 10, 0.18f);

        // 水母(3 只,home 位置为"飘来后停留"的位置)
        var jellies = new List<Transform>();
        var jellyRenderers = new List<Renderer>();
        Vector3[] homes = { new Vector3(-1.5f, 2.2f, 2.0f), new Vector3(2.0f, 2.8f, 3.0f), new Vector3(-0.5f, 3.2f, 5.0f) };
        for (int i = 0; i < homes.Length; i++)
        {
            var j = MakeJelly($"Jelly{i}", root, mJelly, homes[i], jellyRenderers);
            jellies.Add(j);
        }

        // 水母握手触手(分节)+ 镜像珊瑚上的同步副本
        var handTentacle = MakeChain("HandTentacle", root, mJelly, 10, jellyRenderers);

        // 转场鱼群(放在场景根下,不受 UnderwaterRoot 隐藏影响,现实空间里也能看见)
        var mFishTransition = Mat("FishTransition", new Color(1f, 0.75f, 0.25f));
        var fishTransition = MakeFish("TransitionFish", null, mFishTransition, 36, 0.22f);

        // 控制器 / 输入 / HUD / 表现层
        var exp = new GameObject("Experience");
        var controller = exp.AddComponent<ExperienceController>();
        var input = exp.AddComponent<KeyboardStageInput>(); input.controller = controller;
        var xrInput = exp.AddComponent<XRControllerStageInput>(); xrInput.controller = controller;
        var tagSource = exp.AddComponent<MockTagPoseSource>(); tagSource.bellyPoint = belly;
        var hud = exp.AddComponent<StageHud>(); hud.controller = controller;
        var view = exp.AddComponent<OceanStageView>();
        view.controller = controller;
        var bridge = exp.AddComponent<PassthroughBridge>();
        bridge.manager = ovrManager;
        bridge.layer = ptLayer;
        var startButton = exp.AddComponent<StartButton>();
        startButton.controller = controller;
        startButton.view = view;
        startButton.cam = cam;
        startButton.buttonMaterial = Mat("StartButton", new Color(0.15f, 0.65f, 0.85f));
        view.passthrough = bridge;
        view.ground = ground.transform;
        view.decoCorals = decoCorals.ToArray();
        view.transitionFish = fishTransition;
        view.cam = cam;
        view.sun = sun;
        view.underwaterRoot = root.gameObject;
        view.heroCoral = hero;
        view.bellyPoint = belly;
        view.tagSource = tagSource;
        view.probeMarker = probe.transform;
        view.strokeSeaweedRoot = stroke;
        view.handPoint = handPoint;
        view.handTentacle = handTentacle;
        view.ambientSeaweed = weeds.ToArray();
        view.nearFish = near;
        view.schoolFish = school;
        view.interactFish = interact;
        view.jellies = jellies.ToArray();
        view.jellyRenderers = jellyRenderers.ToArray();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[BuildStep1Scene] 场景已生成: " + ScenePath);
    }

    static Material Mat(string name, Color c, bool emission = false)
    {
        string path = $"Assets/Materials/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader = Shader.Find(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null
            ? "Universal Render Pipeline/Lit" : "Standard");
        var m = existing != null ? existing : new Material(shader);
        m.color = c;
        if (emission) { m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; m.SetColor("_EmissionColor", Color.black); }
        if (existing == null) AssetDatabase.CreateAsset(m, path);
        else EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Prim(PrimitiveType t, string name, Transform parent, Material m)
    {
        var g = GameObject.CreatePrimitive(t);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    static Transform MakeCoral(string name, Transform parent, Material m, Vector3 pos, float scale)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent);
        root.position = pos;
        root.localScale = Vector3.one * scale;
        var trunk = Prim(PrimitiveType.Cylinder, "Trunk", root, m);
        trunk.transform.localPosition = new Vector3(0, 0.4f, 0);
        trunk.transform.localScale = new Vector3(0.35f, 0.4f, 0.35f);
        for (int i = 0; i < 4; i++)
        {
            var b = Prim(PrimitiveType.Capsule, "Branch" + i, root, m);
            float a = i * 90f + 20f;
            b.transform.localPosition = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 0.3f, 0.9f, Mathf.Sin(a * Mathf.Deg2Rad) * 0.3f);
            b.transform.localRotation = Quaternion.Euler(Mathf.Sin(a * Mathf.Deg2Rad) * 30f, 0, -Mathf.Cos(a * Mathf.Deg2Rad) * 30f);
            b.transform.localScale = new Vector3(0.18f, 0.45f, 0.18f);
        }
        return root;
    }

    static Transform MakeSeaweed(string name, Transform parent, Material m, Vector3 pos, float height)
    {
        var root = new GameObject(name).transform;   // 摇摆的枢轴在底部
        root.SetParent(parent);
        root.position = pos;
        var blade = Prim(PrimitiveType.Cylinder, "Blade", root, m);
        blade.transform.localPosition = new Vector3(0, height * 0.5f, 0);
        blade.transform.localScale = new Vector3(0.06f, height * 0.5f, 0.06f);
        return root;
    }

    static Transform[] MakeChain(string name, Transform parent, Material m, int count, List<Renderer> renderers)
    {
        var group = new GameObject(name).transform;
        group.SetParent(parent);
        var arr = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            var s = Prim(PrimitiveType.Sphere, $"{name}{i}", group, m);
            s.transform.localScale = Vector3.one * 0.05f;
            s.SetActive(false);
            renderers.Add(s.GetComponent<Renderer>());
            arr[i] = s.transform;
        }
        return arr;
    }

    static Transform[] MakeFish(string name, Transform parent, Material m, int count, float size)
    {
        var group = new GameObject(name).transform;
        if (parent != null) group.SetParent(parent);
        var arr = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            var f = Prim(PrimitiveType.Sphere, $"{name}{i}", group, m);
            f.transform.localScale = new Vector3(size * 0.4f, size * 0.4f, size);   // 朝向 +Z
            arr[i] = f.transform;
        }
        return arr;
    }

    static Transform MakeJelly(string name, Transform parent, Material m, Vector3 pos, List<Renderer> renderers)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent);
        root.position = pos;
        var bell = Prim(PrimitiveType.Sphere, "Bell", root, m);
        bell.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);
        renderers.Add(bell.GetComponent<Renderer>());
        for (int i = 0; i < 5; i++)
        {
            var t = Prim(PrimitiveType.Cylinder, "Tentacle" + i, root, m);
            float a = i * 72f * Mathf.Deg2Rad;
            t.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.25f, -0.55f, Mathf.Sin(a) * 0.25f);
            t.transform.localScale = new Vector3(0.04f, 0.5f, 0.04f);
            renderers.Add(t.GetComponent<Renderer>());
        }
        return root;
    }
}
