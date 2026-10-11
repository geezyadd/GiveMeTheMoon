# Roadmap: M1, "boring but perfect loop"

Written 2026-10-01 from a code investigation. Statuses updated 2026-10-11 against the code. M1 goal:
station → buy → install → flight → next station.
The loop works in co-op without bugs and **feels good**. Game context is in `AGENTS.md`.

Status values: **Done**, **Partly**, **Not done**, **Deferred** (moved by a decision, see "Decisions"). "Now" says
what the code does today. The "Why" column is the state on 2026-10-01 and is kept as history.

## P0. The loop must be what was designed (M1 does not work without this)
| # | Task | Why (state on 2026-10-01) | Status | Now (2026-10-11) |
|---|---|---|---|---|
| 1 | **Modules stay on the ship between stations.** A normal landing goes straight to Build, without a reset | `FinishAtCurrentPose` → `ServerResetForBuild` calls `ClearInstalledModules()`, places wreckage and teleports the ship 18 m: all upgrades are lost at every station | Done | a normal landing ends in `ShipRunService.FinishLanded` → `ServerSettleAfterLanding`, without a reset; `ServerResetForBuild` runs only at the start and end of a run and on an aborted flight; `LoopSmokeTest` checks the engines after landing |
| 2 | **M1 economy.** A proper starting balance, income for arriving at a station (config). No or almost no free drops | there is no income; about 8 modules drop for free at every station (`ShipStationCatalog.Drops`), so the shop is not needed | Deferred | the starting balance is 1 000 000 (`WalletConfiguration`), there is no income, drops still spawn on every arrival (`ShipStationDropService.SpawnDrops`) |
| 3 | **Shop and installation only at a station, in the Build phase.** Checked on both client and server | the shop opens in flight; `CmdPurchase` checks nothing; `ShipSocket.CanAccept` allows installing modules in flight | Done (PR #5, #7) | the shop opens at the station kiosk; the server checks Build, alive and distance on open and on purchase (`ShopAccessService`, `ShopPurchaseSystem`); install and removal only in Build (`ShipSocketRule`); **L** opens the shop only in editor / development builds |
| 4 | **Removing a module returns the item via the shared catalog** | `ShipUninstallInteractable` looks for the prefab only in the drop list, so a shop-only module silently cannot be removed | Done (PR #13) | `ShipUninstallInteractable` takes the item prefab from the installed `ModuleDefinition`, resolved by id in `ModuleCatalog` |
| 5 | **A flight without a helm does not last forever.** Needs autopilot to the target or a mandatory helm, plus a timeout | without a helm the nose is not turned to the target, alignment ~0.09, the flight takes about 11 times longer; turning away from the target only increases the time | Deferred to M3 | in `TravelInSpace` a ship without a helm module keeps the route heading and arrives; a pilot who turns away can still fly forever. No autopilot and no timeout will be added (see "Decisions") |
| 6 | **Items on the deck travel with the ship** | the ship moves via transform; modules and cargo lying on the deck most likely stay at the old station (not verified) | Done | `ShipDeckCargo` attaches items lying on the deck to the ship; the world shift moves loose items on every peer (`ShipRunDirector.ServerShiftWorld`) |
| 7 | **Co-op: the whole loop verified with two players** (ParrelSync). The helm is released when the pilot disconnects. Seating is synced on the client | a pilot disconnect blocks control until landing; on a remote client the player does not move into the seat (found by audit) | Partly | a disconnecting rider frees the seat (`ShipRider.OnStopServer` → `ShipBase.ServerRemoveRider`), the seat state is a SyncVar on `ShipRider`; two players were run on 2026-10-02; the whole loop with two real clients has not been re-run after the 2026-10-06 changes (`LoopSmokeTest` uses a bot on the host) |
| 8 | Cleanup: old drops and wreckage do not pile up; shop text about the Helm fixed | drops keep hanging when the old pad is removed; in the shop the Helm says "Required to launch" although the slot is not mandatory | Not done | the pad before the previous one is destroyed (`ShipStationPads.TrySpawnNext`), but items left at an old station are destroyed only when the run ends; `HelmModule.asset` and `Tooltip_Helm.asset` still say "Required to launch the ship" |

## P1. Feel ("satisfying")
| # | Task | Why (state on 2026-10-01) | Status | Now (2026-10-11) |
|---|---|---|---|---|
| 9 | **Sense of speed in flight and a decision on the flight model.** Parallax or scrolling environment (stars, clouds, dust, distant objects) | in cruise the ship hangs in place, only rocks move, speed is not visible | Partly | the flight model is `TravelInSpace` by default: the ship really flies to the next pad (`ShipFlightConfig_Default.asset`); there is still no scenery or parallax, and the rocks are removed |
| 10 | **Basic sound:** footsteps, engines (pitch depends on thrust), lever, module install and removal, purchase, takeoff and landing, UI | there is not a single `AudioSource` in the game | Not done | no `AudioSource` or `AudioClip` in `Assets/Features` code or prefabs |
| 11 | **Interaction feedback:** tooltips (in progress), a "2 engines needed to take off" hint, effect and sound on install, lever animation, item throw | there is only an outline now; Release drops the item with zero velocity | Partly | tooltips work (`TooltipModule`, with module stats and the key hint); the lever tooltip does not say what blocks takeoff; no install effect or sound; a released item still gets zero velocity (`Grabbable`) |
| 12 | **Smooth landing and transition to Build:** no teleport and no sudden wreckage; players stay on the deck | landing currently looks like a jerk | Done in code | the ship eases onto the pad (`ShipFlight.SimulateTravelLanding`) and stays where it landed, wreckage appears only on an aborted flight; `LoopSmokeTest` checks the ship on the pad and the player on the deck. The feel has not been judged by a human in this update |
| 13 | **HUD:** station number, phase, ETA, what blocks takeoff | there is only a "to platform" timer and the balance | Not done | still only the "to platform" timer (`FlightTimePresenter`) and the balance |
| 14 | **Camera:** a single source (`Camera.main` and `OutputCamera` are mixed now), light shake and FOV on takeoff and landing, engine VFX | all of this is missing | Not done | `CharacterMovableBase` still reads `Camera.main`; no shake, FOV change or VFX |

## P2. Foundation for M2–M4 (content)
| # | Task | Why (state on 2026-10-01) | Status | Now (2026-10-11) |
|---|---|---|---|---|
| 15 | **Modules through data:** one module catalog (shop, drops, removal, views), each module has a list of `StatModifier` in data | a new module now needs edits in ~8 places and a `switch` on Engine (`ServerOnModuleInstalled`, `ShopItemStatsService`, `CanRemoveModule`) | Done (PR #13) | one `ModuleDefinition` asset per module and one `ModuleCatalog`; shop, drops, slots, stats and removal read it; `ModuleCatalogGuardTests` fails on code that checks a concrete module |
| 16 | **Stats really affect the game:** Thrust, Handling, Armor, `IFlightStatContributor`; real values visible on the client | only `FlightSpeed` works; modifiers exist only on the server | Partly (PR #11) | the final stats are synced to clients (`ShipStats` model) and base values are in `ShipAccumulativeStatsConfiguration`; gameplay reads only `FlightSpeed` and `DodgeRange`; only a development overlay shows the synced values; there is no `IFlightStatContributor` |
| 17 | **Obstacles and events through an interface and spawn tables.** Rock colliders, damage, `ServerAbort` | rocks are harmless and hardcoded in `ShipRunService`; `ServerAbort` is never called | Deferred to M3 | the rocks are removed from the code (2026-10-03); `ServerAbort` still has no caller |
| 18 | **Stations as data:** station types, a shop assortment per station | there is one pad prefab and one global drop list now | Not done | still one pad prefab and one drop list (`ShipStationCatalog`) and one `ShopCatalog` |
| 19 | **Run goal:** end, win and loss (`MaxLoops`), return to lobby | the run is endless now, nobody calls `ReturnToLobby()` | Partly | when the whole crew is dead everyone returns to the lobby and the host can start again (`PlayerLifeSystem`, `AllDeadRule`); there is no win; `MaxLoops` is 0 and only locks the launch |
| 20 | Late join, fuel and cargo capacity as M2 mechanics | joining after start is impossible now (`MapAlreadyStarted`) | Not done | joining a started run is still rejected (`ConnectionAuthenticator`); per-player models keyed by `PlayerKey` are in place for it |

## Decisions
- **Endless flight (item 5)** is solved in M3 by an obstacle zone and an event beyond its border, not by an autopilot or
  a timeout.
- **Rocks (item 17)** are cut until the obstacle-zone milestone.
- **Economy (item 2)** (starting balance, income, no drops) is postponed: the game is at a very early stage.
- **Voice chat** is not needed for now: playtests run in Discord.

## Done on 2026-10-06 (pull requests)
| PR | What | Items |
|---|---|---|
| #4 (open) | GrabModule: one hand model, one aim, third-person reach | 11 |
| #5 | Ship: install modules only in Build; all catalogs via Addressables | 3 |
| #6 | Player nameplate: synced display name over other players' heads | — |
| #7 | Shop: physical kiosk at every station | 3 |
| #8 | Ship: `ShipRunService` and `ShipBase` split into single-purpose classes | — |
| #9 | Shop: buy back a dead crewmate at the station kiosk | — |
| #10 | Ship: run state and sockets through generated network models | 20 (groundwork) |
| #11 | Ship: final stats synced to clients; base stats in config | 16 |
| #12 | Integration branch `feature/m1-all` (station and ship lines merged); a revived body no longer slides from the death spot | — |
| #13 (2026-10-10) | Modules as data: `ModuleDefinition` and one `ModuleCatalog` | 4, 15 |
