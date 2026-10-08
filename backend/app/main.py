from __future__ import annotations

from datetime import datetime, timezone
from pathlib import Path
from typing import Annotated

from fastapi import FastAPI, HTTPException
from fastapi.responses import FileResponse
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel, Field

REPO_ROOT = Path(__file__).resolve().parents[2]
FRONTEND_DIR = REPO_ROOT / "frontend"
UNITY_BUILD_DIR = FRONTEND_DIR / "unity" / "Build"
UNITY_TEMPLATE_DIR = FRONTEND_DIR / "unity" / "TemplateData"
UNITY_STREAMING_DIR = FRONTEND_DIR / "unity" / "StreamingAssets"

app = FastAPI(
    title="Random Walk Lab",
    version="1.0.0",
    description="Thin web backend for the Unity 1D random-walk visualizer.",
)


class ExperimentConfig(BaseModel):
    agentCount: Annotated[int, Field(ge=1, le=2000)] = 100
    maxMoves: Annotated[int, Field(ge=1, le=100000)] = 1000
    speedMultiplier: Annotated[float, Field(ge=0.25, le=5.0)] = 1.0


@app.get("/api/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.get("/api/config")
def config() -> dict:
    return {
        "defaults": {
            "agentCount": 100,
            "maxMoves": 1000,
            "speedMultiplier": 1.0,
        },
        "limits": {
            "agentCount": {"min": 1, "max": 2000},
            "maxMoves": {"min": 1, "max": 100000},
            "speedMultiplier": {"min": 0.25, "max": 5.0},
        },
    }


@app.post("/api/experiments")
def record_experiment(payload: ExperimentConfig) -> dict:
    # The simulation itself runs entirely in the browser. This endpoint intentionally
    # stays stateless for now; it gives the frontend a stable full-stack contract that
    # can later be upgraded to persistence/history without changing Unity.
    return {
        "accepted": True,
        "receivedAt": datetime.now(timezone.utc).isoformat(),
        "config": payload.model_dump(),
    }


def _unity_media_type(filename: str) -> str:
    if filename.endswith(".wasm.br"):
        return "application/wasm"
    if filename.endswith(".js.br"):
        return "application/javascript"
    if filename.endswith(".data.br"):
        return "application/octet-stream"
    if filename.endswith(".js"):
        return "application/javascript"
    if filename.endswith(".wasm"):
        return "application/wasm"
    return "application/octet-stream"


@app.get("/unity/Build/{filename:path}")
def unity_build_file(filename: str) -> FileResponse:
    path = (UNITY_BUILD_DIR / filename).resolve()
    try:
        path.relative_to(UNITY_BUILD_DIR.resolve())
    except ValueError as exc:
        raise HTTPException(status_code=404, detail="Not found") from exc

    if not path.is_file():
        raise HTTPException(status_code=404, detail="Unity build file not found")

    headers = {
        "Cache-Control": "no-store, no-cache, must-revalidate, max-age=0",
        "Pragma": "no-cache",
        "Expires": "0",
    }
    if filename.endswith(".br"):
        headers["Content-Encoding"] = "br"

    return FileResponse(path, media_type=_unity_media_type(filename), headers=headers)


if UNITY_TEMPLATE_DIR.exists():
    app.mount(
        "/unity/TemplateData",
        StaticFiles(directory=UNITY_TEMPLATE_DIR),
        name="unity-template-data",
    )

if UNITY_STREAMING_DIR.exists():
    app.mount(
        "/unity/StreamingAssets",
        StaticFiles(directory=UNITY_STREAMING_DIR),
        name="unity-streaming-assets",
    )

# Mount last so API and special Unity routes above take precedence.
app.mount("/", StaticFiles(directory=FRONTEND_DIR, html=True), name="frontend")
