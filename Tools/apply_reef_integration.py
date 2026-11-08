"""Apply the runtime bridge after importing the original artwork."""
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[1]


def replace_once(text, old, new):
    if text.count(old) != 1:
        raise RuntimeError(f"Expected one source match, got {text.count(old)}: {old[:100]}")
    return text.replace(old, new, 1)


def main():
    staging = ROOT / "Tools/ReefIntegration~"
    for source in staging.glob("*.cs"):
        folder = "Assets/Editor" if source.name in ("BuildReefMainScene.cs", "ReefMigrationChecks.cs") else "Assets/Scripts"
        shutil.copy2(source, ROOT / folder / source.name)

    path = ROOT / "Assets/Scripts/OceanStageView.cs"
    text = path.read_text(encoding="utf-8-sig")
    text = text.replace(", _schoolAway;", ";")
    text = replace_once(text, '        [Header("Refs")]',
        '        [Header("Imported reef artwork")]\n        public ReefArtDriver importedArt;\n        public float sunDayIntensity = 1f;\n\n        [Header("Refs")]')
    text = replace_once(text, '        Vector3 _heroBase, _mirrorBase;',
        '        Vector3 _heroBase, _mirrorBase;\n        Vector3[] _interactScale;')
    text = replace_once(text, '            _jellyHome = new Vector3[jellies.Length];',
        '            if (importedArt != null) importedArt.Initialize(this);\n\n            _jellyHome = new Vector3[jellies.Length];')
    text = replace_once(text,
        '            for (int i = 0; i < interactFish.Length; i++) _interactPos[i] = interactFish[i].position;',
        '            _interactScale = new Vector3[interactFish.Length];\n            for (int i = 0; i < interactFish.Length; i++)\n            {\n                _interactPos[i] = interactFish[i].position;\n                _interactScale[i] = interactFish[i].localScale;\n            }')
    text = replace_once(text, '            controller.StageChanged += (_, __) => ApplyVisibility();',
        '            controller.StageChanged += HandleStageChanged;')
    text = replace_once(text, '        void ApplyVisibility()',
        '        void HandleStageChanged(Stage previous, Stage next) => ApplyVisibility();\n\n        void OnDestroy()\n        {\n            if (controller != null) controller.StageChanged -= HandleStageChanged;\n        }\n\n        void ApplyVisibility()')
    text = replace_once(text,
        '        void UpdateSeaweed(Stage s, float dt, float t)\n        {',
        '        void UpdateSeaweed(Stage s, float dt, float t)\n        {\n            if (importedArt != null) { importedArt.UpdateGel(s); return; }')
    text = replace_once(text,
        '        void UpdateJellies(Stage s, float dt, float t)\n        {',
        '        void UpdateJellies(Stage s, float dt, float t)\n        {\n            if (importedArt != null) { importedArt.UpdateJellies(s, dt, t, _breath); return; }')
    text = replace_once(text, '            _dark = Mathf.MoveTowards(_dark, darkTarget, dt * 0.3f);',
        '            _dark = Mathf.MoveTowards(_dark, darkTarget, dt * (importedArt != null ? 1f / 4.5f : 0.3f));')
    text = replace_once(text, '            if (sun != null) sun.intensity = Mathf.Lerp(1f, 0.08f, _dark);',
        '            if (sun != null) sun.intensity = Mathf.Lerp(sunDayIntensity, 0.16f, _dark);')
    text = replace_once(text, '            RenderSettings.fogColor = water;',
        '            RenderSettings.fogColor = water;\n            if (importedArt != null) importedArt.ApplyLighting(_dark);')
    text = replace_once(text,
        '            for (int i = 0; i < schoolFish.Length; i++)\n                Orbit(schoolFish[i], i, t, 14f, 8f, 1.5f, 4f, 0.15f, off);',
        '            if (importedArt != null) importedArt.UpdateBackgroundFish(schoolFish, dt, t);\n            else for (int i = 0; i < schoolFish.Length; i++)\n                Orbit(schoolFish[i], i, t, 14f, 8f, 1.5f, 4f, 0.15f, off);')
    text = replace_once(text,
        '                float sc = Mathf.Lerp(f.localScale.x, scaleTarget * Reveal(0.3f), 1f - Mathf.Exp(-dt * 3f));\n                f.localScale = Vector3.one * sc;',
        '                float currentScale = f.localScale.x / Mathf.Max(.001f, _interactScale[i].x);\n                float sc = Mathf.Lerp(currentScale, scaleTarget * Reveal(0.3f), 1f - Mathf.Exp(-dt * 3f));\n                f.localScale = _interactScale[i] * sc;')
    path.write_text(text, encoding="utf-8")

    breath = ROOT / "Assets/UltrasoundReef/Runtime/JellyfishBreath.cs"
    text = breath.read_text(encoding="utf-8-sig")
    text = replace_once(text, '  public bool paused;', '  public bool paused;\n  public bool handleInput=true;')
    text = replace_once(text, '   if(k!=null){', '   if(handleInput && k!=null){')
    breath.write_text(text, encoding="utf-8")

    # Keep the original builder available without allowing it to overwrite the migrated Main.
    legacy = ROOT / "Assets/Editor/BuildStep1Scene.cs"
    text = legacy.read_text(encoding="utf-8-sig")
    text = replace_once(text, 'const string ScenePath = "Assets/Scenes/Main.unity";',
        'const string ScenePath = "Assets/Scenes/LegacyStep1Preview.unity";')
    text = replace_once(text, '[MenuItem("UnderTheSea/Build Step1 Scene")]',
        '[MenuItem("UnderTheSea/Legacy/Build Placeholder Preview")]')
    text = replace_once(text, '        if (existing != null) AssetDatabase.DeleteAsset(path);\n        var m = new Material(Shader.Find("Standard")) { color = c };',
        '        var shader = Shader.Find(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null\n            ? "Universal Render Pipeline/Lit" : "Standard");\n        var m = existing != null ? existing : new Material(shader);\n        m.color = c;')
    text = replace_once(text, '        AssetDatabase.CreateAsset(m, path);',
        '        if (existing == null) AssetDatabase.CreateAsset(m, path);\n        else EditorUtility.SetDirty(m);')
    text = text.replace('会覆盖 Main.unity', '保存为独立的 LegacyStep1Preview.unity，不覆盖主场景')
    legacy.write_text(text, encoding="utf-8")

    mock = ROOT / "Assets/Scripts/MockTagPoseSource.cs"
    text = mock.read_text(encoding="utf-8-sig")
    text = replace_once(text, '            if (!Input.GetMouseButton(button)) return false;',
        '            if (!Input.GetMouseButton(button) || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) return false;')
    mock.write_text(text, encoding="utf-8")
    hud = ROOT / "Assets/Scripts/StageHud.cs"
    text = hud.read_text(encoding="utf-8-sig").replace('水草跟随(松开则自动抚摸)', '拨开水草(松开则自动抚摸)')
    text = text.replace('回到Start", _small);', '回到Start   [Alt+左键拖动] 转头 [Home] 复位", _small);')
    hud.write_text(text, encoding="utf-8")
    print("Applied artwork adapters, source input isolation, preserved fish scale and legacy-builder protection.")


if __name__ == "__main__":
    main()
