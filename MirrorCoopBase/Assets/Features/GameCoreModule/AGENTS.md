# GameCoreModule — composition root

The composition root (installers), the shared contracts and the scenes. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file.

## Assembly
- `Features.GameCoreModule.Contracts` (layer 1): `IGameplaySession`, `SceneNames`; the run's contracts for the flight field: `IFlightRouteListener` + `FlightLaunch`, `IWorldShiftFollower`, `IShipPositionSource`, `ILocalViewpointSource`.
- `Features.GameCoreModule` (layer 7; references: all modules): `GlobalSceneInstaller` calls `ShipModuleInstaller`, then `ShipImpactModuleInstaller` and `ShipDamageModuleInstaller`.

## Key places
| What | Where |
|---|---|
| Scenes | `GameCoreModule/GameResources/Scenes/`: Bootstrap → Menu → Lobby → Game; only Bootstrap is in Build Settings |

## Rendering
- `GameResources/Rendering/GlobalVolume.prefab`: one global URP `Volume` (priority 0) with
  `GameResources/Rendering/GlobalVolumeProfile` (Bloom + Vignette, a copy of the GabrielAguiarProductions pack's
  `URPGP1_GlobalVolumeProfile`, so the explosions glow). An instance sits in `LobbyScene` and in `GameScene`; change the
  look in the prefab or the profile, not in a scene. The output camera (`GameCameraOutput` in CameraModule's
  `GameCameraRoot`) renders post-processing since 2026-11-27 (it did not before, so no volume showed).

## Tests
- `Features.GameCoreModule.Editor.Tests` (configuration loading and module catalog guards)
- EditMode guard: `ConfigurationLoadingGuardTests` (no `Resources.Load`, every configuration address loads), `ModuleCatalogGuardTests` (module ids set and unique, shop and drops only use catalog modules, no code checks a concrete module).
