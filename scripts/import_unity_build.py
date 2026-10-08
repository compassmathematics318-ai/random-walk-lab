from __future__ import annotations

import json
import re
import shutil
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
DEST = REPO_ROOT / "frontend" / "unity"


def _single(build_dir: Path, pattern: str) -> str:
    matches = sorted(p.name for p in build_dir.glob(pattern) if p.is_file())
    if len(matches) != 1:
        raise RuntimeError(
            f"Expected exactly one {pattern} in {build_dir}, found {len(matches)}: {matches}"
        )
    return matches[0]


def _metadata_from_generated_index(index_path: Path) -> dict[str, str]:
    defaults = {
        "companyName": "DefaultCompany",
        "productName": "1D_prob_sim",
        "productVersion": "0.1.0",
    }
    if not index_path.is_file():
        return defaults

    text = index_path.read_text(encoding="utf-8", errors="ignore")
    for key in tuple(defaults):
        match = re.search(rf'{key}:\s*["\']([^"\']+)["\']', text)
        if match:
            defaults[key] = match.group(1)
    return defaults


def main() -> int:
    if len(sys.argv) != 2:
        print('Usage: python scripts/import_unity_build.py "C:\\path\\to\\UnityWebBuild"')
        return 2

    source = Path(sys.argv[1]).expanduser().resolve()
    build_dir = source / "Build"
    template_dir = source / "TemplateData"

    if not build_dir.is_dir() or not template_dir.is_dir():
        print("Expected the selected folder to contain Build/ and TemplateData/.")
        return 1

    # Validate before deleting the current working payload.
    try:
        build_files = {
            "loader": _single(build_dir, "*.loader.js"),
            "data": _single(build_dir, "*.data*"),
            "framework": _single(build_dir, "*.framework.js*"),
            "code": _single(build_dir, "*.wasm*"),
        }
    except RuntimeError as exc:
        print(exc)
        return 1

    for name in ("Build", "TemplateData", "StreamingAssets"):
        src = source / name
        dst = DEST / name
        if dst.exists():
            shutil.rmtree(dst)
        if src.exists():
            shutil.copytree(src, dst)
            print(f"Imported {name}/")

    generated_index = source / "index.html"
    if generated_index.is_file():
        shutil.copy2(generated_index, DEST / "unity-generated-index.reference.html")

    manifest = {
        **build_files,
        **_metadata_from_generated_index(generated_index),
    }
    (DEST / "build.json").write_text(
        json.dumps(manifest, indent=2) + "\n",
        encoding="utf-8",
    )
    print("Updated frontend/unity/build.json")
    print("Unity Web payload imported successfully.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
