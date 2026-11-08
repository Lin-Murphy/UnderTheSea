using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnderTheSea;
using UltrasoundReef;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;
using Stage = UnderTheSea.Stage;

/// <summary>Reuses the authored exam scene, preserving the project's XR and six-stage controls.</summary>
public static class BuildReefMainScene
{
    const string Art = "Assets/UltrasoundReef/Environment/";
    const string Target = "Assets/Scenes/Main.unity";
    const string Settings = "Assets/Settings/UnderTheSea/";

    [MenuItem("UnderTheSea/Migration/Build Main With Imported Reef Art")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before rebuilding Main.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        // 需要能回滚:要么有迁移前的快照压缩包,要么工程在 Unity Version Control(Plastic)工作区里(Main.unity 可用 VCS 撤销)。
        if (!File.Exists("Migration~/2026-11-05/UnderTheSea-before-migration.zip") && !Directory.Exists(".plastic"))
            throw new InvalidOperationException("No rollback available: pre-migration archive missing and not a Unity Version Control workspace.");

        ConfigurePipeline();
        ConvertLegacyMaterials();
        // Load the supplied, fully dressed scene rather than regenerating its art from older steps.
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/UnderwaterExamPreview.unity", OpenSceneMode.Single);
        var sourceCamera = Camera.main;
        if (!sourceCamera) throw new InvalidOperationException("Source preview camera is missing.");
        Vector3 desktopPosition = sourceCamera.transform.position;
        Quaternion desktopRotation = sourceCamera.transform.rotation;
        float fov = sourceCamera.fieldOfView;
        var gel = Object.FindFirstObjectByType<GelSeaweedPreview>();
        var sourceFlow = Object.FindFirstObjectByType<ProbeExamFlow>();
        if (!gel || !sourceFlow) throw new InvalidOperationException("Source gel/probe controllers are missing.");
        var sourceArrival = sourceFlow.jellyfishArrival;
        var firstJelly = sourceArrival.jellyfishRoot;
        firstJelly.gameObject.SetActive(true);
        var motes = sourceFlow.motes;
        var sourceFlowRoot = sourceFlow.gameObject;
        Object.DestroyImmediate(sourceArrival);
        Object.DestroyImmediate(sourceFlow);
        Object.DestroyImmediate(sourceCamera.gameObject);

        var underwater = FindTransform(scene, "UNDERWATER GARDEN - editable environment");
        underwater.name = "UnderwaterRoot";
        foreach (string label in new[] { "Gel interaction - desktop preview", "Soft water light shafts", "Underwater color and bloom" })
        {
            var item = FindTransform(scene, label);
            if (item) item.SetParent(underwater, true);
        }
        gel.transform.SetParent(underwater, true);
        if (motes) motes.transform.SetParent(underwater, true);
        Object.DestroyImmediate(sourceFlowRoot);
        firstJelly.SetParent(underwater, true);
        // Keep the old art proxies archived in the scene but use the finished GPU fish and moon jelly.
        var oldFish = FindTransform(scene, "Fish schools"); if (oldFish) oldFish.gameObject.SetActive(false);
        var oldJelly = FindTransform(scene, "Companion jellyfish - art proxy"); if (oldJelly) oldJelly.gameObject.SetActive(false);
        // 2026-11-05 协作版素材：珊瑚换成 ReefScene_Unity.fbx 的模型与烘焙贴图，位置/尺寸外框不变。
        CollabArtSwap.ConfigureImport();
        CollabArtSwap.ApplyCorals(FindTransform(scene, "Coral gardens"), gel.contactCoral, null, out int collabCorals);
        Debug.Log("[CollabArt] Replaced patient and " + collabCorals + " decorative corals.");

        // 参与者本身就是珊瑚:删除镜像珊瑚、它的支撑岩石,以及旧的整片式涂胶海草(由 SoftSeaweedStrands 取代)。
        var heroCoral = gel.contactCoral;
        foreach (string label in new[] { "Mirror coral - reserved interaction", "Mirror coral supporting rock" })
        {
            var item = FindTransform(scene, label);
            if (item) Object.DestroyImmediate(item.gameObject);
        }
        Object.DestroyImmediate(gel.gameObject);
        gel = null;

        // 机位:相机放在主角珊瑚上方稍后的位置,低头就能看到"自己"这株珊瑚,抬头看到前方的礁石。
        Bounds heroBounds = heroCoral.GetComponent<Renderer>().bounds;
        // 眼睛高于珊瑚顶部 ~0.55m(憋气时珊瑚长大 1.35 倍也碰不到相机),稍靠后;视线约 45° 向下,珊瑚占画面下半部分。
        desktopPosition = new Vector3(heroBounds.center.x, heroBounds.max.y + .55f, heroBounds.center.z - .55f);
        Vector3 lookAt = new Vector3(heroBounds.center.x, heroBounds.center.y, heroBounds.center.z + .7f);
        desktopRotation = Quaternion.LookRotation(lookAt - desktopPosition);

        var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab");
        if (!rigPrefab) throw new InvalidOperationException("Meta OVRCameraRig prefab is missing.");
        var rigObject = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, scene);
        rigObject.name = "OVRCameraRig";
        rigObject.transform.position = desktopPosition;
        var rig = rigObject.GetComponent<OVRCameraRig>();
        var manager = rigObject.GetComponent<OVRManager>() ?? rigObject.AddComponent<OVRManager>();
        manager.isInsightPassthroughEnabled = true;
        // 头显启动时眼睛就在 rig 位置(主角珊瑚上方),而不是"地面 + 真实身高"。
        manager.trackingOriginType = OVRManager.TrackingOrigin.EyeLevel;
        var layer = rigObject.GetComponent<OVRPassthroughLayer>() ?? rigObject.AddComponent<OVRPassthroughLayer>();
        // This SDK's OVRPassthroughLayer creates an Underlay internally (no public overlay-type setter).
        var camera = rig.centerEyeAnchor.GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.fieldOfView = fov; camera.nearClipPlane = .05f; camera.farClipPlane = 85f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.allowHDR = false;   // Quest Passthrough(Underlay)需要带 alpha 的颜色缓冲;HDR 的 B10G11R11 没有 alpha
        var cameraData = camera.GetComponent<UniversalAdditionalCameraData>() ?? camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = false;
        cameraData.volumeLayerMask = 1;
        var config = OVRProjectConfig.CachedProjectConfig;
        config.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Required;
        config.isPassthroughCameraAccessEnabled = true;   // AprilTag 检测需要读取头显透视相机
        OVRProjectConfig.CommitProjectConfig(config);

