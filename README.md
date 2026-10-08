# Random Walk Lab

A full-stack web wrapper around the Unity 6.3 1D random-walk simulation.

This replacement repo already contains the **new Unity Web build** supplied in `NewBuilds.zip`. The
website controls are wired to Unity's `AgentSpawner.ApplyWebConfig` bridge, so changing the walker
count, maximum steps, or speed and pressing **Run experiment** starts a fresh Unity experiment with
those values.

## Website controls

The configuration panel exposes:

- **Walking agents:** 1–2,000
- **Maximum steps:** 1–100,000
- **Walking speed:** 0.25×–5×

Unity controls shown in the interface:

- **WASD** — move camera
- **Space / Shift** — move vertically
- **Mouse** — look around
- **Scroll** — zoom
- **Q** — pause/resume and release/recapture the cursor
- **R** — reset the current experiment
- **Esc** — leave browser fullscreen

## Repository layout

```text
random-walk-lab/
├─ frontend/
│  ├─ index.html
│  ├─ styles.css
│  ├─ app.js
│  └─ unity/
│     ├─ build.json
│     ├─ Build/
│     ├─ TemplateData/
│     └─ unity-generated-index.reference.html
├─ backend/
│  ├─ app/main.py
│  ├─ requirements.txt
│  └─ Dockerfile
├─ unity-source-patch/       # reference C# bridge/source for future Unity rebuilds
├─ scripts/
├─ docs/
├─ .github/workflows/
├─ docker-compose.yml
└─ README.md
```

`build.json` makes the custom frontend independent of Unity's build prefix. The current payload uses
`NewBuilds.*`; future builds can have any prefix and the import helper will regenerate the manifest.

## First-time local setup (Windows / PowerShell)

From the repo root:

```powershell
py -3.11 -m venv .venv
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
python -m pip install -r backend\requirements.txt
.\scripts\dev.ps1
```

Open:

```text
http://127.0.0.1:8000
```

## Normal startup after that

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\.venv\Scripts\Activate.ps1
.\scripts\dev.ps1
```

If you replaced frontend or Unity files while the browser was already open, use **Ctrl+F5** once.

To stop the server, press **Ctrl+C** in the PowerShell window.

## Important: do not double-click frontend/index.html

Unity Web builds need to be served over HTTP. This FastAPI server also supplies the correct
`Content-Encoding: br` and MIME types for the Brotli-compressed Unity `.data`, JavaScript, and WebAssembly files.

During development the Unity build responses are deliberately sent with **no-cache** headers, which
prevents Chrome from silently reusing an older `.wasm` after a rebuild.

## Import a future Unity Web build

If you rebuild Unity later, point the helper at Unity's output folder (the folder containing `Build/`
and `TemplateData/`):

```powershell
python scripts\import_unity_build.py "C:\path\to\YourNewUnityWebBuild"
```

The helper will:

1. replace only the Unity payload under `frontend/unity/`;
2. preserve the custom website;
3. detect the new `.loader.js`, `.data`, `.framework.js`, and `.wasm` filenames;
4. regenerate `frontend/unity/build.json` automatically.

Then restart the server and hard-refresh the browser.

## GitHub

The repo intentionally does **not** include `.venv`; create it locally as shown above. `.gitignore`
already excludes virtual environments and Python caches.

```powershell
git init
git add .
git commit -m "Initial Random Walk Lab"
```

## Quick parameter-bridge check

Start the site, wait for **Simulation ready**, set something obvious such as:

```text
Walking agents: 25
Maximum steps: 50
Walking speed: 2.00x
```

Press **Run experiment**. The Unity HUD should reset to `TOTAL WALKERS 25` and the new population
should begin immediately.
