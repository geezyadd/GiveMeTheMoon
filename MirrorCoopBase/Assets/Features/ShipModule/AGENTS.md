# ShipModule — the ship core

Hull, flight, slots, seats, riders, stats and the run. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file.

## Assembly
`Features.ShipModule` (layer 4; references: Camera, Grab, CharacterMovable, FloatingController, GameFlow, Stats, Connection, NetworkModel, WeaponModule.Contracts, DebugModule.Contracts, QuickOutline, TextMeshPro): the ship core: hull, flight, slots, seats, riders, stats, the run; what the feature modules of layer 5 and the ship core need from each other is in `ShipModule/Scripts/Contracts/`: `IShipModuleCondition`, `IShipBreachCondition`, `IShipLandingRepair`, `IShipRunStartListener`, `IShipServerImpactEvents` + `ShipRockImpact` / `ShipRockImpactKind`, the server writers `IShipConditionWriter` (the synced condition, core adapter `ShipConditionWriter` over the bridge) and `IShipModuleWorkingState` (suspend / resume a socket's modifiers, adapter `ShipModuleWorkingState`), and the registry of enabled components `IComponentRegistry<T>` / `IReadOnlyComponentRegistry<T>` (one generic `ComponentRegistry<T>`, bound once per type by the module that owns the component: deck spawn points, hull damage points, tool racks, impact syncs; a scene component adds itself in `OnEnable`); the hull volume (`ShipHullVolume`, `ShipHullBox`) is in `Hull/` and the knockback the flight owns (`ShipKnockback`, `IShipKnockback`) in the core; the core references none of the ship feature modules.

The ship's assemblies (an arrow = references; split on 2026-10-25 so features do not meet in one installer, asmdef and
folder):
```
ShipImpactModule ──┐
ShipDamageModule ──┼──> ShipModule (core + Contracts/) ──> Grab, Camera, CharacterMovable, ... (layers 1-3)
TurretModule ──────┘
ShipDamageModule <── ShopModule, TooltipModule        GameCoreModule ──> all of them
```
The three feature modules do not reference each other: the damage module hears the impacts through
`IShipServerImpactEvents`, the turret reads `IShipModuleCondition`, both from the core's `Contracts/`.

Module pages: [ShipImpactModule](../ShipImpactModule/AGENTS.md), [ShipDamageModule](../ShipDamageModule/AGENTS.md), [TurretModule](../TurretModule/AGENTS.md).

## Flight
**How the code works now** (`ShipFlightSettings`, `ShipFlight`, `ShipRoute`,
`ShipRunService`):
- there are two flight modes (`ShipFlightMode`). The default is `TravelInSpace` (`ShipFlightConfig_Default.asset`):
  the ship really flies to the next station;
- on launch the route is chosen and the next pad spawns right away at the destination: a random direction inside a
  170° cone ahead (`ShipRoute.ChooseDestination`). The flight length is set in time in `ShipRunConfig`:
  seconds(loop) = `FirstFlightSeconds` (120) + loop × `PerLoopFlightSeconds` (120), capped by `MaxFlightSeconds`
  (0 = no cap; `ShipRunConfigValidator` checks the values at startup), and the destination lies seconds × `CruiseSpeed`
  from the hover point (1 680 m on loop 0, 8 400 m on loop 4 at 14 m/s): about 2 minutes of straight cruise at flight
  speed 1, faster engines shorten it (`ShipFlightStatService.EvaluateRouteWork`). Tests shorten it with their own copy
  of the config (`LoopSmokeShipRunConfigOverride`) or, in editor / development builds, with
  `IShipFlightLengthDebug.DebugUseFlightSeconds` (`DeckFeelHarness`);
- on takeoff the ship moves over 9 s to a hover point (+350 m forward, +100 m up) and **keeps the heading it had at
  the station**: the autopilot does not turn to the destination (`ShipFlight`);
