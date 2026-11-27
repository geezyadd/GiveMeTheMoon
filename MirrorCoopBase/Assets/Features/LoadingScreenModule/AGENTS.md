# LoadingScreenModule — the screen that covers scene changes

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## The idea
A black screen covers every scene change, so the player never sees a scene load,
a hitch or an empty frame. `ILoadingScreenService.FadeIn(onComplete)` covers the screen, `FadeOut(onComplete)`
uncovers it; the action may be null. `FadeOut(delaySeconds, onComplete)` keeps the screen dark for the delay first:
after the lobby or the game map loads, the callers hold it for `ConnectionConfig.LoadingScreenHoldSeconds` (1.5 s) so
falling items and players still in the T-pose settle out of sight. The hold is a fixed time, not a readiness check: a
slow machine may still show the end of the spawn. A call during the opposite fade turns it around from where it is, and the cut-off
fade still calls its actions so nobody waits forever. Async callers await `FadeInAsync()`
(`LoadingScreenServiceExtensions`). The fade runs on `ICoroutineRunner` ([CoroutineRunnerModule](../CoroutineRunnerModule/AGENTS.md))
on unscaled time.

## Assemblies
- `Features.LoadingScreenModule.Contracts` (layer 1, no references): `ILoadingScreenService`, the async extension.
- `Features.LoadingScreenModule` (layer 2; Contracts, CoroutineRunnerModule, Zenject): `LoadingScreenService` (plain
  class), `LoadingScreenView`, `LoadingScreenModuleInstaller`, called from GameCore's `GlobalSceneInstaller`.

## Key places
| What | Where |
|---|---|
| The view | `GameResources/Prefabs/LoadingScreen.prefab`, placed in `GlobalScene` (bound `FromComponentInHierarchy`): overlay canvas, sorting order 32000, black `Background` (no loader animation yet). Fade times are on `LoadingScreenView` (0.6 s in, 1.5 s out). The canvas is off while uncovered and blocks clicks while shown |
| Menu → session (host, join, Steam) | `MenuSessionPresenter.RunAsync`: covers, connects and enters the session state, uncovers after the hold (at once on a failure) |
| Leave to the menu (lobby or game) | `LeaveSessionPresenter`: covers, stops the session, enters the menu state, uncovers |
| Lobby ↔ game (every peer) | `ConnectionMapChange.HandleSceneChange` waits for the cover before the map loads; `FinishMapReady` uncovers after `MapReady` and the hold |
| A dropped session | `ConnectionNetworkManager.StopSessionAndReturnToMenu` uncovers once the menu is back, so a drop during a map change does not leave the menu covered |

## Known limits
- A player who joins a run already in flight at a station loads the game map after the menu presenter has uncovered:
  the lobby shows for a moment and the map load is not covered.
- Checked in Play mode on the host only (2026-11-27): host, lobby → game, game → lobby, leave to the menu. Not checked
  with a second player.
