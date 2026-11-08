"""Import the staged artwork after ReefPackageInstaller has resolved dependencies."""
from pathlib import Path
import hashlib
import json
import re
import shutil

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "Migration~" / "2026-11-05"
SOURCE = WORK / "SourceProject"


def main():
    packages = json.loads((ROOT / "Packages/manifest.json").read_text())["dependencies"]
    if packages.get("com.unity.render-pipelines.universal") != "17.3.0":
        raise SystemExit("Run ReefPackageInstaller.Install in Unity 6000.3 first.")
    copied, retained, adapted = [], [], []
    for source in sorted((SOURCE / "Assets").rglob("*")):
        if not source.is_file():
            continue
        relative = source.relative_to(SOURCE)
        destination = ROOT / relative
        # The destination's existing Scenes directory identity must remain stable.
        if relative.as_posix() == "Assets/Scenes.meta":
            retained.append(relative.as_posix())
            continue
        if destination.exists() and destination.read_bytes() != source.read_bytes():
            raise SystemExit(f"Unexpected file collision: {relative}")
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, destination)
        copied.append(relative.as_posix())
    # 6.6 introduced convenience overloads that are not available in 6.3.
    for script in (ROOT / "Assets/UltrasoundReef/Editor").glob("*.cs"):
        original = script.read_text(encoding="utf-8-sig")
        fixed = re.sub(r"FindObjectsByType<([^>]+)>\(FindObjectsInactive.Include\)",
                       r"FindObjectsByType<\1>(FindObjectsInactive.Include, FindObjectsSortMode.None)", original)
        fixed = re.sub(r"FindObjectsByType<([^>]+)>\(\)",
                       r"FindObjectsByType<\1>(FindObjectsSortMode.None)", fixed)
        if fixed != original:
            script.write_text(fixed, encoding="utf-8")
            adapted.append(script.relative_to(ROOT).as_posix())
    readme = ROOT / "Assets/TutorialInfo/Editor/ReadmeEditor.cs"
    text = readme.read_text(encoding="utf-8-sig")
    text = text.replace("using Unity.Scripting.LifecycleManagement;\n", "")
    text = text.replace("[OnCodeLoaded]", "[InitializeOnLoadMethod]")
    text = text.replace('AssetDatabase.FindAssetGUIDs("Readme t:Readme")', 'AssetDatabase.FindAssets("Readme t:Readme")')
    text = text.replace("AssetDatabase.LoadAssetByGUID<Readme>(ids[0])", "AssetDatabase.LoadAssetAtPath<Readme>(AssetDatabase.GUIDToAssetPath(ids[0]))")
    readme.write_text(text, encoding="utf-8")
    adapted.append(readme.relative_to(ROOT).as_posix())
    docs = ROOT / "Documentation/ImportedReef-2026-11-05"
    shutil.copytree(SOURCE / "Documentation", docs, dirs_exist_ok=True)
    for name in ("FILE_MANIFEST.json", "迁移说明.txt"):
        shutil.copy2(SOURCE / name, docs / name)
    report = {"copied_assets_and_meta": len(copied), "copied": copied,
              "retained_destination_folder_meta": retained, "unity_6_3_api_adaptations": adapted}
    (WORK / "asset-import.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps({k: v for k, v in report.items() if k != "copied"}))


if __name__ == "__main__":
    main()
