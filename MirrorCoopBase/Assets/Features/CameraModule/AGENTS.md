# CameraModule — camera and camera shake

The game camera and the camera shake. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file.

## Assembly
- `Features.CameraModule` (layer 3; references: CharacterMovable, Contracts, CameraModule.Contracts, Input).
- `Features.CameraModule.Contracts` (layer 1; no references): `CameraModule/Contracts/`: `ICameraShakeService`, `ICameraShakeHandle`, `CameraShakeProfile`, so the weapons and the ship shake the camera without referencing the camera module.

## Rules
- The camera does not know about Grab: `IGameCameraService.LookApplied` fires in `PresentNow()` after
  `ApplyLookRig()` and before the brain update; `HeldItemFollowSystem` (Grab) listens to it. The frame order is the
  same as before.
- **Camera shake goes only through `ICameraShakeService`** (`Features.CameraModule.Contracts`): `AddShake(profile,
  strength)` for a one-shot shake, `BeginShake(profile)` for a continuous one (the handle sets the intensity and
  stops it). `CameraShakeSystem` (CameraModule) applies the sum of all shakes to the look pivot once per frame in
  `LookApplied`, so a seated station's view (the turret's camera point, `BeginFixedView`) shakes too; nothing else
  turns or moves the camera for a shake. The one exception (2026-11-27, on the user's request): the explosions' screen
  shake is ScreenShakeModule's, through the Cinemachine noise of the FP / TP cameras (`CameraShaker`, bound by
  `GameCameraService` when it spawns a camera).

- **Look lock:** while any owner holds `CameraLookLockModel` (bound in `CameraModuleInstaller`) the mouse does not turn
  the view (`CameraLookDriver` drops the look input; the cursor stays locked). The Grab module takes it while a held
  control (the throttle lever) is moved by the mouse.

## Key places
| What | Where |
|---|---|
| Camera objects | all from prefabs in `CameraCatalog` (nothing is added in code): `GameCameraRoot` (the root kept across scenes, with `GameCameraOutput`: the main camera, its audio listener and the Cinemachine brain in manual update with blends in LateUpdate), `CameraLookPivot` (the look pivot with `CameraLookRig`; a new one is made when the old one went down with a player), and the FP / TP camera prefabs. A missing prefab is an error in the console |
| Turning with a carrier | `CameraCarryTurnModel` (bound in `DataInstaller`): the local player's `CarrierRider` adds the yaw its carrier (a moving platform) turned each frame, as drawn; `CameraLookDriver.Tick` adds it to the rig's yaw on every look path, so the view turns with what the player stands on (the player body itself never turns) |
| Camera shake | **only through `ICameraShakeService`** (contracts in `CameraModule/Contracts/`, assembly `Features.CameraModule.Contracts`; core in `CameraModule/Scripts/Shake/`). A shake is one `CameraShakeProfile` asset (degrees, roll share, metres, seconds, frequency, fade power, length scaled by strength); a new source brings its own profile asset, no code in the camera. One-shot: `AddShake(profile, strength)`, one entry per profile in `CameraShakeModel` (bound in `DataInstaller`): a stronger shake of that profile restarts it, a weaker one is ignored while the current one is still stronger, different profiles add up; it fades as (1 - age / length) ^ power and stays in the model at zero (no allocation per hit). Continuous: `BeginShake(profile)` returns an `ICameraShakeHandle` (`SetIntensity` eases over the profile's seconds, `Stop` removes it). `CameraShakeSampler` (pure, tested in `CameraShakeTests`) moves every shake on and sums them (Perlin noise per axis); `CameraShakeSystem` applies the sum to the look pivot in `LookApplied` and clears the model on `CleanupGameplay` / `RestartGameplay` and every scene load. Sources: rock impacts (`ShipImpactCameraShakeSystem`), the minigun (`MinigunFeel`) |

## Player settings
The camera reads the player's settings only through its own models; the appliers in `Scripts/Settings/` are the only
code that reads `SettingsModel` (`Features.SettingsModule.Data`):
- `CameraLookSettingsModel` (bound in `DataInstaller`): mouse sensitivity and invert Y, read by `CameraLookDriver`
  (they were `CameraCatalog` fields before 2026-11-27); `CameraLookSettingsApplier`.
- `CameraFieldOfViewModel` (bound in `DataInstaller`): the first-person field of view; `CameraFieldOfViewSystem` sets it
  on the FP camera and adds `CameraCatalog.ThirdPersonFieldOfViewOffset` (20) for the TP camera, on a change and on
  every camera that registers (`GameCameraModel.OnCameraRegistered`); `CameraFieldOfViewSettingApplier`. Defaults
  60 / 80 as the prefabs had.
- `CameraShakeModel.StrengthScale`: `CameraShakeSystem` multiplies the summed pose by it (0 = no shake);
  `CameraShakeStrengthSettingApplier` sets it from the screen shake setting (default 50 % = 1x). The turret keeps its
  own look sensitivity (`ShipTurretDefinition`).

## Tests
- `Features.CameraModule.Editor.Tests` (`CameraShakeTests`: a weaker shake of one profile never cuts a stronger one, a stronger one restarts it, two profiles add up, a continuous + a one-shot shake sum, a one-shot fades to zero, a length scaled by strength, the continuous intensity eases over the profile's seconds, stop and clear)
