using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnderTheSea;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;
using Stage = UnderTheSea.Stage;

[InitializeOnLoad]
public static class ReefMigrationChecks
{
    const string ReportDir = "Documentation/Migration-2026-11-05/";
    const string SmokeKey = "UnderTheSea.ReefSmokeCheck";
    static readonly List<string> failures = new List<string>();
    static readonly List<string> observations = new List<string>();
    static OceanStageView view;
    static TagPoseSource originalTags;
    static ReefVerificationTagSource syntheticTags;
    static int step, startFrame;
    static float deadline;
    static double wallDeadline;
    static bool batch;
    static bool previousRunInBackground;
    static bool runtimeArmed;

    [Serializable] public class Report
    {
        public string unityVersion, pipeline, scene;
        public string[] failures, observations;
        public bool passed;
        public int frameCount;
    }

    static ReefMigrationChecks()
    {
        EditorApplication.playModeStateChanged += change => {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(SmokeKey, false))
                BeginRuntime();
        };
    }

    [MenuItem("UnderTheSea/Migration/Validate Saved Main")]
    public static void ValidateSavedScene()
    {
        var errors = new List<string>();
        var notes = new List<string>();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
        var pipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (!pipeline) errors.Add("Graphics is not assigned to URP.");
        for (int i = 0; i < QualitySettings.names.Length; i++)
            if (QualitySettings.GetRenderPipelineAssetAt(i) != pipeline)
                errors.Add("Quality level has a different pipeline: " + QualitySettings.names[i]);
        if (pipeline)
            foreach (var renderer in pipeline.rendererDataList)
                if (!(renderer is UniversalRendererData ur) || !ur.postProcessData)
                    errors.Add("URP renderer or PostProcessData missing.");
        var global = GraphicsSettings.GetSettingsForRenderPipeline<UniversalRenderPipeline>();
        if (!global || UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(global))
            errors.Add("Active URP global settings contain missing resource types.");
        foreach (var go in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)))
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go.gameObject) != 0)
                errors.Add("Missing script on " + go.name);
        var stageView = Object.FindFirstObjectByType<OceanStageView>();
        if (!stageView || !stageView.importedArt) errors.Add("Main is not using imported artwork.");
        else
        {
            if (!stageView.controller || !stageView.cam || !stageView.tagSource || !stageView.passthrough)
                errors.Add("Required experience/XR/tracking reference missing.");
            if (stageView.jellies.Length != 7 || stageView.transitionFish.Length != 60 || stageView.interactFish.Length != 12)
                errors.Add("Unexpected animated object counts.");
            if (!stageView.softSeaweed || !stageView.softSeaweed.body)
                errors.Add("Scene 2 soft seaweed strands are not connected.");
            if (!stageView.fishSchool || stageView.fishSchool.fish.Length != stageView.nearFish.Length + stageView.schoolFish.Length + stageView.interactFish.Length
                || stageView.fishSchool.hideCorals.Length == 0)
                errors.Add("demo1 fish school / coral hiding is not connected.");
            foreach (var fish in stageView.nearFish.Concat(stageView.schoolFish).Concat(stageView.interactFish).Concat(stageView.transitionFish))
            {
                var mf = fish.GetComponentInChildren<MeshFilter>(true);
                if (!mf || AssetDatabase.GetAssetPath(mf.sharedMesh) != CollabArtSwap.Fbx || !mf.sharedMesh.name.StartsWith("Fish_"))
                { errors.Add("A fish is not the demo1 FBX fish: " + fish.name); break; }
            }
            if (scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Any(t => t.name.IndexOf("Mirror", StringComparison.OrdinalIgnoreCase) >= 0))
                errors.Add("Mirror coral objects are still in Main.");
            var heroBounds = stageView.heroCoral.GetComponent<Renderer>().bounds;
            Vector3 eye = stageView.cam.transform.position;
            if (eye.y <= heroBounds.max.y || new Vector2(eye.x - heroBounds.center.x, eye.z - heroBounds.center.z).magnitude > 1f)
                errors.Add("Camera is not placed on the participant coral.");
            notes.Add("Main uses the original ExperienceController, keyboard and right-controller input.");
            notes.Add("Camera sits on the participant coral; the mirror coral is removed.");
            notes.Add("demo1 FBX fish, soft seaweed strands, four-part moon jellies, shafts, caustics and motes are connected.");
        }
        var volume = Object.FindFirstObjectByType<Volume>();
        if (!volume || !volume.sharedProfile || volume.sharedProfile.components.Any(c => !c))
            errors.Add("Volume/profile has missing persisted components.");
        else if (!volume.sharedProfile.TryGet<Bloom>(out _) || !volume.sharedProfile.TryGet<ColorAdjustments>(out _))
            errors.Add("Source bloom/color adjustments missing.");
        foreach (string guid in AssetDatabase.FindAssets("t:Shader", new[] { "Assets/UltrasoundReef" }))
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));
            if (ShaderUtil.ShaderHasError(shader)) errors.Add("Shader error: " + shader.name);
        }
        if (!EditorBuildSettings.scenes.Any(s => s.path == "Assets/Scenes/Main.unity" && s.enabled))
            errors.Add("Main is not enabled in build settings.");
        Write("editor-validation.json", errors, notes);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
    }

    [MenuItem("UnderTheSea/Migration/Run Complete Flow Smoke Check")]
    public static void RunFlow()
    {
        ValidateSavedScene();
        SessionState.SetBool(SmokeKey, true);
        SessionState.SetBool(SmokeKey + ".batch", Application.isBatchMode);
        EditorApplication.isPlaying = true;
    }

    static void BeginRuntime()
    {
        failures.Clear(); observations.Clear(); step = 0; startFrame = Time.frameCount;
        runtimeArmed = false;
        batch = SessionState.GetBool(SmokeKey + ".batch", false);
        view = Object.FindFirstObjectByType<OceanStageView>();
        previousRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        originalTags = view.tagSource;
        syntheticTags = view.gameObject.AddComponent<ReefVerificationTagSource>();
        syntheticTags.tick = Tick;
        view.tagSource = syntheticTags;
        Application.logMessageReceived += ObserveLog;
        EditorApplication.update += Watchdog;
        deadline = Time.time + 1f;
        wallDeadline = EditorApplication.timeSinceStartup + 180;
    }

    static void ObserveLog(string condition, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            failures.Add(type + ": " + condition);
    }

    static void Expect(bool condition, string description)
    {
        if (!condition) failures.Add(description);
        else observations.Add(description);
    }

    static bool ProbeTracked() => (bool)typeof(OceanStageView)
        .GetField("_probeTracked", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);

    static void Go(Stage stage, float seconds)
    {
        view.controller.GoTo(stage); deadline = Time.time + seconds;
    }

    static void Capture(string name)
    {
        Directory.CreateDirectory(ReportDir + "Screenshots");
        ScreenCapture.CaptureScreenshot(ReportDir + "Screenshots/" + name + ".png");
    }

    static void Tick()
    {
        if (!runtimeArmed)
        {
            if (Time.frameCount < 3) return;
            runtimeArmed = true; startFrame = Time.frameCount;
            deadline = Time.time + 1f;
            return;
        }
        if (Time.time < deadline) return;
        switch (step++)
        {
            case 0:
                Expect(view.VrAmount < .01f && !view.underwaterRoot.activeSelf, "Standby hides the reef.");
                Go(Stage.Enter, 11.5f); break;
            case 1:
                Expect(view.VrAmount > .99f && view.underwaterRoot.activeSelf, "Enter reveals the full reef.");
                Capture("01-enter"); syntheticTags.valid = true;
                syntheticTags.position = view.bellyPoint.position + new Vector3(.2f, .05f, .1f);
                Go(Stage.ApplyGel, 8f); break;
            case 2:
                Expect(!view.softSeaweed.TagTracked, "ApplyGel has no tag tracking.");
                Expect(view.fishSchool.FishOnMe >= 8, "During ApplyGel small fish swim into the participant coral and hide.");
                Capture("02-gel"); Go(Stage.Probe, 5f); break;
            case 3:
                Expect(view.fishSchool.FishOnMe == 0, "Probe: the fish leave the participant coral and return to normal.");
                Expect(view.softSeaweed.TagTracked && view.softSeaweed.PressDepth > .5f, "Probe: seaweed follows the probe Tag 1 with a press.");
                Expect(view.softSeaweed.ContactRatio > .1f, "Probe: seaweed strands touch the participant coral.");
                Capture("03-probe"); syntheticTags.valid = false; deadline = Time.time + .7f; break;
            case 4:
                Expect(!view.softSeaweed.TagTracked, "Lost probe tag returns the seaweed to the default wave.");
                Go(Stage.DuskTransition, 7.5f); break;
            case 5:
            {
                var art = view.importedArt;
                var glow = Object.FindFirstObjectByType<BreathGlow>();
                Expect(view.controller.Current == Stage.BreathHold, "Dusk completes the jellyfish arrival before BreathHold.");
                float night = Shader.GetGlobalFloat("_ReefNightBlend");
                Expect(night > .4f && night < .6f, "Night is only 50% of the daylight darkness.");
                Expect(view.jellies.Length == 7 && Enumerable.Range(0, view.jellies.Length).All(i => art.JellyVisibility(i) > .9f),
                    "Four near (incl. front-left) and three demo1 far jellyfish surround the participant.");
                Expect(art.jellyAnimation.All(j => j.SmoothedBreath < .05f), "Jellyfish do not glow after the dusk transition.");
                Expect(view.fishSchool.FishOnMe >= 8, "At night the fish hide back inside the participant coral.");
                Expect(view.fishSchool.FishInGaps > view.fishSchool.FishOnMe, "Other reef fish hide in nearby corals at night.");
                Expect(glow && glow.NightParticles > 40, "Night: slowly twinkling particles appear across the reef.");
                Capture("04-night-arrival");
                view.controller.scriptBreathHeld = true; deadline = Time.time + 3.6f; break;
            }
            case 6:
            {
                var art = view.importedArt;
                var glow = Object.FindFirstObjectByType<BreathGlow>();
                Expect(view.controller.Breath > .99f, "Holding the trigger inhales to full within 3 s.");
                Expect(view.HeroGrowth > 1.3f, "The participant coral grows to 1.35x while inhaling.");
                Expect(view.jellies[0].localScale.x / art.JellyBaseScale(0).x > 1.18f, "Jellyfish grow toward 130% while inhaling.");
                Expect(art.jellyAnimation.All(j => j.SmoothedBreath > .85f), "Jellyfish glow while inhaling.");
                Expect(glow && glow.GlowLevel > .9f && glow.LiveParticles > 20, "Coral, anemone and participant tips glow with sparkles.");
                Expect(view.softSeaweed.WrapAmount > .9f && view.softSeaweed.ContactRatio > .2f, "Seaweed wraps around the participant while inhaling.");
                Capture("05-breath-hold");
                view.controller.scriptBreathHeld = false; deadline = Time.time + 3.6f; break;
            }
            case 7:
                Expect(view.controller.Breath < .01f && view.HeroGrowth < 1.05f
                    && view.jellies[0].localScale.x / view.importedArt.JellyBaseScale(0).x < 1.1f,
                    "Releasing the trigger exhales back to normal within 3 s.");
                Go(Stage.ExhaleTransition, 16.5f); break;
            case 8:
                Expect(view.controller.Current == Stage.ExhaleTransition, "Scenes 4+5 merged: dawn waits for the assistant (no Finale stage).");
                Expect(Enumerable.Range(0, view.jellies.Length).All(i => view.importedArt.JellyVisibility(i) < .01f),
                    "At dawn every jellyfish swims away; none stays behind.");
                Capture("06-dawn"); Go(Stage.Exit, 3f); break;
            case 9:
                Expect(view.TransitionFishVisible > 30, "Exit gathers the sardine-storm fish by swimming in.");
                Capture("07-exit-gather"); deadline = Time.time + 13.5f; break;
            case 10:
                Expect(view.controller.Current == Stage.Standby && view.VrAmount < .01f,
                    "Exit returns through Finished to Standby automatically.");
                Go(Stage.Probe, 1f); break;
            case 11:
                view.controller.ResetToStandby(); deadline = Time.time + 2f; break;
            default:
                Expect(view.controller.Current == Stage.Standby && view.VrAmount < .01f,
                    "Reset returns an active experience to the AR Start state.");
                Expect(Time.frameCount > startFrame + 60, "The runtime advanced beyond the initial frame.");
                Finish(); break;
        }
    }

    static void Finish()
    {
        EditorApplication.update -= Watchdog;
        Application.logMessageReceived -= ObserveLog;
        if (view) view.tagSource = originalTags;
        if (syntheticTags) { syntheticTags.tick = null; Object.Destroy(syntheticTags); }
        SessionState.SetBool(SmokeKey, false);
        Write("flow-validation.json", failures, observations);
        Application.runInBackground = previousRunInBackground;
        EditorApplication.isPlaying = false;
        if (batch) EditorApplication.delayCall += () => EditorApplication.Exit(failures.Count == 0 ? 0 : 1);
    }

    static void Watchdog()
    {
        if (EditorApplication.timeSinceStartup <= wallDeadline &&
            (EditorApplication.isPlayingOrWillChangePlaymode || !runtimeArmed)) return;
        failures.Add(EditorApplication.isPlayingOrWillChangePlaymode
            ? "Runtime check timed out; the player loop may not be advancing." : "Play check was interrupted.");
        Finish();
    }

    static void Write(string file, List<string> errors, List<string> notes)
    {
        Directory.CreateDirectory(ReportDir);
        var report = new Report { unityVersion = Application.unityVersion,
            pipeline = GraphicsSettings.defaultRenderPipeline ? GraphicsSettings.defaultRenderPipeline.name : "Built-in",
            scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
            failures = errors.ToArray(), observations = notes.ToArray(), passed = errors.Count == 0,
            frameCount = Time.frameCount };
        File.WriteAllText(ReportDir + file, JsonUtility.ToJson(report, true));
        Debug.Log("[ReefMigration] " + file + ": " + (report.passed ? "passed" : "failed"));
    }
}
