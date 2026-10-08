from __future__ import annotations
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
unity = root / "frontend" / "unity"
manifest = json.loads((unity / "build.json").read_text(encoding="utf-8"))

print("Random Walk Lab repo check")
print("- frontend/index.html:", (root / "frontend" / "index.html").is_file())
print("- backend/app/main.py:", (root / "backend" / "app" / "main.py").is_file())
for key in ("loader", "data", "framework", "code"):
    p = unity / "Build" / manifest[key]
    print(f"- Unity {key}: {p.name} ({'OK' if p.is_file() else 'MISSING'})")
