"""Compile the prepared bridge against this computer's Unity 6.3 assemblies before import."""
from pathlib import Path
import importlib.util
import json
import re
import shutil
import subprocess
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
EDITOR = Path(r"C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Data")
CACHED_ASSEMBLIES = Path(r"D:\project\Unity\My project1\Library\ScriptAssemblies")


def main():
    work = ROOT / "Migration~/2026-11-05"
    with tempfile.TemporaryDirectory(prefix="csharp-preflight-", dir=work) as td:
        candidate = Path(td)
        with zipfile.ZipFile(work / "UnderTheSea-before-migration.zip") as archive:
            archive.extractall(candidate)
        shutil.copytree(work / "SourceProject/Assets", candidate / "Assets", dirs_exist_ok=True)
        shutil.copytree(ROOT / "Tools/ReefIntegration~", candidate / "Tools/ReefIntegration~")
        spec = importlib.util.spec_from_file_location("bridge", ROOT / "Tools/apply_reef_integration.py")
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        module.ROOT = candidate
        module.main()
        for path in (candidate / "Assets/UltrasoundReef/Editor").glob("*.cs"):
            text = path.read_text(encoding="utf-8-sig")
            text = re.sub(r"FindObjectsByType<([^>]+)>\(FindObjectsInactive.Include\)",
                          r"FindObjectsByType<\1>(FindObjectsInactive.Include, FindObjectsSortMode.None)", text)
            text = re.sub(r"FindObjectsByType<([^>]+)>\(\)", r"FindObjectsByType<\1>(FindObjectsSortMode.None)", text)
            path.write_text(text, encoding="utf-8")
        path = candidate / "Assets/TutorialInfo/Editor/ReadmeEditor.cs"
        text = path.read_text(encoding="utf-8-sig").replace("using Unity.Scripting.LifecycleManagement;\n", "")
        text = text.replace("[OnCodeLoaded]", "[InitializeOnLoadMethod]")
        text = text.replace('AssetDatabase.FindAssetGUIDs("Readme t:Readme")', 'AssetDatabase.FindAssets("Readme t:Readme")')
        text = text.replace("AssetDatabase.LoadAssetByGUID<Readme>(ids[0])", "AssetDatabase.LoadAssetAtPath<Readme>(AssetDatabase.GUIDToAssetPath(ids[0]))")
        path.write_text(text, encoding="utf-8")
        refs = list((EDITOR / "NetStandard/ref/2.1.0").glob("*.dll"))
        refs += list((EDITOR / "NetStandard/compat/2.1.0/shims/netfx").glob("*.dll"))
        refs += list((EDITOR / "Managed").rglob("Unity*.dll"))
        refs += [p for p in CACHED_ASSEMBLIES.glob("*.dll") if not p.name.startswith("Assembly-CSharp")]
        sources = list((candidate / "Assets").rglob("*.cs"))
        options = ["-nologo", "-target:library", "-nostdlib+", "-langversion:latest",
                   "-define:UNITY_EDITOR,UNITY_EDITOR_WIN,UNITY_STANDALONE_WIN,UNITY_6000_0_OR_NEWER,UNITY_6000_3_OR_NEWER",
                   f'-out:"{candidate / "BridgePreflight.dll"}"']
        options += [f'-r:"{p}"' for p in dict.fromkeys(refs)]
        options += [f'"{p}"' for p in sources]
        response = candidate / "compile.rsp"
        response.write_text("\n".join(options), encoding="utf-8")
        result = subprocess.run([str(EDITOR / "NetCoreRuntime/dotnet.exe"),
                                 str(EDITOR / "DotNetSdkRoslyn/csc.dll"), "@" + str(response)],
                                capture_output=True, text=True)
        log = result.stdout + result.stderr
        (work / "csharp-preflight.log").write_text(log, encoding="utf-8")
        print(log[-16000:])
        print(json.dumps({"exit_code": result.returncode, "sources": len(sources), "references": len(refs),
                          "note": "Static compile only; cached package assemblies, not a Unity import or runtime acceptance."}))
        raise SystemExit(result.returncode)


if __name__ == "__main__":
    main()
