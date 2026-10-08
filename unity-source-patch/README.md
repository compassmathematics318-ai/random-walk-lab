# Unity source patch required for web-configurable parameters

The uploaded Unity Web build is already compiled. Browser JavaScript cannot change the private
`AgentSpawner.agentCount` field or the agents' runtime timing values unless Unity exposes a callable
method first.

This folder contains replacement scripts based directly on the project's latest `Scripts` folder.
The patch is deliberately small:

- `AgentSpawner.cs`
  - adds `maxMoves` and `walkSpeedMultiplier` experiment settings;
  - applies those settings to each spawned `RandomWalkAgent` before it starts;
  - exposes `ApplyWebConfig(string json)` for `unityInstance.SendMessage(...)`;
  - validates browser-provided values.
- `FreeCameraController.cs`
  - uses **Q** instead of **Esc** for pause/resume so browser fullscreen can use Esc normally.
- `RandomWalkAgent.cs`, `NumberLine.cs`, `StatsHUD.cs`
  - included as reference copies and intentionally otherwise unchanged.

## Apply it once

1. Back up the working Unity project.
2. Replace the corresponding `.cs` files in the Unity project's `Assets/Scripts/` folder with these files.
   Do **not** replace or delete the existing `.meta` files.
3. Keep the scene GameObject named exactly `AgentSpawner` because the webpage targets that object.
4. Confirm the project still runs in the Unity Editor.
5. Make a new **Web** build.
6. Import the rebuilt Web payload into this repo:

   ```powershell
   python scripts/import_unity_build.py "C:\path\to\your\new\UnityWebBuild"
   ```

7. Restart the web server and test the three website controls.

The custom website intentionally does not use Unity's generated `index.html`; it keeps only the
`Build/` and `TemplateData/` payloads and loads them from the custom frontend.