- in cruise it flies along its heading at `CruiseSpeed` (14 m/s) × the `FlightSpeed` stat. The pilot in the helm
  seat turns the heading and sways the ship sideways. The destination lies up to 85° off the takeoff heading, so the
  crew finds it **by the navigator's sonar**: the station is a contact on the radar module's sonar console (see
  [SonarModule](../SonarModule/AGENTS.md)); without a navigator the crew finds it by eye (there is no minimap any more);
- the helm is **mandatory**: the ship's `control` socket has `HelmModule` as its starting module
  (`ShipSocket._startingModule`, data on `ShipPlatform.prefab`), every `ServerResetForBuild` installs it again and the
  module is not removable (`ModuleDefinition._isRemovable` = false, `ShipSocketRule.CanUninstall`). It is not sold in
  the shop and does not drop at stations. The branch "no control module" in `ShipFlightControl.ServerStep` stays as a
  guard; normal play never gets there;
- arrival is by distance: the ship is within `ApproachDistance` (70 m) of the destination (`ShipRoute.HasArrived`).
  In this mode `ShipTransit` only gives the HUD estimate, live from the current position: distance / cruise speed;
  while the nose points away from the destination the HUD shows the distance instead (`FlightTimePresenter`);
- on landing the ship eases onto the pad's landing point (`LandingSeconds` = 9 s) and the phase becomes `Build`;
- after a landing farther than 800 m from the world origin the world is shifted back to it
  (`ShipWorldShiftService.RecenterIfFar`, `WorldShift`; clients get it through `ShipRunDirector`);
- at every launch (`TravelInSpace` only) the server generates a **flight field** of rocks between the stations
  (`FlightFieldModule`) on a worker thread and publishes it in the `LiveField` model a tick after it is done, during
  the takeoff; every peer builds only the rocks near its camera from the placement list. The field follows world shifts and is cleared on landing, wreck / abort and the end of the run.
  The server checks the ship's hull against the field's rocks in every flight phase: a small rock breaks and damages
  the hull, a large one knocks the ship back (see "Rock collisions" in [ShipImpactModule](../ShipImpactModule/AGENTS.md));
- the old mode `HoverInPlace` is still in the code: the ship hangs at the hover point, progress is the `ShipTransit`
  timer and the next pad spawns at landing. It is chosen in the config asset or, in editor / development builds,
  through `IShipRouteDebug.DebugUseFlightMode`.

## Riding the ship
**Riding the ship.** In flight a player on the deck is not moved by physics: `ShipRider` binds it to the ship
(kinematic, collisions off) at a ship-local offset, and the owner walks and jumps in that offset. Each peer binds only
its own player; the others follow the owner's synced offset. When and how a player gets bound:
- at takeoff (`ShipBase.BeginTakeoff` on the server, `ShipBase.OnClientFlightChanged(true)` on a client)
  `ShipRiders.BoardRidersOverDeck` boards every player over the deck, from the nose to the stern: at most
  `BoardingEdgeTolerance` (0.5 m) outside the walk boxes and `BoardingMinHeight`..`BoardingMaxHeight` (-0.5..6 m) above the
  deck surface under it, so a player in a jump at the lever rides too;
