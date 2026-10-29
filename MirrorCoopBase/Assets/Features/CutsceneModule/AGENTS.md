# CutsceneModule — shared cutscenes

A short cutscene every player sees at the same time: the server starts it, each peer moves its own camera along the
same shots at the same network time, blocks its player's controls and hides its HUD, then blends back to the player.
Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## Assembly
- `Features.CutsceneModule.Contracts` (layer 1; no references): `CutsceneModule/Contracts/`: `ICutsceneDirector`
  (server: `ServerPlay(shots)`, `ServerSkip()`, `IsPlaying`), `CutsceneShot` / `CutsceneShotKind` (look at a point,
  follow a target) and `ICutsceneTargetSource` (every peer: where a follow shot's target stands on this peer), so a
  feature plays a cutscene without referencing the module.
- `Features.CutsceneModule` (layer 6; references: CutsceneModule.Contracts, GameCoreModule.Contracts, Camera,
  CharacterMovable, Input (core and realization), Mvp, Lobby (the game HUD window), Ship (the ship's network identity and
  the component registry, the debug conditions), DebugModule.Contracts, NetworkModel, Mirror, Zenject, Unity.Cinemachine).
  Its own module rather than more code in CameraModule: it needs the network, the HUD window and the player controls,
  which the camera must not depend on. Installed by `CutsceneModuleInstaller` in `GlobalSceneInstaller`; the models
  (`CutsceneModelInstaller`, `CutsceneLocalModel`) in `DataInstaller`; `CutsceneConfiguration` in
  `ConfigurationInstaller`. Tests: `Features.CutsceneModule.Editor.Tests`.

## Key places
| What | Where |
|---|---|
| Data | `CutsceneConfiguration_Default` (`GameResources/Configurations`, Addressables `Configurations`, checked by `CutsceneConfigurationValidator`): start lead 0.15 s (the server starts a cutscene that far ahead so every client has it before its first frame), return blend 0.8 s, camera turn 0.7 s, curve height 0.2 of the distance, field of view 60, the debug test cutscene (40 m beside the ship, 15 m up, two shots of 2 s) |
| Network | One synced model `Cutscene` (generated from `NetworkModelModule/GameResources/Definitions/Cutscene.asset`, Shared, field `Playback`): a `CutscenePlayback` (sequence, start network time, shots seconds, skipped, the `CutsceneShot[]`), one SyncVar, so a client gets the whole cutscene at once. Bridge `Network/CutsceneSync` on the ship's `PoseSync` network identity (next to `FieldAgentSync`), found through `IComponentRegistry<CutsceneSync>`; it also carries the skip request `CmdSkip` (no authority needed). A late joiner past the end of a playback ignores it |
| Server | `Server/CutscenePlaybackService` (`ICutsceneDirector`): `ServerPlay` writes the next sequence starting `StartLeadSeconds` ahead; `ServerSkip` marks the running one skipped. Needs the run's ship (throws without its sync) |
| Every peer | `Client/CutsceneCameraSystem` on `IGameCameraService.LookApplied` (before the camera brain updates): `CutscenePlaybackRule` decides start (a sequence this peer has not played, still running) and end (its shots are over, skipped or replaced); its own `CinemachineCamera` (`CutsceneCamera`, made at startup) takes the view through `IGameCameraService.BeginOverrideView` (a cut: it starts at the player's view) and `CutsceneCameraPath` places it every frame: each shot eases the camera on a curve bowing up from where the last shot left it to the shot's point and turns it onto the shot's focus (a follow shot's focus is asked from the `ICutsceneTargetSource`s each frame, the shot's own point when none answers); at the end `EndOverrideView` blends back to the player's camera over the return blend. `CutsceneLocalModel` (local) says whether this peer plays now; `CutscenePlayerLockSystem` holds `PlayerControlBlockModel` meanwhile (no walking, aim, use, hand weapons, helm; the turret checks the block too), `CutsceneHudSystem` hides the `GameHudWindow` and shows it again |
| Skip | **Space skips it for everyone** (the simpler network option): `CutsceneSkipSystem` listens to the jump action of `IInputService` (the control block keeps it from jumping) and sends `CutsceneSync.CmdSkip`; the server marks the playback skipped and every peer blends back |
| Debug | Debug window → View → "Play a test cutscene" (host with a ship, `CutsceneDebugSystem`): the ship from its right, then from its left |

## Known limits
- Shots hold world points: a world shift during a cutscene (the loop test shifts in cruise) leaves its camera at the
  old places until it ends.
- A follow target is resolved on each peer from what that peer shows, so two players may follow slightly different
  middles of a flock.
- The navigator's sonar window stays open over a cutscene; the cutscene plays behind it.
- No sounds.

## Tests
- `Features.CutsceneModule.Editor.Tests` (`CutscenePlaybackTests`: a playback starts once and ends at the end of its
  shots, a skip or a new sequence ends it, the camera starts at the player's view and ends a shot at its point looking at
  its focus, a moved follow target is looked at).
- PlayMode: `PointOfInterestNestTest` and `LoopSmokePointOfInterestCheck` play the bug nest's entry cutscene (controls
  blocked, HUD hidden, it ends by itself, Space skips it) and the debug test cutscene (skipped with Space).
