# ScreenShakeModule — screen shake, the strongest wins

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## The idea
Ported from FLEXUS-test-assignment's ScreenShakeModule (2026-11-27). Every shake runs out its own time, but only the
strongest one at each moment is shown: a small shake during a big one adds nothing, and when the big one ends a longer
weaker one goes on from where its own fade is. The shake is the Cinemachine camera's noise
(`CinemachineBasicMultiChannelPerlin`, the `6D Shake` noise profile). Local: a shake never goes over the network.

Used only by explosions for now. The older camera shake (`ICameraShakeService`, CameraModule: weapons, rock impacts)
is separate and still in use; the two add up on screen.

## Assembly
`Features.ScreenShakeModule` (layer 1; references Zenject, Unity.Cinemachine). Bindings: `ScreenShakeModuleInstaller`
(service, system), called from GameCore's `GlobalSceneInstaller`; `ScreenShakeModel` is bound in `DataInstaller`.

## Parts
- `ScreenShakeProfile` (Create > Screen Shake > Profile): amplitude gain at strength 1, frequency gain, duration (fades
  linearly to zero). A new source brings its own profile asset.
- `IScreenShakeService` / `ScreenShakeService`: `Shake(profile, strength)`; strength 0 or less is ignored.
- `ScreenShakeModel`: the running shakes (`ActiveScreenShake`) and the cameras that show them (`ICameraShaker`).
- `ScreenShakeSystem` (`ITickable`, `Advance(deltaTime)`): every shake loses the frame's time and the ended ones go; the
  strongest left drives every camera's amplitude and frequency; with none left they are set to 0.
- `CameraShaker`: on the FP and TP camera prefabs (CameraModule), next to their `CinemachineBasicMultiChannelPerlin`
  (amplitude 0 at rest). The game cameras are not made through the container, so `GameCameraService` binds it to the
  model when it spawns a camera (`Bind`); it is listed while it is on.

## Tests
`Features.ScreenShakeModule.Editor.Tests`: `ScreenShakeSystemTests` (only the strongest is shown, a weaker longer one
goes on with its own fade after the strong one ends, the cameras stop when every shake ended).
