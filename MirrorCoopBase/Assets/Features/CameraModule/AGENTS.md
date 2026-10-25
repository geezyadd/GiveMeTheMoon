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
  turns or moves the camera for a shake.

## Key places
| What | Where |
|---|---|
| Camera shake | **only through `ICameraShakeService`** (contracts in `CameraModule/Contracts/`, assembly `Features.CameraModule.Contracts`; core in `CameraModule/Scripts/Shake/`). A shake is one `CameraShakeProfile` asset (degrees, roll share, metres, seconds, frequency, fade power, length scaled by strength); a new source brings its own profile asset, no code in the camera. One-shot: `AddShake(profile, strength)`, one entry per profile in `CameraShakeModel` (bound in `DataInstaller`): a stronger shake of that profile restarts it, a weaker one is ignored while the current one is still stronger, different profiles add up; it fades as (1 - age / length) ^ power and stays in the model at zero (no allocation per hit). Continuous: `BeginShake(profile)` returns an `ICameraShakeHandle` (`SetIntensity` eases over the profile's seconds, `Stop` removes it). `CameraShakeSampler` (pure, tested in `CameraShakeTests`) moves every shake on and sums them (Perlin noise per axis); `CameraShakeSystem` applies the sum to the look pivot in `LookApplied` and clears the model on `CleanupGameplay` / `RestartGameplay` and every scene load. Sources: rock impacts (`ShipImpactCameraShakeSystem`), the minigun (`MinigunFeel`) |

## Tests
- `Features.CameraModule.Editor.Tests` (`CameraShakeTests`: a weaker shake of one profile never cuts a stronger one, a stronger one restarts it, two profiles add up, a continuous + a one-shot shake sum, a one-shot fades to zero, a length scaled by strength, the continuous intensity eases over the profile's seconds, stop and clear)
