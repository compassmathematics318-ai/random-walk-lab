# Architecture

```text
Browser
  ├─ HTML/CSS/JavaScript UI
  ├─ Unity Web runtime (WebAssembly)
  └─ SendMessage("AgentSpawner", "ApplyWebConfig", json)
             │
             ▼
Unity AgentSpawner.cs
  ├─ agentCount
  ├─ maxMoves
  ├─ walkSpeedMultiplier
  └─ ResetExperiment()

FastAPI
  ├─ serves the frontend
  ├─ serves Brotli-compressed Unity assets with correct HTTP headers
  ├─ GET /api/config
  ├─ GET /api/health
  └─ POST /api/experiments
```

The simulation remains authoritative inside Unity. The backend does not attempt to reproduce the
random-walk logic. The web UI only supplies experiment parameters and presentation.

## Why the special Unity asset route exists

The uploaded build uses Brotli files (`.br`). A correct server response needs both the underlying
content type (for example `application/wasm`) and `Content-Encoding: br`. The FastAPI route in
`backend/app/main.py` sets those headers explicitly.
