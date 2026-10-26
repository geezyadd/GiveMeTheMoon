# SpaceHazeModule — the dark space haze

The haze that limits how far the crew sees by eye, so the navigator's sonar leads the ship. Read the
[root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in
the root file.

## Assembly
- `Features.SpaceHazeModule.Data` (layer 1; no references): `SpaceHazeModel`, this peer's own debug haze settings
  (off, a clear distance of its own), local and unsynced, bound in `DataInstaller`.
- `Features.SpaceHazeModule` (layer 2; references: SpaceHazeModule.Data, GameCoreModule.Contracts,
  DebugModule.Contracts, Zenject, Unity.RenderPipelines.Universal.Runtime, Unity.RenderPipelines.Core.Runtime): the
  configuration, the haze rule, the URP pass and its systems; installed by `SpaceHazeModuleInstaller` in
  `GlobalSceneInstaller`. Tests: `Features.SpaceHazeModule.Editor.Tests`.

## Key places
| What | Where |
|---|---|
| Haze | `SpaceHazeModule/`. **Every peer** hazes its own game camera from the same `SpaceHazeConfiguration_Default` (Addressables `Configurations`), so all players see equally far; nothing is synced. `SpaceHazeSystem` hooks `RenderPipelineManager.beginCameraRendering`: for a Base game camera (`CameraType.Game`, its `UniversalAdditionalCameraData` looked up once per camera) while a run's ship stands (`IShipPositionSource`: stations and flight; not in the menu or the lobby) it asks `SpaceHazeRule` for the haze shape and enqueues `SpaceHazeRenderPass` into the camera's renderer (no renderer feature asset, no scene change). The pass (`AfterRenderingSkybox`, `requiresIntermediateTexture`, depth input) copies the camera colour through `SpaceHaze.shader` (`GameResources/Shaders`, referenced by the configuration so it ships with the Addressables): from the camera depth it takes every pixel's distance; within `ClearMeters` (150 m) nothing changes, between it and `HiddenMeters` (260 m) a thing fades (smoothstep), past it it is hidden, and from `BackdropStartMeters` (1 800 m) to `BackdropEndMeters` (2 000 m) the haze thins out again, so the planets (2 km and farther) and the sky (the far plane) stay as the backdrop. A hidden thing fades **into the sky behind it**: `SpaceHazeSkySource` hands the material the scene's six-sided skybox (`RenderSettings.skybox`: faces, tint, exposure, turn; read again only when the skybox changes) and the shader samples it along the pixel's view direction, so a rock melts into the nebula instead of turning into a silhouette; `Tint` (alpha 0 by default) lays a colour of its own over that sky (a lighter tint shows as faint silhouettes on the black sky); a skybox without the six faces leaves the plain tint. `Strength` 1 hides a thing fully. The pass runs before the transparents and the HUD: screen-space markers (the sonar marks, `SonarMarkersView`) and the HUD are never hazed, so a navigator's mark leads the crew through the haze. `SpaceHazeRule.GetHazeShare` is the shader's formula (tested); change both together. **Debug** (editor / development builds, `SpaceHazeDebugSystem`, the debug window's "View" section): "Haze: on / off" (registers again with the new label) and "Haze clear distance" (metres; moves the whole fade, keeps its length) for this peer only. |

## Known limits
- The haze is a distance from the camera, not a volume: a big rock half inside the clear distance is sharp at its
  near side and fades at its far side.
- The next station is seen when it is farther than the backdrop (2 km), is hidden between about 1.8 km and the hidden
  distance, and shows again near: the station marker and the sonar lead there.
- The haze is not Unity fog: it needs no fog setting in the scenes and no fog shader variants in a build.

## Tests
- `Features.SpaceHazeModule.Editor.Tests` (`SpaceHazeRuleTests`: nothing hazed within the clear distance, half way in
  the middle of the fade, hidden past the hidden distance up to the backdrop, nothing past the backdrop end, the debug
  off, a debug clear distance moves the fade and keeps its length, a clear distance past the backdrop pushes the
  backdrop behind the fade).