- the safety net: every frame in flight `ShipRiders.CatchDeckRiders` checks the local player (from
  `CharacterMovableModel.LocalMovable`, not only on the ride volume's trigger enter) and binds it within a frame when it
  stands or comes down over the walk boxes, `CatchMinHeight`..`CatchMaxHeight` (-0.5..1.5 m) above the deck surface under
  it and not rising faster than `CatchMaxRiseSpeed` (0.15 m/s). A standing player's position is 1 m above the deck surface;
- all heights come from `ShipDeckRideRule`, measured from the deck surface under the player, not from the ship's pivot;
- a client that spawns the ship while it flies gets the flight state through the `ShipPoseSync._flying` hook: Mirror's
  generated SyncVar deserialization calls the hook on the first spawn too, whenever the value differs from the default;
  the same holds for `ShipRider._syncedRideShip` of a player already riding;
- walking or jumping past the deck edge in flight still drops the player off the ship (`DropOffDeck`, with
  `_confineRidersToDeck` off) and it cannot be caught again for `ReboardDelaySeconds` (0.5 s); the safety net then catches
  it only if it is over the deck again;
- in editor and development builds every bind, release and refusal is logged with `[ShipRide]` (`Debug/ShipRideDiagnostics`,
  compiled only under `UNITY_EDITOR || DEVELOPMENT_BUILD` together with its call sites):
  who, where on the deck (ship-local, height above the deck), which check refused, flying on this peer, host / client;
  a refusal only when its reason changes, at most once a second per player.
- the ride's vertical motion (`RideVertical`) is the station one: the same hover spring formula as
  `FloatingController` (`HoverSpring`, values from the player's own `FloatingController.Spring`), so a landing sags and
  springs back as on the station, and the same jump, coyote time, jump buffer and air gravity (`JumpAndAirGravity` with
  `JumpTuning` from `PlayerStatsConfiguration`, shared with `CharacterMovableBase`). It runs in fixed physics steps
  (`Time.fixedDeltaTime`) inside the frame and is shown interpolated, so it feels the same at any frame rate and never
  sinks below the deck; the others see it through the synced offset (`CmdSetRideOffset` / `RideOffset`).

## Loop status (2026-10-11)
- modules stay on the ship after a normal landing. `ServerResetForBuild` resets every socket to its starting module
  (the helm) or to empty only at the
  start and end of a run and on a wreck (`ShipRunService.FinishAtCurrentPose`, only when the next pad is missing; the unused `ServerAbort` was
  removed on 2026-10-25);
- modules are installed and removed only in the `Build` phase (`ShipSocketRule`);
- modules still drop for free on every arrival at a station (`ShipStationCatalog.Drops`,
  `ShipStationDropService.SpawnDrops`), so the shop is not needed yet (a stage preset turns the drops off, see
  [StagePresetModule](../StagePresetModule/AGENTS.md));
- the final ship stats are synced to clients (`ShipStats` model) and gameplay reads every one of them: `FlightSpeed`
  (cruise speed, base 1: the large engine +1, the rocket engine +0.5, the propeller a very small +0.08),
  `DodgeRange` (side sway), `Hull` and `Armor` (see "Ship hull" in Key places below) and `SonarRange` (the sonar's range in metres, base 0,
  the radar module +800; see [SonarModule](../SonarModule/AGENTS.md)); no module changes `DodgeRange`, `Hull` or `Armor`. The unused `Thrust` and
  `Handling` stats are removed (2026-10-25);
- **ship hull:** `ShipStatType.Hull` is the hull HP, an accumulative stat (current + max, start 100/100 in
  `ShipAccumulativeStatsConfiguration_Default`). Only `ShipHullService.ServerApplyDamage(ShipDamage)` (`IShipHull`)
  takes it down; `Armor` is a damage reduction (`ShipHullConfiguration`: 1 % per point, at most 75 %; base armor 0).
  The current and max hull are synced in the `ShipStats` model (`Hull`, `HullMax`), so a player joining at a station
  sees the right value; every player has a hull bar in the game HUD that flashes on a hit. The hull is full at the
  start of a run (every `ServerResetForBuild`) and is repaired for free on every landing while
  `ShipHullConfiguration.RepairOnLanding` is on (default). A module modifier on `Hull` raises the max and adds the same
  amount to the current hull; taking it off lowers the max and only clamps the current hull to it (never heals).
  Hull 0 sends the whole crew to the lobby once, through the same exit as "the whole crew is dead"
  (`IShipHullEvents.OnDestroyed` -> `PlayerLifeSystem`, after `AllDeadReturnDelay`), not through a `Wreck`.
  Small rocks the ship flies into deal hull damage (see "Rock collisions" in [ShipImpactModule](../ShipImpactModule/AGENTS.md)); large rocks deal none by
  default (`ShipRockImpactConfiguration.LargeRockDamage` = 0);

## Known limits
- A client that spawns the ship while it flies is not tested: a join in flight is turned away, so no test reaches it;
  it relies on the SyncVar hooks above firing on the first spawn. The server kills a fallen player by its server
  position (`FallKillSystem`); between the owner binding itself and its `CmdBeginRide` arriving (one round trip at the
  start of a takeoff) the server still sees it standing where it was, which is far above the 30 m kill depth.
- **No world shift in flight.** On loop 4 the ship is ~8-9 km from the origin before the landing recenter (float
  step there is ~1 mm); deck and camera jitter at that distance is not checked yet.

Known issue: in the first cruise of a `DeckFeelHarness` run the player jumps about 136 m in one frame, dies and the
crew returns to the lobby, so the harness ends with "FAIL ship cannot launch". Seen on 2026-10-06 and again on
2026-10-16, also on the base commit. The `[ShipRide]` log shows the cause: the harness's walk legs in cruise were written
for a confined deck and walk the player off the narrow nose of the deck, which drops it off the ship by design
(`_confineRidersToDeck` off). It is not the rider binding bug; the harness is not fixed yet.

## Key places
| What | Where |
|---|---|
| Ship, flight, riders | `Assets/Features/ShipModule/Scripts/`: `ShipBase` (ties its parts: `ShipDeckGeometry`, `ShipModules`, `ShipRiders`, `ShipSeats`, `ShipFlightControl`, `ShipDeckCargo`), `ShipFlight`, `ShipPoseSync`, `ShipRider` (+ `ShipDeckRideRule`, `ShipRideDiagnostics`, see "Riding the ship" above), `ShipRunDirector`, `ShipRunService` (phase machine) with `ShipStationPads`, `ShipStationDropService`, `ShipRoute`, `ShipFlightStatService`, `ShipWorldShiftService`, `ShipRunRiderService`, `ShipRunBindingModel` |
| Flight and landing settings | `ShipFlightSettings` (+ `ShipFlightConfig_Default.asset`), `ShipRunConfig` |
| Modules and slots | a module is one `ModuleDefinition` asset (`ShipModule/GameResources/Modules/`: id, slot type, item prefab, slot view, name, price, stat modifiers); `ModuleCatalog` lists them all and resolves a module id (`ModuleCatalogValidator` checks it at startup); `ShipItem`, `ShipSocket` (+ `ShipSocketRule`, state in the `ShipSockets` model), `Ship*Interactable`; catalogs in `ShipModule/GameResources/Configurations/` (Addressables) |
| Stats | `Assets/Features/StatsModule/`, `ShipStatType` (`DodgeRange` 2, `Armor` 4, `FlightSpeed` 5, `Hull` 6, `SonarRange` 7: assets store the numbers, so never renumber; 1 and 3 were the removed `Thrust` / `Handling`, do not reuse them, and `ShipStatsConfigurationValidator` / `ModuleCatalogValidator` reject a stat number the enum does not define), `ShipStatEntity`, `ShipStatSheet` (server: base values from `ShipAccumulativeStatsConfiguration` plus module modifiers; `Hull` is the only accumulative ship stat: modifiers raise its max), synced `ShipStats` model; player movement stats are `PlayerStatType` / `PlayerStatsConfiguration` in `CharacterMovableModule/Scripts/PlayerStats/` |
| Ship hull | contracts in `GameCoreModule/Scripts/Contracts/` (`IShipHull`, `ShipDamage`, `ShipDamageCause`, `IShipHullEvents`: any module can deal damage or listen); `ShipModule/Scripts/Hull/`: `ShipHullService` (the one damage entry and the landing repair), `ShipHullDamageRule` (armor), `ShipHullStat` (the bound ship's Hull / Armor stats, publishes `ShipStats`), `ShipHullEventBus`, `ShipHullConfiguration` (+ `ShipHullConfigurationValidator`); HUD bar `ShipHullView` / `ShipHullPresenter` in `LobbyModule` (game HUD; flash timing in `ShipHullHudConfiguration`); hull 0 -> lobby in `PlayerLifeSystem` with `CrewLobbyReturnSchedule`; the debug window's Ship → "Hit the hull" hits it with `DebugDamage` (or the typed damage), "Destroy the hull" takes it to 0 (host, editor / development builds, `ShipHullDebugSystem` / `IShipHullDebug`) |
| World shift | `WorldShift` (count and total of the shifts), `ShipWorldShiftService`, `ShipPoseSync`; content outside the ship module follows a shift through `IWorldShiftFollower` (server: `ShipWorldShiftService.ApplyWorldShift`, remote clients: `ShipRunDirector.RpcShiftWorld`) |
| Cargo on the deck | `ShipDeckCargo`: items lying on the deck are attached to the ship and travel with it |
| Seats and the helm | Seats: `ShipSitInteractable` seats a player (`ShipSeats`); what the seated player does lives in the slot view's `ISeatStation` (a turret: `ShipTurretStation`; the helm: `ShipHelmStation`): a new seat role is a new station, not a branch in the rider or the seats. There is no seat role enum any more (`ShipSeatRole` is gone): `ShipSeat` is only the sit point. **Occupancy** is keyed by the player's stable `PlayerKey` (rule "Player data and rejoin" in the [root AGENTS.md](../../../../AGENTS.md)): `ShipSocketState.OccupantKey` in the synced `ShipSockets` model, written by the server through `ShipSeatRule` (one occupant, only the occupant stands up); every `ShipRider` carries its key in a SyncVar the server writes at spawn (`ShipRider.Key`), and any peer finds the player of a key with `IShipRiders.TryFindByKey` (a lookup in `ShipRiderKeyModel`, where every rider registers itself while spawned on that peer, riding or not). A disconnecting player frees its seat (`ShipRider.OnStopServer` -> `ShipBase.ServerRemoveRider`), so a player who comes back with the same key stands on the deck at a spawn point and the seat is free. **The helm** (`HelmItemView` with `ShipHelmStation`, `ShipModule/Scripts/Helm/`): on the seated owner's peer the move stick's sideways axis (A / D) steers; the host's flight reads its own stick at once, a client pilot sends it at most every `ShipFlightSettings.HelmSteerSendSeconds` (0.03 s) through its rider's generic seat input (`ShipRider.OwnerSendSeatInput` -> `ShipSeats.ServerReceiveSeatInput`, which hands it only to the station of the seat the sender holds, `ISeatStationOwnerInput`); `HelmSteer` keeps that steer for the current pilot only, so a pilot who leaves or stands up steers nothing. The flight asks `IShipSteering` (`ShipSteering`: the first slot view implementing `IShipSteeringStation` with a pilot) for the steer, and the client pose sync asks it whether the local player steers (no interpolation delay then). Jump stands up from any seat (`ShipRider`) |
| Deck debugging | `ShipModule/Scripts/Debug/` (editor / development builds only): `DeckFeelHarness` (deck feel measurements), `ShipStatsDebugOverlay` (also the hull), `ShipHullDebugSystem`, `WeaponDebugTargetSystem`, and, in their own modules, `ShipImpactModule/Scripts/Debug/ShipRockImpactDebugSystem` (see [ShipImpactModule](../ShipImpactModule/AGENTS.md)) and `ShipDamageModule/Scripts/Debug/ShipDamageDebugSystem`: each registers its actions in the debug window (see [DebugModule](../DebugModule/AGENTS.md)); `ShipDebugActionConditions` holds their availability checks (host, a ship, in flight). There are no debug keys any more |

## Tests
- `Features.ShipModule.Editor.Tests`
- EditMode guard: `ShipSocketIdTests` (every ship socket has a unique id).
- The helm's and the seats' `HelmSteerTests` (the host pilot's stick, a client pilot's sent steer and its send cadence, no steer left from a pilot who stood up) and `ShipSeatRuleTests` (one occupant by key, only the key holds the seat, a rejoined player's key, a freed seat equals a never-seated one).