        var experience = new GameObject("Experience");
        var controller = experience.AddComponent<ExperienceController>();
        controller.duskDuration = 7.2f; // 1.2 s delay plus the source's complete 6 s arrival.
        experience.AddComponent<KeyboardStageInput>().controller = controller;
        experience.AddComponent<XRControllerStageInput>().controller = controller;
        experience.AddComponent<StageHud>().controller = controller;
        var view = experience.AddComponent<OceanStageView>();
        view.controller = controller; view.cam = camera; view.underwaterRoot = underwater.gameObject;
        view.heroCoral = heroCoral;
        camera.transform.rotation = desktopRotation;
        var patientBounds = view.heroCoral.GetComponent<Renderer>().bounds;
        view.bellyPoint = Anchor("BellyPoint - calibrate for patient", underwater, patientBounds.center);
        view.handPoint = Anchor("HandPoint - calibrate for patient", underwater, patientBounds.center + new Vector3(.5f, 0f, -.25f));
        view.jellyHandPos = patientBounds.center + new Vector3(.9f, .65f, -.3f);
        var tags = experience.AddComponent<MockTagPoseSource>(); tags.bellyPoint = view.bellyPoint;
        // 2026-11-05:真机 AprilTag(移植自 3D-101):Quest 透视相机 + Keijiro AprilTag(tagStandard41h12),
        // 只在场景3 探头检查时检测;组合来源先用真机检测,没有时再用编辑器鼠标模拟。
        var aprilGo = new GameObject("AprilTag tracking (Quest passthrough camera)");
        aprilGo.transform.SetParent(experience.transform, false);
        var cameraAccess = aprilGo.AddComponent<Meta.XR.PassthroughCameraAccess>();
        cameraAccess.enabled = false;   // QuestAprilTagPoseSource 拿到权限后再打开
        cameraAccess.CameraPosition = Meta.XR.PassthroughCameraAccess.CameraPositionType.Left;
        var questTags = aprilGo.AddComponent<QuestAprilTagPoseSource>();
        questTags.cameraAccess = cameraAccess;
        questTags.controller = controller;
        var compositeTags = experience.AddComponent<CompositeTagPoseSource>();
        compositeTags.sources = new TagPoseSource[] { questTags, tags };
        view.tagSource = compositeTags;
        var probeMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        probeMarker.name = "ProbeTagMarker";
        Object.DestroyImmediate(probeMarker.GetComponent<Collider>());
        view.probeMarker = probeMarker.transform;
        view.probeMarker.SetParent(underwater, false);
        view.probeMarker.position = view.bellyPoint.position;
        probeMarker.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Art + "Simulated probe marker.mat");
        view.probeMarker.localScale = Vector3.one * .045f;

        var terrain = FindTransform(scene, "Terrain and layered rocks");
        view.ground = FindTransform(scene, "Sculpted sand");
        var corals = FindTransform(scene, "Coral gardens");
        var seaGrass = FindTransform(scene, "Sea grass");
        var shafts = FindTransform(scene, "Soft water light shafts");
        view.decoCorals = Children(corals).Concat(Children(terrain).Where(t => t != view.ground))
            .Concat(Children(shafts)).ToArray();
        view.ambientSeaweed = Children(seaGrass).ToArray();
        // Reveal/breath animation changes these transforms; a static batch would bake their scale.
        foreach (var renderer in underwater.GetComponentsInChildren<Renderer>(true))
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,
                GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & ~StaticEditorFlags.BatchingStatic);
        var kelpMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Art + "WideKelpGPU.asset");
        var kelpMat = AssetDatabase.LoadAssetAtPath<Material>(Art + "Rooted kelp GPU.mat");
        foreach (var plant in seaGrass.GetComponentsInChildren<MeshFilter>(true))
        {
            plant.sharedMesh = kelpMesh; plant.GetComponent<Renderer>().sharedMaterial = kelpMat;
        }

        // 2026-11-05(第二轮):demo1 布局 = FBX 坐标减去 FBX 里参与者珊瑚的位置,再放到 Main 主角珊瑚的位置上。
        Vector3 fbxPlayer = CollabArtSwap.PlayerCoralPosition();
        Vector3 Demo1(Vector3 fbxPos) => new Vector3(fbxPos.x - fbxPlayer.x + heroBounds.center.x, fbxPos.y, fbxPos.z - fbxPlayer.z + heroBounds.center.z);

        // 海葵(demo1 有,Main 原来没有):贴在沙地/岩石表面,场景4 吸气时尖端发光
        var anemoneMesh = CollabArtSwap.BakeAnemone(out var anemoneMats, out var anemoneSpots);
        var anemoneRoot = Anchor("Anemones (demo1 layout)", underwater, Vector3.zero);
        var anemones = new List<Transform>();
        for (int i = 0; i < anemoneSpots.Count; i++)
        {
            var (spot, height) = anemoneSpots[i];
            Vector3 p = Demo1(spot);
            p.y = GroundHeight(terrain, p) - .02f;
            var a = Anchor("Anemone " + i, anemoneRoot, p);
            a.rotation = Quaternion.Euler(0f, i * 137.5f, 0f);
            a.localScale = Vector3.one * height;
            var mf = a.gameObject.AddComponent<MeshFilter>(); mf.sharedMesh = anemoneMesh;
            var mr = a.gameObject.AddComponent<MeshRenderer>(); mr.sharedMaterials = anemoneMats;
            mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            anemones.Add(a);
        }

        // 海平面 + 天花板水纹:在光柱顶部上方的一大片会起伏的水面,从下往上看有流动的光纹
        var surfaceGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        surfaceGo.name = "Sea surface - ceiling ripples";
        Object.DestroyImmediate(surfaceGo.GetComponent<Collider>());
        surfaceGo.transform.SetParent(underwater, false);
        float shaftTop = shafts ? shafts.GetComponentsInChildren<Renderer>().Max(r => r.bounds.max.y) : 10f;
        // 2026-11-05:海平面升高(光柱顶部上方 4.5m)
        surfaceGo.transform.position = new Vector3(heroBounds.center.x, shaftTop + 4.5f, heroBounds.center.z + 8f);
        surfaceGo.transform.localScale = new Vector3(16f, 1f, 16f);
        var surfaceMat = CloneMaterial("Assets/Materials/SeaSurface.mat", new Material(Shader.Find("UnderTheSea/SeaSurface")));
        surfaceMat.shader = Shader.Find("UnderTheSea/SeaSurface");
        surfaceMat.SetTexture("_Caustics", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "SoftCaustics.png"));
        surfaceMat.SetColor("_DeepColor", Color.Lerp(RenderSettings.fogColor, new Color(.45f, .82f, .9f), .55f));
        EditorUtility.SetDirty(surfaceMat);
        var surfaceRenderer = surfaceGo.GetComponent<MeshRenderer>();
        surfaceRenderer.sharedMaterial = surfaceMat;
        surfaceRenderer.shadowCastingMode = ShadowCastingMode.Off; surfaceRenderer.receiveShadows = false;
        view.decoCorals = view.decoCorals.Concat(anemones).Append(surfaceGo.transform).ToArray();
        // 2026-11-05(二):鱼全部换成 /main/demo1 分支的鱼 —— ReefScene_Unity.fbx 的 Fish_* 原始网格与材质,
        // 尺寸与 demo1 一致(远处 36 条 Fish_101..136,近处 Fish_137/138 + 10 条复制),由 ReefFishSchool 做 demo1 的轨道游动。
        var fbxFish = AssetDatabase.LoadAssetAtPath<GameObject>(CollabArtSwap.Fbx).GetComponentsInChildren<MeshRenderer>(true)
            .Where(r => r.name.StartsWith("Fish_")).OrderBy(r => r.name, StringComparer.Ordinal).ToList();
        var schoolSrc = fbxFish.Where(r => int.Parse(r.name.Substring(5)) < 137).ToList();
        var nearSrc = fbxFish.Where(r => int.Parse(r.name.Substring(5)) >= 137).ToList();
        if (schoolSrc.Count == 0 || nearSrc.Count == 0) throw new InvalidOperationException("demo1 fish nodes missing in FBX.");
        var near = DemoFish("NearFish", underwater, nearSrc, nearSrc.Count, 1f).ToList();
        near.AddRange(DemoFish("NearFishExtra", underwater, new[] { nearSrc[0] }, 10, 1f));
        view.nearFish = near.ToArray();
        view.schoolFish = DemoFish("SchoolFish", underwater, schoolSrc, schoolSrc.Count, 1f);
        view.interactFish = DemoFish("InteractFish", underwater, new[] { nearSrc[0] }, 12, .5f);
        // Transition fish remain outside UnderwaterRoot so they can swim in AR before/after the reef.
        // 沙丁鱼风暴:更多、更小的鱼(60 条 × 0.45)
        view.transitionFish = DemoFish("TransitionFish", null, new[] { nearSrc[0] }, 60, .45f);

        // 场景3:附近可供躲藏的珊瑚(优先参与者前方、离得近、够高的珊瑚)+ 一条缓慢巡游的"天敌"
        var hideCorals = corals.GetComponentsInChildren<MeshRenderer>(true)
            .Where(r => r.bounds.size.y >= .6f)
            .Select(r => (r, d: Vector2.Distance(new Vector2(r.bounds.center.x, r.bounds.center.z), new Vector2(heroBounds.center.x, heroBounds.center.z))))
            .Where(x => x.d > 1.2f && x.d < 8f)
            .OrderBy(x => x.d + (x.r.bounds.center.z < heroBounds.center.z - .5f ? 3f : 0f))
            .Take(10).Select(x => x.r.transform).ToArray();
        var predator = DemoFish("Predator - reef cruiser", underwater, new[] { nearSrc[0] }, 1, 2.2f)[0];
        var school = experience.AddComponent<ReefFishSchool>();
        // 原来的探头小鱼(interactFish)成为"身体小鱼":涂耦合剂和天黑时躲进我体内
        school.fish = view.nearFish.Concat(view.schoolFish).Concat(view.interactFish).ToArray();
        school.bodyFishStart = view.nearFish.Length + view.schoolFish.Length;
        school.nearCount = view.nearFish.Length;
        school.heroCoral = heroCoral;
        school.hideCorals = hideCorals;
        school.predator = predator;
        view.fishSchool = school;

        // 场景2:柔软、每根独立运动的海草,围绕参与者根部
        var weedGo = new GameObject("Soft seaweed strands - scene 2", typeof(MeshFilter), typeof(MeshRenderer));
        weedGo.transform.SetParent(underwater, false);
        var weedMat = CloneMaterial("Assets/Materials/SoftSeaweed.mat", new Material(Shader.Find("UnderTheSea/SoftSeaweed")));
        weedMat.shader = Shader.Find("UnderTheSea/SoftSeaweed");
        if (kelpMat && kelpMat.HasProperty("_BaseColor"))
        {
            weedMat.SetColor("_BaseColor", kelpMat.GetColor("_BaseColor") * .8f);
            weedMat.SetColor("_TipColor", kelpMat.GetColor("_TipColor"));
        }
        EditorUtility.SetDirty(weedMat);
        weedGo.GetComponent<MeshRenderer>().sharedMaterial = weedMat;
        var weeds = weedGo.AddComponent<SoftSeaweedStrands>();
        weeds.body = heroCoral; weeds.eye = camera.transform;
        var weedMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        weedMarker.name = "SeaweedTagMarker";
        Object.DestroyImmediate(weedMarker.GetComponent<Collider>());
        weedMarker.transform.SetParent(underwater, false);
        weedMarker.transform.localScale = Vector3.one * .045f;
        weedMarker.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Art + "Simulated probe marker.mat");
        weedMarker.SetActive(false);
        weeds.tagMarker = weedMarker.transform;   // 测试用小黄点:真实 tag 位置,与小丑鱼同时显示(showTagMarker / T 键开关)
        weeds.showTagMarker = true;
        // 2026-11-05:小丑鱼代替 tag 小黄点 —— 像一条鱼在水草和珊瑚间游动、拨动水草
        var clownfish = BuildClownfish(underwater);
        var clownDriver = clownfish.gameObject.AddComponent<TagClownfish>();
        clownDriver.controller = controller;
        clownDriver.seaweed = weeds;
        clownDriver.animator = clownfish.GetComponentInChildren<Animator>();
        weeds.interactor = clownfish;
        view.softSeaweed = weeds;

        var jellyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "MoonJellyfish.prefab");
        var jellyList = new List<Transform> { firstJelly };
        for (int i = 1; i < 3; i++)
        {
            var jelly = (GameObject)PrefabUtility.InstantiatePrefab(jellyPrefab, scene);
            jelly.name = "Moon jellyfish " + i;
            jelly.transform.SetParent(underwater, true);
            jelly.transform.position = firstJelly.position + new Vector3(i == 1 ? -2.1f : .8f, .45f*i, 1.2f*i);
            jelly.transform.localScale = Vector3.one * (i == 1 ? .6f : .5f);
            jelly.SetActive(true); jellyList.Add(jelly.transform);
        }
        // demo1 的远处水母(FBX Jelly_*):同样的水母网格与动画,放在 demo1 的位置,高约 0.87m
        var farJellySpots = CollabArtSwap.FarJellyPositions();
        for (int i = 0; i < farJellySpots.Count; i++)
        {
            var jelly = (GameObject)PrefabUtility.InstantiatePrefab(jellyPrefab, scene);
            jelly.name = "Far jellyfish (demo1) " + i;
            jelly.transform.SetParent(underwater, true);
            jelly.transform.position = Demo1(farJellySpots[i]);
            jelly.SetActive(true); jellyList.Add(jelly.transform);
        }
        int farStart = 3, farEnd = jellyList.Count;
        // 2026-11-05:左前方加一只水母(和近处水母同样大小)
        {
            var jelly = (GameObject)PrefabUtility.InstantiatePrefab(jellyPrefab, scene);
            jelly.name = "Moon jellyfish - front left";
            jelly.transform.SetParent(underwater, true);
            jelly.transform.position = new Vector3(heroBounds.center.x - 2.4f, 2.4f, heroBounds.center.z + 3.6f);
            jelly.transform.localScale = Vector3.one * .55f;
            jelly.SetActive(true); jellyList.Add(jelly.transform);
        }
        view.jellies = jellyList.ToArray();
        CollabArtSwap.ApplyJellies(view.jellies); // 协作版水母网格；JellyfishBreath 的呼吸/显隐参数照常生效
        for (int i = farStart; i < farEnd; i++)
        {
            var jr = view.jellies[i].Find("Collab jellyfish").GetComponent<Renderer>();
            view.jellies[i].localScale *= .87f / Mathf.Max(.01f, jr.bounds.size.y);
        }
        // 2026-11-05:正前方(水平方向与视线夹角最小)的那只水母往下挪 1m,不用抬头太多
        {
            Vector3 eyeFlat = new Vector3(desktopPosition.x, 0f, desktopPosition.z);
            var ahead = view.jellies.OrderBy(j => Vector3.Angle(Vector3.forward, new Vector3(j.position.x, 0f, j.position.z) - eyeFlat)).First();
            ahead.position += Vector3.down * 1f;
            Debug.Log("[ReefMigration] Lowered the jellyfish straight ahead: " + ahead.name + " -> y " + ahead.position.y.ToString("F2"));
        }
        // The source driver owns _Breath/_Visibility property blocks; the legacy emission writer is unused.
        view.jellyRenderers = Array.Empty<Renderer>();
        view.handTentacle = null;   // 场景5 的握手环节已删除(全部水母离开)
        view.sun = RenderSettings.sun;
        if (!view.sun) view.sun = Object.FindFirstObjectByType<Light>();
        view.sunDayIntensity = view.sun.intensity;
        view.waterDay = RenderSettings.fogColor;
        view.waterNight = new Color(.018f, .045f, .095f);
        var bridge = experience.AddComponent<PassthroughBridge>(); bridge.manager = manager; bridge.layer = layer;
        view.passthrough = bridge;
        var artDriver = experience.AddComponent<ReefArtDriver>();
        view.importedArt = artDriver;
        artDriver.gel = null;   // 旧的整片式涂胶海草已移除,场景2 由 SoftSeaweedStrands 负责
        artDriver.gelTagTarget = Anchor("Gel Tag 0 bend target", experience.transform, view.bellyPoint.position);
        artDriver.jellyAnimation = view.jellies.Select(j => j.GetComponent<JellyfishBreath>()).ToArray();
        foreach (var animation in artDriver.jellyAnimation) { animation.handleInput = false; animation.Visibility = 0f; }
        artDriver.volume = Object.FindFirstObjectByType<Volume>();
        artDriver.daySky = RenderSettings.ambientSkyColor;
        artDriver.dayEquator = RenderSettings.ambientEquatorColor;
        artDriver.dayGround = RenderSettings.ambientGroundColor;
        // 场景4 呼吸发光:珊瑚/海葵/参与者自己的尖端微光 + 发光颗粒
        var glow = experience.AddComponent<BreathGlow>();
        glow.controller = controller;
        glow.self = heroCoral;
        glow.glowObjects = hideCorals.Concat(anemones).ToArray();
        glow.visibleRoot = underwater.gameObject;
        glow.viewer = camera.transform;
        glow.jellies = view.jellies;
        glow.art = artDriver;
        var sparkleMat = CloneMaterial("Assets/Materials/BreathSparkle.mat", new Material(Shader.Find("UnderTheSea/BreathSparkle")));
        sparkleMat.shader = Shader.Find("UnderTheSea/BreathSparkle");
        EditorUtility.SetDirty(sparkleMat);
        glow.sparkleMaterial = sparkleMat;

        // 2026-11-05 音效(VR 音效.7z,原文件名保留在 Assets/Audio,按文件名对应时机)
        var sound = experience.AddComponent<ReefSoundscape>();
        sound.controller = controller; sound.view = view;
        sound.bubblesLoop = Audio("背景音 泡泡咕嘟咕嘟（常驻）.mp3", true);
        sound.enterMusic = Audio("进入VR世界后的背景乐 搭配开场词.mp3", true);
        sound.afterIntro = Audio("播放完背景乐和开场词，播放这个.mp3", false);
        sound.sceneEnd = Audio("每结束一个场景  放这个.mp3", false);
        sound.fishToCoral = Audio("小鱼靠近珊瑚.mp3", false);
        sound.breathHoldAmbience = Audio("憋气时背景音.mp3", true);
        sound.breathInOut = Audio("憋气时声音（憋气先播放前半段音乐，松手呼气时播放后半段）.mp3", false);
        sound.exitToAR = Audio("退场开始进入ar世界时播放.mp3", false);
        // 2026-11-05:统一响度(Quest 喇叭偏小)。短音效按录音自动算;流式长音轨用实测值:
        // 泡泡 −37.3 dB / 峰值 .55 → ×2.9;开场背景乐 −18.4 dB / .47 → ×1.3;憋气背景 −31.9 dB / .29 → ×2.5
        sound.afterIntroGain = VoiceGain(sound.afterIntro, -20f);
        sound.sceneEndGain = VoiceGain(sound.sceneEnd, -20f);
        sound.fishToCoralGain = VoiceGain(sound.fishToCoral, -20f);
        sound.exitToARGain = VoiceGain(sound.exitToAR, -20f);
        sound.breathGain = VoiceGain(sound.breathInOut, -20f);
        sound.masterGain = 1.5f;

        // 2026-11-05 旁白配音:按《Voiceover script 确定版》(Assets/Audio/VO/VO_script.txt)匹配阶段与触发。
        // 录音本身偏小(约 −27~−31 dB),每条按响度自动算增益,统一到约 −20 dB。
        var vo = experience.AddComponent<ReefVoiceOver>();
        vo.controller = controller; vo.view = view; vo.soundscape = sound; vo.seaweed = weeds;
        ReefVoiceOver.Cue Cue(string id, Stage stage, ReefVoiceOver.Trigger trigger, float delay, string text, string after = null, float fallback = 0f)
        {
            var clip = Audio("VO/VO_" + id + ".mp3", false);
            return new ReefVoiceOver.Cue { id = id, text = text, clip = clip, stage = stage, trigger = trigger, delay = delay, afterId = after, fallback = fallback, gain = VoiceGain(clip) };
        }
        ReefVoiceOver.Cue Seg(ReefVoiceOver.Cue c, float start, float end) { c.clipStart = start; c.clipEnd = end; return c; }
        vo.cues = new[]
        {
            // 01 Opening:VR 完全显现后(与开场背景乐同时,背景乐被压低)
            Cue("01",   Stage.Enter,            ReefVoiceOver.Trigger.VrFullyRevealed, 1f,
                "Welcome to Ultrasound Wonders: Under the Sea. Make yourself comfortable. Here, you are a coral, resting gently beneath the waves. For now, breathe normally. Follow your clinician's instructions, and let them know if you need a pause."),
            // 02 Gel:涂耦合剂开始(小鱼游进我体内)
            Cue("02",   Stage.ApplyGel,         ReefVoiceOver.Trigger.StageEnter,      1f,
                "A gentle wave passes over you. Some little fish have come to say hello."),
            // 03 Probe contact:探头第一次碰到(tag 进入海草区域);tag 一直没识别到时 3 秒后照常播放
            // 2026-11-05:03.1 与 03.2 是同一段台词的两个录音版本(以前连着播所以重复)→ 用户选定只播 03.2
            // 2026-11-05 晚:换成新录音 VO_03.mp3(台词略改,加了小丑鱼);旧的 03.1 / 03.2 不再使用
            Cue("03",   Stage.Probe,            ReefVoiceOver.Trigger.FirstTagContact, .3f,
                "A little clownfish nestles close as the seagrass gently rests against you. You may feel a little pressure.", null, 3f),
            // 04 Night falls:天黑过渡开始
            Cue("04",   Stage.DuskTransition,   ReefVoiceOver.Trigger.StageEnter,      .5f,
                "Night is falling over the reef, the little fish nestle among your coral branches. Let's help the jellyfish's gentle light grow."),   // 2026-11-05 晚:新录音
            // 05 Breathe in:进入场景4 后 1 秒。2026-11-06 用户要求只保留前半句(去掉 "Watch the jellyfish grow and glow.")
            //   切点由逐词时间得出:"comfortable." 结束于 5.36s,"Watch" 开始于 6.08s → 取 5.72s
            Seg(Cue("05", Stage.BreathHold,     ReefVoiceOver.Trigger.StageEnter,      1f,
                "Take a slow breath in, as deeply as feels comfortable."), 0f, 5.72f),
            // 06 已去掉(2026-11-06):"Gently hold…" "Let's try it again…" "Wonderful…" 都不再播。
            //   改由 BreathGuide:05 播完后按住扳机 → 吸气声 + 1 秒后视觉中心缓现 "Hold";
            //   松开扳机 → "Hold" 缓慢消失 + 配音 "Now you can breathe normally."(VO_06 的 5.49–7.29s)+ 吐气声;每次按/松都重复。
            // 07 Resume normal breathing:按 A 天亮、水母游走
            Cue("07",   Stage.ExhaleTransition, ReefVoiceOver.Trigger.StageEnter,      .5f,
                "You can breathe normally now. The light spreads softly around the little fish."),
            // 08 Closing:退场开始;回到现实后继续播完
            Cue("08",   Stage.Exit,             ReefVoiceOver.Trigger.StageEnter,      .3f,
                "Our underwater journey is coming to an end. Thank you for sharing this little ocean with us. Gently bring your attention back to the room. Keep resting while we help you remove the headset."),
        };

        // 2026-11-06 场景4 呼吸引导:吸气声 / "Hold" 字幕 / 松开扳机的配音 + 吐气声
        var guide = experience.AddComponent<BreathGuide>();
        guide.controller = controller; guide.voiceOver = vo; guide.soundscape = sound; guide.viewer = camera.transform;
        var vo06 = Audio("VO/VO_06.mp3", false);
        guide.releaseClip = vo06; guide.releaseStart = 5.49f; guide.releaseEnd = 7.29f; guide.releaseGain = VoiceGain(vo06);
        var holdMat = CloneMaterial("Assets/Materials/HoldText.mat", new Material(Shader.Find("UnderTheSea/HoldText")));
        holdMat.shader = Shader.Find("UnderTheSea/HoldText");
        holdMat.mainTexture = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf").material.mainTexture;
        EditorUtility.SetDirty(holdMat);
        guide.textMaterial = holdMat;

        var desktopLook = experience.AddComponent<ReefDesktopCamera>();
        desktopLook.cameraTransform = camera.transform; desktopLook.controller = controller;
        desktopLook.homeRotation = desktopRotation;
        var start = experience.AddComponent<StartButton>();
        start.controller = controller; start.view = view; start.cam = camera;
        start.buttonMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/StartButton.mat");

        RenderSettings.ambientMode = AmbientMode.Trilight;
        Shader.SetGlobalFloat("_ReefNightBlend", 0f);
        Shader.SetGlobalFloat("_ReefMotionActive", 0f);
        EditorSceneManager.SaveScene(scene, Target);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Target, true) };
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Target, OpenSceneMode.Single);
        ReefMigrationChecks.ValidateSavedScene();
        Debug.Log("[ReefMigration] Main saved with imported art, original six-stage flow, Quest rig and AR transitions.");
    }

    static void ConfigurePipeline()
    {
        Directory.CreateDirectory(Settings); AssetDatabase.Refresh();
        string rendererPath = Settings + "Quest_Renderer.asset";
        string pipelinePath = Settings + "Quest_URP.asset";
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if (!renderer)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            AssetDatabase.CreateAsset(renderer, rendererPath);
            ResourceReloader.ReloadAllNullIn(renderer, "Packages/com.unity.render-pipelines.universal");
        }
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        if (!pipeline) { pipeline = UniversalRenderPipelineAsset.Create(renderer); AssetDatabase.CreateAsset(pipeline, pipelinePath); }
        pipeline.msaaSampleCount = 4; pipeline.renderScale = 1f; pipeline.supportsHDR = false;   // 关闭 HDR:HDR 缓冲 B10G11R11 没有 alpha,Passthrough 上会蒙一层半透明蓝色
        pipeline.supportsCameraDepthTexture = false; pipeline.supportsCameraOpaqueTexture = false;
        // URP 17.3 exposes this property with an internal setter.
        var serializedPipeline = new SerializedObject(pipeline);
        var mainShadows = serializedPipeline.FindProperty("m_MainLightShadowsSupported");
        if (mainShadows == null) throw new InvalidOperationException("URP main-light shadow field is unavailable.");
        mainShadows.boolValue = false;
        serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
        RepairGlobalSettings();
        GraphicsSettings.defaultRenderPipeline = pipeline;
        int quality = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = pipeline;
        }
        QualitySettings.SetQualityLevel(quality, false);
        EditorUtility.SetDirty(renderer); EditorUtility.SetDirty(pipeline); AssetDatabase.SaveAssets();
    }

    [MenuItem("UnderTheSea/Migration/Repair Unity 6.3 Global Settings")]
    public static void RepairGlobalSettings()
    {
        string path = Settings + "Quest_GlobalSettings.asset";
        var global = AssetDatabase.LoadAssetAtPath<RenderPipelineGlobalSettings>(path);
        if (!global)
        {
            var type = typeof(UniversalRenderPipelineAsset).Assembly.GetType(
                "UnityEngine.Rendering.Universal.UniversalRenderPipelineGlobalSettings", true);
            global = RenderPipelineGlobalSettingsUtils.Create(type, path);
        }
        if (UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(global))
            throw new InvalidOperationException("Fresh Unity 6.3 global settings contain missing resource types.");
        // The source 6.6 global asset carries resources introduced after 6.3. Rebuild its settings
        // through Unity's API while retaining its GUID. The exact 6.6 original remains in Migration~.
        var source = AssetDatabase.LoadAssetAtPath<RenderPipelineGlobalSettings>(
            "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset");
        if (source && UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(source))
        {
            UnityEditor.SerializationUtility.ClearAllManagedReferencesWithMissingTypes(source);
            EditorUtility.CopySerialized(global, source);
            source.name = "UniversalRenderPipelineGlobalSettings";
            EditorUtility.SetDirty(source);
        }
        UnityEditor.Rendering.EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>(global);
        AssetDatabase.SaveAssets();
    }

    static void ConvertLegacyMaterials()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" }))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (!mat || !mat.shader || mat.shader.name != "Standard") continue;
            // Snapshot all source properties before changing the shader; existing GUIDs are preserved.
            Color color = mat.GetColor("_Color"), emission = mat.GetColor("_EmissionColor");
            Texture tex = mat.GetTexture("_MainTex"), normal = mat.GetTexture("_BumpMap");
            Texture metal = mat.GetTexture("_MetallicGlossMap"), emissionMap = mat.GetTexture("_EmissionMap");
            Vector2 scale = mat.GetTextureScale("_MainTex"), offset = mat.GetTextureOffset("_MainTex");
            float metallic = mat.GetFloat("_Metallic"), smooth = mat.GetFloat("_Glossiness");
            if (mat.GetFloat("_Mode") != 0f) throw new InvalidOperationException("Unexpected non-opaque legacy material: " + mat.name);
            mat.shader = Shader.Find("Universal Render Pipeline/Lit");
            mat.SetColor("_BaseColor", color); mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale("_BaseMap", scale); mat.SetTextureOffset("_BaseMap", offset);
            mat.SetTexture("_BumpMap", normal); mat.SetTexture("_MetallicGlossMap", metal);
            mat.SetFloat("_Metallic", metallic); mat.SetFloat("_Smoothness", smooth);
            mat.SetTexture("_EmissionMap", emissionMap); mat.SetColor("_EmissionColor", emission);
            if (normal) mat.EnableKeyword("_NORMALMAP");
            if (emissionMap || emission.maxColorComponent > 0f) mat.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(mat);
        }
    }

    static Transform FindTransform(UnityEngine.SceneManagement.Scene scene, string name) =>
        scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == name);
    static IEnumerable<Transform> Children(Transform parent) => parent ? parent.Cast<Transform>() : Enumerable.Empty<Transform>();
    static Transform Anchor(string name, Transform parent, Vector3 position)
    {
        var t = new GameObject(name).transform; t.SetParent(parent, false); t.position = position; return t;
    }
    /// <summary>
    /// demo1 的鱼:复制 FBX 里的 Fish_* 节点(原网格、原材质、原尺寸 × bodyScale),套一层无缩放的外壳。
    /// 外壳的 +Z = 鱼头方向(按"眼睛"子网格判断),游动代码只需让外壳 LookRotation(速度方向)。
    /// </summary>
    static Transform[] DemoFish(string label, Transform parent, IList<MeshRenderer> sources, int count, float bodyScale)
    {
        var group = Anchor(label, parent, Vector3.zero);
        var fish = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            var src = sources[i % sources.Count];
            var shell = Anchor(label + i, group, new Vector3(-2f, 1f, 2f));
            var body = Object.Instantiate(src.gameObject);
            body.name = src.name;
            body.transform.SetParent(shell, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = src.transform.localRotation;   // FBX 根节点为单位变换
            body.transform.localScale = src.transform.localScale * bodyScale;
            var renderer = body.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            HeadToForward(body.transform, shell);
            body.transform.position += shell.position - renderer.bounds.center;   // 网格中心对准外壳原点
            fish[i] = shell;
        }
        return fish;
    }

    static void HeadToForward(Transform body, Transform shell)
    {
        var mesh = body.GetComponent<MeshFilter>().sharedMesh;
        var mats = body.GetComponent<MeshRenderer>().sharedMaterials;
        if (!mesh || !mesh.isReadable) throw new InvalidOperationException("Fish mesh is not readable: " + body.name);
        int eyes = Array.FindIndex(mats, m => m && m.name.IndexOf("Eye", StringComparison.OrdinalIgnoreCase) >= 0);
        var verts = mesh.vertices;
        Vector3 all = Vector3.zero; foreach (var v in verts) all += v; all /= verts.Length;
        Vector3 head;
        if (eyes >= 0 && eyes < mesh.subMeshCount)
        {
            var idx = mesh.GetIndices(eyes);
            Vector3 eye = Vector3.zero; foreach (int k in idx) eye += verts[k]; eye /= Mathf.Max(1, idx.Length);
            head = eye - all;
        }
        else
        {
            var b = mesh.bounds; head = b.size.x >= b.size.z ? Vector3.right : Vector3.forward;
        }
        Vector3 local = shell.InverseTransformDirection(body.TransformDirection(head));
        local.y = 0f;
        if (local.sqrMagnitude < 1e-8f) return;
        float yaw = Vector3.SignedAngle(local, Vector3.forward, Vector3.up);
        body.rotation = Quaternion.AngleAxis(yaw, shell.up) * body.rotation;
    }
    static Transform[] Tentacle(string name, Transform parent, Camera camera)
    {
        var group = Anchor(name, parent, Vector3.zero);
        var points = new Transform[10];
        for (int i = 0; i < points.Length; i++) { points[i] = Anchor(name+i, group, Vector3.zero); points[i].gameObject.SetActive(false); }
        var ribbon = new GameObject(name + " ribbon", typeof(MeshFilter), typeof(MeshRenderer));
        ribbon.transform.SetParent(parent, false);
        ribbon.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Art + "Jelly soft ribbons.mat");
        var driver = ribbon.AddComponent<ReefHandRibbon>(); driver.points = points; driver.viewer = camera;
        return points;
    }
    /// <summary>
    /// 小丑鱼素材(Assets/Models/Clownfish,来自 Clownfish_Unity_Package):Generic 骨骼,导入的动画改名 Swim_Loop 并循环,
    /// 建一个只有 Swim_Loop 默认状态的 Animator Controller。外壳 +Z = 鱼头(模型鱼头朝 +X),总长约 0.22m。
    /// </summary>
    static Transform BuildClownfish(Transform parent)
    {
        const string dir = "Assets/Models/Clownfish/";
        const string fbxPath = dir + "Clownfish_Unity.fbx";
        var importer = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
        if (!importer) throw new InvalidOperationException("Clownfish FBX missing: " + fbxPath);
        var clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
        bool dirty = importer.animationType != ModelImporterAnimationType.Generic || !importer.importAnimation;
        foreach (var c in clips)
        {
            if (c.name != "Swim_Loop" || !c.loopTime) dirty = true;
            c.name = "Swim_Loop"; c.loopTime = true;
        }
        if (dirty)
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
        var swim = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>().First(c => c.name == "Swim_Loop");
        string controllerPath = dir + "Clownfish.controller";
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
        if (!controller)
        {
            controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath, swim);
        }
        else
        {
            var state = controller.layers[0].stateMachine.defaultState;
            if (state) state.motion = swim;
        }

        var shell = Anchor("Clownfish - probe tag (stirs the seaweed)", parent, Vector3.zero);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath));
        model.transform.SetParent(shell, false);
        model.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);   // 鱼头 +X → 外壳 +Z
        var animator = model.GetComponent<Animator>();
        if (!animator) animator = model.AddComponent<Animator>();   // Unity 对象不能用 ?? 判空
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        foreach (var r in model.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
        }
        shell.localScale = Vector3.one * .55f;   // 0.404m → 约 0.22m
        return shell;
    }

    /// <summary>载入 Assets/Audio 下的音效并设置导入方式:长的背景/音乐用流式 Vorbis,短音效载入时解压。</summary>
    static AudioClip Audio(string file, bool longTrack)
    {
        string path = "Assets/Audio/" + file;
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (!importer) throw new InvalidOperationException("Missing sound: " + path);
        var settings = importer.defaultSampleSettings;
        var load = longTrack ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        bool preload = !longTrack;   // 短音效/配音预载,第一次播放不卡顿
        if (settings.loadType != load || settings.compressionFormat != AudioCompressionFormat.Vorbis || settings.preloadAudioData != preload)
        {
            settings.loadType = load; settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = .7f;
            settings.preloadAudioData = preload;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    /// <summary>配音增益:把录音的 RMS 拉到约 −20 dB,且峰值不超过 1(AudioGain 另有软限幅)。</summary>
    static float VoiceGain(AudioClip clip, float targetDb = -20f)
    {
        if (!clip || !clip.LoadAudioData()) return 1f;
        var data = new float[clip.samples * clip.channels];
        if (!clip.GetData(data, 0)) return 1f;
        double sum = 0; float peak = 0f;
        foreach (var v in data) { sum += v * v; peak = Mathf.Max(peak, Mathf.Abs(v)); }
        double rmsDb = 20 * Math.Log10(Math.Sqrt(sum / Math.Max(1, data.Length)) + 1e-9);
        float gain = (float)Math.Pow(10, (targetDb - rmsDb) / 20);
        gain = Mathf.Min(gain, 1.2f / Mathf.Max(peak, 1e-3f), 6f);   // 峰值最多 1.2,超出部分由 AudioGain 软限幅
        return Mathf.Round(Mathf.Max(1f, gain) * 100f) / 100f;
    }

    /// <summary>从上往下打到沙地/岩石上的高度(临时加 MeshCollider 做射线检测,用完删除)。</summary>
    static float GroundHeight(Transform terrain, Vector3 p)
    {
        float best = float.NegativeInfinity;
        var ray = new Ray(new Vector3(p.x, 50f, p.z), Vector3.down);
        foreach (var mf in terrain.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!mf.sharedMesh) continue;
            var col = mf.gameObject.AddComponent<MeshCollider>();
            col.sharedMesh = mf.sharedMesh;
            if (col.Raycast(ray, out var hit, 100f)) best = Mathf.Max(best, hit.point.y);
            Object.DestroyImmediate(col);
        }
        return float.IsNegativeInfinity(best) ? p.y : best;
    }

    static Material CloneMaterial(string path, Material source)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat) { mat = new Material(source); AssetDatabase.CreateAsset(mat, path); }
        return mat;
    }
}
