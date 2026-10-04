# Roadmap: M1, "boring but perfect loop"

Written 2026-10-01 from a code investigation. M1 goal: station → buy → install → flight → next station.
The loop works in co-op without bugs and **feels good**. Game context is in `AGENTS.md`.

## P0. The loop must be what was designed (M1 does not work without this)
| # | Task | Why (current state) |
|---|---|---|
| 1 | **Modules stay on the ship between stations.** A normal landing goes straight to Build, without a reset | `FinishAtCurrentPose` → `ServerResetForBuild` calls `ClearInstalledModules()`, places wreckage and teleports the ship 18 m: all upgrades are lost at every station |
| 2 | **M1 economy.** A proper starting balance, income for arriving at a station (config). No or almost no free drops | there is no income; about 8 modules drop for free at every station (`ShipStationCatalog.Drops`), so the shop is not needed |
| 3 | **Shop and installation only at a station, in the Build phase.** Checked on both client and server | the shop opens in flight; `CmdPurchase` checks nothing; `ShipSocket.CanAccept` allows installing modules in flight |
| 4 | **Removing a module returns the item via the shared catalog** | `ShipUninstallInteractable` looks for the prefab only in the drop list, so a shop-only module silently cannot be removed |
| 5 | **A flight without a helm does not last forever.** Needs autopilot to the target or a mandatory helm, plus a timeout | without a helm the nose is not turned to the target, alignment ~0.09, the flight takes about 11 times longer; turning away from the target only increases the time |
| 6 | **Items on the deck travel with the ship** | the ship moves via transform; modules and cargo lying on the deck most likely stay at the old station (not verified) |
| 7 | **Co-op: the whole loop verified with two players** (ParrelSync). The helm is released when the pilot disconnects. Seating is synced on the client | a pilot disconnect blocks control until landing; on a remote client the player does not move into the seat (found by audit) |
| 8 | Cleanup: old drops and wreckage do not pile up; shop text about the Helm fixed | drops keep hanging when the old pad is removed; in the shop the Helm says "Required to launch" although the slot is not mandatory |

## P1. Feel ("satisfying")
| # | Task | Why |
|---|---|---|
| 9 | **Sense of speed in flight and a decision on the flight model.** Parallax or scrolling environment (stars, clouds, dust, distant objects) | in cruise the ship hangs in place, only rocks move, speed is not visible |
| 10 | **Basic sound:** footsteps, engines (pitch depends on thrust), lever, module install and removal, purchase, takeoff and landing, UI | there is not a single `AudioSource` in the game |
| 11 | **Interaction feedback:** tooltips (in progress), a "2 engines needed to take off" hint, effect and sound on install, lever animation, item throw | there is only an outline now; Release drops the item with zero velocity |
| 12 | **Smooth landing and transition to Build:** no teleport and no sudden wreckage; players stay on the deck | landing currently looks like a jerk |
| 13 | **HUD:** station number, phase, ETA, what blocks takeoff | there is only a "to platform" timer and the balance |
| 14 | **Camera:** a single source (`Camera.main` and `OutputCamera` are mixed now), light shake and FOV on takeoff and landing, engine VFX | all of this is missing |

## P2. Foundation for M2–M4 (content)
| # | Task | Why |
|---|---|---|
| 15 | **Modules through data:** one module catalog (shop, drops, removal, views), each module has a list of `StatModifier` in data | a new module now needs edits in ~8 places and a `switch` on Engine (`ServerOnModuleInstalled`, `ShopItemStatsService`, `CanRemoveModule`) |
| 16 | **Stats really affect the game:** Thrust, Handling, Armor, `IFlightStatContributor`; real values visible on the client | only `FlightSpeed` works; modifiers exist only on the server |
| 17 | **Obstacles and events through an interface and spawn tables.** Rock colliders, damage, `ServerAbort` | rocks are harmless and hardcoded in `ShipRunService`; `ServerAbort` is never called |
| 18 | **Stations as data:** station types, a shop assortment per station | there is one pad prefab and one global drop list now |
| 19 | **Run goal:** end, win and loss (`MaxLoops`), return to lobby | the run is endless now, nobody calls `ReturnToLobby()` |
| 20 | Late join, fuel and cargo capacity as M2 mechanics | joining after start is impossible now (`MapAlreadyStarted`) |
