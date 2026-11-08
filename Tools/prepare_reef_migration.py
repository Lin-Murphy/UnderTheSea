"""Snapshot the destination and verify/stage the supplied Unity source project."""
from pathlib import Path
import hashlib
import json
import shutil
import zipfile

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(r"C:\Users\Murphy\Documents\xwechat_files\wxid_ukkgdn493pok11_d38b\msg\file\2026-11\Ultrasound_Under_the_Sea_2026-11-05\Ultrasound_Under the Sea")
WORK = ROOT / "Migration~" / "2026-11-05"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    WORK.mkdir(parents=True, exist_ok=True)
    archive = WORK / "UnderTheSea-before-migration.zip"
    if archive.exists():
        raise SystemExit("Backup already exists; refusing to replace the rollback point.")
    originals = [p for folder in ("Assets", "Packages", "ProjectSettings")
                 for p in (ROOT / folder).rglob("*") if p.is_file()]
    originals += [ROOT / "CLAUDE.md", ROOT / "ignore.conf"]
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as backup:
        for path in originals:
            backup.write(path, path.relative_to(ROOT).as_posix())
    with zipfile.ZipFile(archive) as backup:
        assert backup.testzip() is None

    manifest = json.loads((SOURCE / "FILE_MANIFEST.json").read_text(encoding="utf-8-sig"))
    bad = [item["path"] for item in manifest
           if not (SOURCE / item["path"]).is_file()
           or sha(SOURCE / item["path"]) != item["sha256"]]
    if bad:
        raise SystemExit(f"Source checksum mismatch: {bad}")
    stage = WORK / "SourceProject"
    shutil.copytree(SOURCE, stage)
    report = {
        "source": str(SOURCE), "destination": str(ROOT),
        "backup": str(archive), "backup_sha256": sha(archive),
        "source_manifest_verified_files": len(manifest),
        "originals": [{"path": p.relative_to(ROOT).as_posix(), "sha256": sha(p)}
                      for p in originals],
        "source_assets": [{"path": p.relative_to(SOURCE).as_posix(), "sha256": sha(p)}
                          for p in (SOURCE / "Assets").rglob("*") if p.is_file()],
    }
    (WORK / "inventory.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({k: v for k, v in report.items() if not isinstance(v, list)}, ensure_ascii=True))


if __name__ == "__main__":
    main()
