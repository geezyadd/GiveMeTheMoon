# GiveMeTheMoon — project context

Read before any task in this repository: humans, Claude and worker agents.

## The game
A co-op **friendslop** in the vein of Peak, YAPYAP and Lethal Company. A crew of players flies a platform-ship from
station to station and upgrades the ship with modules along the way.

### Core loop (current focus)
1. Start at the first station with base money, enough for the simplest upgrades.
2. **Management phase.** Players buy modules at the station's shop kiosk (aim + **E**; bought items appear on its delivery pad) and install them in ship slots: engines,
   propeller, radar, more later. The helm is always on the ship (see below). The balance is shared by the crew.
3. **Flight phase.** The ship flies to the next station while players stand and walk on the deck.
4. Arrival at the next station: new purchases and upgrades, then the loop repeats.

Intent: in flight the ship stays almost still and the world moves past it, so players on deck get less jitter. This
is not final yet and needs evaluation. **How the code works now** (`ShipFlightSettings`, `ShipFlight`, `ShipRoute`,
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
  "Sonar" in Key places); without a navigator the crew finds it by eye (there is no minimap any more);
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
  the hull, a large one knocks the ship back (see "Rock collisions" in Key places);
- the old mode `HoverInPlace` is still in the code: the ship hangs at the hover point, progress is the `ShipTransit`
  timer and the next pad spawns at landing. It is chosen in the config asset or, in editor / development builds,
  through `IShipRouteDebug.DebugUseFlightMode`.

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
The loop runs from start to end, but it does not match the design yet:
- modules stay on the ship after a normal landing. `ServerResetForBuild` resets every socket to its starting module
  (the helm) or to empty only at the
  start and end of a run and on an aborted flight (`ShipRunService.FinishAtCurrentPose`; nothing calls `ServerAbort`
  yet);
- modules are installed and removed only in the `Build` phase (`ShipSocketRule`);
- modules still drop for free on every arrival at a station (`ShipStationCatalog.Drops`,
  `ShipStationDropService.SpawnDrops`), so the shop is not needed yet (a stage preset turns the drops off, see
  "Stage presets");
- the only income is the target balloons of the flight field (see "Target balloons" in Key places: 20 / 50 / 200 per
  balloon, 6-10 balloons per flight on loop 0); the rest is the starting balance (`WalletConfiguration`, 1 000 000 now;
  a stage preset brings its own);
- the final ship stats are synced to clients (`ShipStats` model) and gameplay reads every one of them: `FlightSpeed`
  (cruise speed, base 1: the large engine +1, the rocket engine +0.5, the propeller a very small +0.08),
  `DodgeRange` (side sway), `Hull` and `Armor` (see "Ship hull") and `SonarRange` (the sonar's range in metres, base 0,
  the radar module +800; see "Sonar"); no module changes `DodgeRange`, `Hull` or `Armor`. The unused `Thrust` and
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
  (`IShipHullEvents.OnDestroyed` -> `PlayerLifeSystem`, after `AllDeadReturnDelay`), not through `ServerAbort` / `Wreck`.
  Small rocks the ship flies into deal hull damage (see "Rock collisions" in Key places); large rocks deal none by
  default (`ShipRockImpactConfiguration.LargeRockDamage` = 0);
- **module breakdowns and hull breaches (the mechanic's job):** a rock impact may break the installed module nearest to
  the hit and open a breach at the nearest free hull damage point (see "Ship damage and repair" in Key places). A broken
  module stays installed but does not work (its stat modifiers are suspended, a broken turret does not fire); an open
  breach leaks hull HP in flight. A player repairs a module with the **wrench** and a breach with the **hammer** by
  holding the interact key on it; both tools lie on the rack on the deck at the start of a run and are sold in the shop.
  Everything is repaired for free on landing (with the hull, `RepairOnLanding`);
- a crew that does not steer to the destination can fly forever: the autopilot only keeps the heading and there is no
  timeout;
- the run has no goal: it ends only when the whole crew is dead or the hull is destroyed (`PlayerLifeSystem` →
  lobby);
- players can join or come back in the lobby or while the ship stands at a station (`Build`); in flight, in a wreck,
  while the map changes and with a full crew they are turned away with a reason the menu shows (see "Player data and
  rejoin");
- there is no sound except the minigun turret's placeholder sounds (see "Weapons" in Key places); the only VFX are
  the turret's, the rock break and the rock impact effects (the impact effect's `AudioSource` is the hook for a
  sound, empty for now).

### Known limits
- **The minigun breaks field rocks** (see "Weapon damage on rocks" in "Breakable rocks"); a hit rock has no damaged
  look between hits (only the flash and shake while it is struck), and a client's rock hits were checked only on the
  host with the host as the gunner, not with a second client process. A player who joins at a station
  sees an installed turret from `ShipSockets` and its gunner from the socket's `OccupantKey` (the gunner's `PlayerKey`); the turret shows the
  gunner's last synced aim, and a gunner who leaves frees the seat and stops firing. Remote fire was checked on the
  host with the loop test's bot as the gunner, not yet with a second client process.
- **Target balloons** are checked on the host only (`LoopSmokeTest`): `CoopTwoProcessTest` does not compare the popped
  list or a balance after pops yet, and a client gunner popping a balloon was not run with a second process. The pop
  sound hooks have no clips. A balloon is a sphere in the data; the string and the crate below it are look only.
- **Rock collisions use spheres.** The server knows a rock only as a sphere from the field data (the turned mesh fits
  inside it with room to spare), so a large rock stops the hull at `LargeContactRadiusShare` (0.8) of that sphere: the
  ship may stop a few metres before the visible rock or touch a corner that sticks out. The hull is boxes
  (`ShipHullVolume`), not the deck colliders. A client sees a hit through the ship's pose (interpolated like any flight
  motion) and the impact RPC; the two-player test does not check rock hits yet.
- The autopilot keeps its heading after a large hit, so a crew that does not steer bounces off the same rock again
  after the cooldown, and in cruise the ship takes hits from the rocks on its way: the tests' short flights stay in the
  empty station zones.
- The field has no "left the field" event.
- **The sonar reads the published field.** Its rock and balloon sources use the peer queries (`IPeerFieldRockQuery`,
  `IPeerFieldTargetQuery`) over the live field model, so a navigator on a client sees the same contacts as one on the
  host, but only once the field is published (about a second into the takeoff on a loop 9 field); the server's checks
  keep the generated field.
- A client that spawns the ship while it flies is not tested: a join in flight is turned away, so no test reaches it;
  it relies on the SyncVar hooks above firing on the first spawn. The server kills a fallen player by its server
  position (`FallKillSystem`); between the owner binding itself and its `CmdBeginRide` arriving (one round trip at the
  start of a takeoff) the server still sees it standing where it was, which is far above the 30 m kill depth.
- **No world shift in flight.** On loop 4 the ship is ~8-9 km from the origin before the landing recenter (float
  step there is ~1 mm); deck and camera jitter at that distance is not checked yet.
- A host and a real second client seeing the same field number, seed and placement count is checked by
  `CoopTwoProcessTest` (see "Two-player test"); the same field after a world shift in flight is covered only by the
  design (one placement list, the origin in the model, pads-like shift of Mirror objects) and by the host checks of
  `LoopSmokeTest`.

Known issue: in the first cruise of a `DeckFeelHarness` run the player jumps about 136 m in one frame, dies and the
crew returns to the lobby, so the harness ends with "FAIL ship cannot launch". Seen on 2026-10-06 and again on
2026-10-16, also on the base commit. The `[ShipRide]` log shows the cause: the harness's walk legs in cruise were written
for a confined deck and walk the player off the narrow nose of the deck, which drops it off the ship by design
(`_confineRidersToDeck` off). It is not the rider binding bug; the harness is not fixed yet.

M1 priorities are in `Docs/ROADMAP.md`.

### Main principle
**The game must be smooth and pleasant.** Every action should feel good and satisfying: walking on the deck, picking
up and installing a module, takeoff, landing, buying. No jitter, no jerks, no falling through. The platform underfoot
is the core mechanic and has to work perfectly.

### Upgrade logic (game design)
An upgrade should obviously answer a problem the player sees in flight:
- bugs and enemies get in the way → guns;
- unclear where to fly → a better radar;
- flights take too long → engines and rocket launchers;
- not enough fuel or cargo capacity → tanks or cargo modules.

The player sees the problem, opens the shop and knows what to buy. The shop shows module stats so the choice is
informed.

## Milestones
| Milestone | Goal |
|---|---|
| **M1. Boring but perfect loop** (now) | station → buy → install → flight → next station; everything works bug-free and feels good, in co-op too |
| M2. Economy and meaningful upgrades | income, prices, module stats really affect flight (speed, fuel, cargo, armor) |
| M3. Eventful flight | rocks to dodge, enemies, events; flights rarely repeat |
| M4. Content | more modules, stations, events, variety |

We are doing **M1** now. Do not add M3 or M4 features, but keep the architecture extensible: a new module, stat,
obstacle or event should be added through data (ScriptableObject catalogs), not through new `switch` statements on
types.

## Tech
- Unity 6 (6000.0.67f1), project in `MirrorCoopBase/`.
- Networking: **Mirror**, host and clients. The server is authoritative: money, module installation, flight.
- DI: **Zenject**. Installers are in `Assets/Features/GameCoreModule/Scripts/Installers/`, configs come from
  Addressables (`ConfigurationInstaller`).
- UI: in-house MVP (`Assets/Features/MvpModule/`): `WindowBehaviour`, `ViewBehaviour`,
  `PresenterBehaviour<TView>`, windows are registered in `WindowsModuleInstaller`.
- Input: New Input System and the generated `IInputService` wrapper
  (`Assets/Features/InputModule/Realization/`). The code is generated; do not edit it by hand.
- **Debug actions: never add a debug hotkey.** A debug / cheat action is registered in `IDebugActionRegistry`
  (`Features.DebugModule.Contracts`) by the module that owns it, where the action lives, and shows up as a labelled
  button in the in-game debug window (see "Debug window" in Key places). One registration per action, no window edit.
- Feature modules live in `Assets/Features/<Module>/{Scripts,GameResources}`.

### Assemblies (asmdef)
- **A new module gets its own asmdef right away** in `Features/<Module>/Scripts/`, with name and root namespace
  `Features.<Module>`. References to other assemblies (Mirror, Zenject, Unity.InputSystem, modules) are explicit.
- An assembly with an asmdef cannot see `Assembly-CSharp`. If a module needs a module that still has no asmdef,
  convert that dependency first; if that is a lot of work, the new module stays without an asmdef for now and this
  goes into NOTES.
- Editor code and tests inside a module with an asmdef get **separate** asmdefs (`Features.<Module>.Editor`,
  Editor platform only; `Features.<Module>.Tests` as a test assembly). An `Editor/` folder inside an asmdef is not
  special: without its own asmdef its code ends up in the runtime assembly and breaks the build.
- **No cycles between modules.** If two modules need each other, the shared contract (interface, event, constants)
  moves to a lower module instead of a mutual reference.
- `GameCoreModule` is the composition root (installers); nothing references it. Types shared by everyone
  (`IGameplaySession`, `SceneNames`) must live in a separate lower assembly, not in GameCore.
- Module assemblies by layer (an assembly references only lower layers; module references in brackets):
  1. `Features.GameCoreModule.Contracts` (`IGameplaySession`, `SceneNames`; the run's contracts for the flight field:
     `IFlightRouteListener` + `FlightLaunch`, `IWorldShiftFollower`, `IShipPositionSource`, `ILocalViewpointSource`),
     `Features.AddressablesConstantGeneratorModule` (`Address_g.cs`; editor tools and tests load a configuration at its
     address with `AddressableConfigurationLoader` in `Features.AddressablesConstantGeneratorModule.EditorLoading`), `Features.MvpModule` (AssetLoader),
     `Features.FloatingControllerModule`, `Features.MatrixModule` (no references: generic grid `IMatrix<T>` with
     shape insert and A* path finding, copied from MuffinProject's `MatrixSubmodule`; keep its public API),
     `Features.PlayerProfileModule.Data` (NetworkModel; the generated `PlayerProfile` model and
     `IPlayerProfileSource`), `Features.CrewInviteModule.Data` (NetworkModel; the generated `CrewRoster` model),
     `Features.FlightFieldModule.Contracts` (NetworkModel; `FlightFieldModule/Contracts/`: the breakable rock contracts
     `FieldRockId`, `FieldRockDescription`, `FieldRockDestroyed`, `IFieldRockQuery`, `IFieldRockBreaker`,
     `IFieldRockEvents`, and the target balloon contracts `FieldTargetId`, `FieldTargetDescription`,
     `FieldTargetPopped`, `FieldTargetPopCause`, `IFieldTargetQuery`, `IFieldTargetPopper`, `IFieldTargetEvents`, and
     the peer queries `IPeerFieldRockQuery` / `IPeerFieldTargetQuery` (every peer, over the published field), so the
     ship, the weapons, the shop and the sonar use them without referencing the field module);
     `Features.WeaponModule.Contracts` (NetworkModel: `IWeaponTarget`, `WeaponHit`, `IWeaponTargetCollider`,
     `IWeaponTargetRegistry`, `IWeaponTargetSource`, `IWeaponImpactFeedback`; a weapon target such as a field rock
     implements them without referencing the weapon module; separate from `GameCoreModule.Contracts` so that one stays
     free of NetworkModel / Mirror);
     `Features.CameraModule.Contracts` (no references: `CameraModule/Contracts/`: `ICameraShakeService`,
     `ICameraShakeHandle`, `CameraShakeProfile`, so the weapons and the ship shake the camera without referencing the
     camera module);
     `Features.EffectPoolModule` (no references: `SelfDisablingEffectPool<TEffect>` over `ISelfDisablingEffect`, the
     pool of effects that disable themselves when over; the rock break and the rock impact effects use it),
     `Features.DebugModule.Contracts` (no references: `DebugModule/Contracts/`: `IDebugActionRegistry`,
     `DebugActionDefinition`, `DebugActionValue`, `DebugSections`, `DebugActionReasons`; the ship, the flight field,
     the player life and the shop register their debug actions through it),
     `Features.SonarModule.Contracts` (no references: `SonarModule/Contracts/`: `ISonarContactSource`, `SonarContact`,
     `SonarContactKind`, `ISonarCondition`, so a module adds its own sonar contacts or says whether the radar works
     without referencing the sonar), `Features.SonarModule.Data` (SonarModule.Contracts, NetworkModel, Mirror, Zenject:
     the generated `SonarConsole` model with `SonarMark`, and the sonar's local models),
     third-party `QuickOutline` and `MiniMapModular`
     (`Mini Mapa(Radar)`) also have their own asmdef; nothing references `MiniMapModular` any more (the package files
     stay in the project).
  2. `Features.CharacterMovableModule` (Contracts, FloatingController, Address, Stats, Input),
     `Features.GameFlowStateMachineModule` (Contracts, SceneLoader),
     `Features.FlightFieldModule` (Matrix, Zenject, Contracts, FlightFieldModule.Contracts, WeaponModule.Contracts,
     EffectPool, NetworkModel, Mirror, DebugModule.Contracts: the flight field model and its
     generation in pure C#, the `LiveField` model, the server service and the local building; it does not reference
     the ship module, the run talks to it through the Contracts; its editor tools are
     `Features.FlightFieldModule.Editor`),
     `Features.WeaponModule` (WeaponModule.Contracts, CameraModule.Contracts, NetworkModel, Zenject, Mirror: the weapon core, its one network
     path `WeaponHolderSync` and the client feel, see "Weapons" in Key places; it does not reference the ship, the
     flight field or the player; its inspector part picker is `Features.WeaponModule.Editor`).
  3. `Features.CameraModule` (CharacterMovable, Contracts, CameraModule.Contracts, Input) → `Features.GrabModule` (Camera, CharacterMovable,
     Contracts, NetworkModel) → `Features.DebugModule` (DebugModule.Contracts, Mvp, Camera, GameFlow, Input, Zenject,
     UnityEngine.UI: the debug window, see "Debug window" in Key places; compiled only in editor / development builds
     through its asmdef's define constraint `UNITY_EDITOR || DEVELOPMENT_BUILD`, so GameCore installs it inside the same
     `#if`).
  4. `Features.ShipModule` (Camera, CameraModule.Contracts, Grab, CharacterMovable, FloatingController, GameFlow, Stats, Connection,
     NetworkModel, WeaponModule, WeaponModule.Contracts, FlightFieldModule.Contracts, EffectPool, Input, TextMeshPro),
     `Features.MenuModule` (GameFlow, Mvp, Connection),
     `Features.PlayerProfileModule` (PlayerProfileModule.Data, Camera, NetworkModel, TextMeshPro).
  5. `Features.PlayerLifeModule` (Ship, Grab, Camera, Mvp, NetworkModel, Connection, PlayerProfileModule.Data),
     `Features.LobbyModule` (Ship, Grab,
     GameFlow, Mvp, Connection).
  6. `Features.StagePresetModule` (StagePresetModule.Data, Ship, Shop, FlightField, Mvp, Connection, NetworkModel:
     the playtest stage presets, see "Stage presets"; its synced model is in `Features.StagePresetModule.Data`, layer 1),
     `Features.ShopModule` (PlayerLife, Ship, Camera, GameFlow, Mvp, NetworkModel, PlayerProfileModule.Data,
     FlightFieldModule.Contracts for the balloon rewards) →
     `Features.TooltipModule` (Shop,
     Ship, Grab, Mvp) → `Features.CrewInviteModule` (CrewInviteModule.Data, Tooltip, Grab, Connection, NetworkModel: the "Hire
     crew" terminal; above Tooltip because it plugs a state tooltip into it through `ITooltipStateProvider`),
     `Features.SonarModule` (SonarModule.Contracts, SonarModule.Data, Ship, Grab, Camera, CharacterMovable, Mvp,
     PlayerLife, FlightFieldModule.Contracts, GameCoreModule.Contracts, GameFlow, NetworkModel, Mirror, Zenject, Input,
     UnityEngine.UI: the navigator's sonar console, see "Sonar" in Key places; its own module rather than more radar
     code in the ship module, because it needs the UI, the player's life and the field contracts the ship must not
     depend on, and the ship stays unaware of it).
  7. Composition root: `Features.GameCoreModule` (all modules) and `Features.BootstrapersModule` (Contracts,
     GameFlow, SceneLoader).
  Tests: `Features.DebugModule.Editor.Tests` (`DebugActionRegistryTests`: a registered action is found in its
  section, a disposed registration removes it once, availability is asked only while the panel is open and a change is
  told once, an unavailable action does not run, a value action gets the typed value), `Features.CameraModule.Editor.Tests` (`CameraShakeTests`: a weaker shake of one profile never cuts a
  stronger one, a stronger one restarts it, two profiles add up, a continuous + a one-shot shake sum, a one-shot fades
  to zero, a length scaled by strength, the continuous intensity eases over the profile's seconds, stop and clear),
  `Features.CharacterMovableModule.Editor.Tests`, `Features.GrabModule.Editor.Tests` (`HoldUseProgressTests`: the
  hold completes after its time, resets on release, walking away, another item in the hand or an unusable target),
  `Features.PlayerLifeModule.Editor.Tests`,
  `Features.StagePresetModule.Editor.Tests` (shipped presets pass the startup validator and fit ShipPlatform's sockets,
  Early / Mid / Late loops and money against shop prices, broken presets throw, the applier's run values),
  `Features.ShipModule.Editor.Tests`, `Features.ShopModule.Editor.Tests` (incl. `FieldTargetRewardSystemTests`: a pop pays its reward once), `Features.GameCoreModule.Editor.Tests`
  (configuration loading and module catalog guards), `Features.SonarModule.Editor.Tests` (rock contacts in range,
  out of range, a debris patch only from enough small rocks and found again by id at flight height, a broken rock's
  contact gone, rock id round trip; the station in flight whatever its distance and none at a station; the mark rule:
  navigator only, an existing and markable contact, the limit, a marked contact renewed, expiry; the console rule:
  one navigator, reach; balloons in range found again by id, a popped one gone; a mark over its object for its kind's time; the scope with the nose up and the station pinned to the edge, ping cooldown, wave and echo
  fade; world markers over the object, on the edge with the arrow, behind the camera), `Game.Connection.Editor.Tests`,
  `Features.PlayerProfileModule.Editor.Tests`, `Features.CrewInviteModule.Editor.Tests`, `NetworkModelModule.Editor.Tests`, `Features.MatrixModule.Editor.Tests`, `Features.WeaponModule.Editor.Tests` (auto fire cadence and spin-up / spin-down at
  any frame rate, hitscan spread bounds and outcomes, every `WeaponHitRule` branch and the fire rate budget, weapon catalog guard, `WeaponTargetRegistryTests`: target sources)
  (matrix creation, neighbours, full-shape insert incl. L-shaped shapes, path finding incl. a 40x40 timing check),
  `Features.FlightFieldModule.Editor.Tests` (`FieldObstacleBaselineTests`: the fields of a few seeds at loops 0, 4 and 9
  match the recorded placement count and fingerprint; `FieldObstacleFieldTests` (well under a minute): 24 fields generated once for the fixture (seeds 1 / 17 / 4242 / 90001 with the game's profile and every obstacle profile forced on seed 7, each at loops 0, 4 and 9), every rock laid out as on a peer: the station zones and the takeoff corridor reserved and empty (no giant, no debris, no rock reaching in), footprints owned and the giants' gap kept, every two plane rocks >= the ship gap, every field has giants and debris of its profile's giant obstacles, debris types and size classes, debris crosses the plane, a way to B through gaps the ship fits, the same request gives the same fingerprint and rocks; `FieldShortFlightTests`: the loop tests' shortest flights (210 m past the hover point, fields of about 22 cells) on the seeds that once had no placement still get giants; `FieldObstacleVolumeTests`: no volume rock in the clearance band or a giant, none fuse across bands, every 100 m slab holds rocks;
  the frame of long flights and the side room at the cone edge; structure catalog guards; networked structures registered with Mirror; structure layout; cluster layout: the same seed gives the same rocks, the giant crosses the flight plane, outcrops and satellites stay over the footprint, giants stick out by about a cell at most, every local structure has a complete recipe of rocks that never collide (their only collider is the weapon target's trigger, off in the prefab); volume: a chunk gives the same rocks by seed whatever was built before, rocks stay in their band and clear of the plane, never fuse across chunks, none below the plane near the route's ends, every profile has rocks; decor: the planet choice depends on the seed only; rocks, `FieldRockTests`: two independent reads of one field give the same rock ids and poses, a world shift keeps the ids and moves the rocks, a peer's debris laid out from the published field matches the server's, the query finds exactly the rocks touching a sphere or a box, a broken rock is left out, `RockDestroyed` fires once with the rock's data, a giant is unbreakable and large, the broken list goes with the field; `FieldTargetGenerationTests`: every balloon in a passable cell, clear of rocks and of the other balloons, in its height band, loop 0 within 6-10, the same request gives the same balloons, the preset scale gives fewer / more, the step changes no cell or placement; `FieldTargetPopTests`: the query finds a balloon until it pops, a balloon pops once with one event, weapon hits pop it at its hit points as a Weapon pop by the shooter, a hit on a popped one or off it gives nothing, the lists go with the field, weapon target id round trip; `FieldTargetCatalogGuardTests`: catalog and configuration pass their checks, the three kinds and their rewards, every prefab one trigger on `FieldRock`, a shared id fails; `FieldRockDamageTests`: hit points from the diameter (a small rock within 0.8 s of minigun fire, 150 m at least 4 s), hits add up, a rock breaks once as a Weapon break with the shooter and the weapon id, an unbreakable rock ignores hits, a hit off the rock is refused, the damage goes with the field, rock id <-> weapon target id round trip, the rocks register as one target source and a broken rock is no live target; a peer's query over the published field finds the same rocks by id and pose as the server's).
- Player identity and name come from Connection only through NetworkModel contracts: `IPlayerIdentityService`
  (`PlayerKey`) and `IPlayerNameSource` (the authenticated name), implemented in Connection. Other modules read a
  player's name through `IPlayerProfileSource` / `IReadOnlyPlayerProfileModel` (PlayerProfileModule.Data), not
  through the `PlayerProfile` bridge.
- The camera does not know about Grab: `IGameCameraService.LookApplied` fires in `PresentNow()` after
  `ApplyLookRig()` and before the brain update; `HeldItemFollowSystem` (Grab) listens to it. The frame order is the
  same as before.
- **Camera shake goes only through `ICameraShakeService`** (`Features.CameraModule.Contracts`): `AddShake(profile,
  strength)` for a one-shot shake, `BeginShake(profile)` for a continuous one (the handle sets the intensity and
  stops it). `CameraShakeSystem` (CameraModule) applies the sum of all shakes to the look pivot once per frame in
  `LookApplied`, so a seated station's view (the turret's camera point, `BeginFixedView`) shakes too; nothing else
  turns or moves the camera for a shake.
- `LoopSmokeTest` stays in `Assembly-CSharp`: every assembly is visible from there. Whatever the test touches in a
  module must be `public` (not `InternalsVisibleTo`).

### Key places
| What | Where |
|---|---|
| Ship, flight, riders | `Assets/Features/ShipModule/Scripts/`: `ShipBase` (ties its parts: `ShipDeckGeometry`, `ShipModules`, `ShipRiders`, `ShipSeats`, `ShipFlightControl`, `ShipDeckCargo`), `ShipFlight`, `ShipPoseSync`, `ShipRider` (+ `ShipDeckRideRule`, `ShipRideDiagnostics`, see "Riding the ship"), `ShipRunDirector`, `ShipRunService` (phase machine) with `ShipStationPads`, `ShipStationDropService`, `ShipRoute`, `ShipFlightStatService`, `ShipWorldShiftService`, `ShipRunRiderService`, `ShipRunBindingModel` |
| Flight and landing settings | `ShipFlightSettings` (+ `ShipFlightConfig_Default.asset`), `ShipRunConfig` |
| Modules and slots | a module is one `ModuleDefinition` asset (`ShipModule/GameResources/Modules/`: id, slot type, item prefab, slot view, name, price, stat modifiers); `ModuleCatalog` lists them all and resolves a module id (`ModuleCatalogValidator` checks it at startup); `ShipItem`, `ShipSocket` (+ `ShipSocketRule`, state in the `ShipSockets` model), `Ship*Interactable`; catalogs in `ShipModule/GameResources/Configurations/` (Addressables) |
| Stats | `Assets/Features/StatsModule/`, `ShipStatType` (`DodgeRange` 2, `Armor` 4, `FlightSpeed` 5, `Hull` 6, `SonarRange` 7: assets store the numbers, so never renumber; 1 and 3 were the removed `Thrust` / `Handling`, do not reuse them, and `ShipStatsConfigurationValidator` / `ModuleCatalogValidator` reject a stat number the enum does not define), `ShipStatEntity`, `ShipStatSheet` (server: base values from `ShipAccumulativeStatsConfiguration` plus module modifiers; `Hull` is the only accumulative ship stat: modifiers raise its max), synced `ShipStats` model; player movement stats are `PlayerStatType` / `PlayerStatsConfiguration` in `CharacterMovableModule/Scripts/PlayerStats/` |
| Ship hull | contracts in `GameCoreModule/Scripts/Contracts/` (`IShipHull`, `ShipDamage`, `ShipDamageCause`, `IShipHullEvents`: any module can deal damage or listen); `ShipModule/Scripts/Hull/`: `ShipHullService` (the one damage entry and the landing repair), `ShipHullDamageRule` (armor), `ShipHullStat` (the bound ship's Hull / Armor stats, publishes `ShipStats`), `ShipHullEventBus`, `ShipHullConfiguration` (+ `ShipHullConfigurationValidator`); HUD bar `ShipHullView` / `ShipHullPresenter` in `LobbyModule` (game HUD; flash timing in `ShipHullHudConfiguration`); hull 0 -> lobby in `PlayerLifeSystem` with `CrewLobbyReturnSchedule`; the debug window's Ship → "Hit the hull" hits it with `DebugDamage` (or the typed damage), "Destroy the hull" takes it to 0 (host, editor / development builds, `ShipHullDebugSystem` / `IShipHullDebug`) |
| World shift | `WorldShift` (count and total of the shifts), `ShipWorldShiftService`, `ShipPoseSync`; content outside the ship module follows a shift through `IWorldShiftFollower` (server: `ShipWorldShiftService.ApplyWorldShift`, remote clients: `ShipRunDirector.RpcShiftWorld`) |
| Flight field | `Assets/Features/FlightFieldModule/`: model (`FieldFrame` cell <-> world, a rectangle: `LengthCells` along A -> B = the distance in cells + the margins behind A and B, `WidthCells` across = `FieldWidthCells` (61), widened when the cruise start lies far off the axis so `SideMarginCells` stay beside it; `FieldCell`, `FieldPlacement`, `FieldRoute`, `FieldRequest`, `FlightField`); a structure is one `FieldStructureDefinition` (id, kind, footprint rows, prefab, `IsNetworked`, visual yaw / scale jitter, cluster recipe) in `FieldStructureCatalog`, which also lists the kinds of small debris (`FieldDebrisDefinition`: id + `FieldDebrisRecipe`); tuning in `FlightFieldConfiguration` (both in the `Configurations` group; the ship gaps: ship width, debris gap margin; the takeoff corridor of `FieldTakeoffCorridorStep` runs from A along the takeoff heading past the cruise start by `TakeoffRunOutLength`, so the pilot has room to find B and turn); `IFlightFieldGenerator` runs the `IFieldGenerationStep`s in the order of their bindings in `FlightFieldModuleInstaller`. **Structure of a field:** every number comes from its `FieldDifficultyProfile` (`Configurations/Profiles/FieldProfile_ObstaclesSparse/Medium/Dense/Giants`, all listed in `FlightFieldConfiguration.DifficultyProfiles`; every value is a `FieldDifficultyRange` read at the field's difficulty 0..1, the hardest of its route difficulties); every `_routesByLoop` entry has route difficulties and weighted profiles (loop 0 Sparse 3 / Medium 1; 1-2 Medium 3 / Sparse 1 / Giants 1; 3-5 Medium 2 / Dense 2 / Giants 1; 6+ Dense 3 / Giants 2 / Medium 1); `FieldProfileSelector` picks one from the field seed (editor / development: `IFieldProfileDebug.DebugUseProfile(id)` forces one from the next launch). **Steps** (`InstallGenerationSteps`, in this order, each runs for every field): `FieldFrameStep`, `FieldStationZoneStep`, `FieldTakeoffCorridorStep`, `FieldObstacleRouteStep`, `FieldDensityMapStep` (low-frequency ridge noise with the profile's contrast and stretch plus a few large voids, `FieldDensityMap`), `FieldObstacleGiantStep`, `FieldObstacleDebrisStep`, `FieldBalanceMetricStep` (zone shares and dodges per minute on the main centerline at cruise speed, in `FlightField.Metrics`), `FieldTargetStep`. The corridor generation (walls, ridges, channels, mazes, branches, the passage repair) was removed on 2026-10-24: the field is one obstacle field. **Obstacle field** (the four profiles are data only; the user compares them): the whole plane is rocks to fly between. The main route is the straight cell line cruise start -> B, reserving nothing (dodge metric, volume station clearance, debug map read it). Giants (large 150-350 m, massive 400-900 m) are synced placements: `FieldObstacleGiantStep` draws the profile's `GiantsPerSquareKilometer` x field area giants, each from a `FieldGiantClass` (Large / Massive) by weight and an obstacle of that class by weight (Large `ObstacleGiant_06/08/10/12`, Massive `_16/_20/_24/_28`: disc footprints of that many cells, trigger prefabs `Rock_NxN`, recipe `ObstacleGiant` = one main rock of the chunky prefabs with its own warm materials `FieldRock_GiantAsh/Basalt/Slate` (copies of the rock materials with about three times the faint emission, so the shadow side of a giant still shows its shape against space; same shader, SRP Batcher kept), no satellites, axis scale 0.85-1.15, inside the footprint's inscribed circle by its farthest vertex, so the rock one sees is about 0.6-0.7 of the footprint: Large about 150-330 m, Massive about 400-780 m; the debug map and the test table give footprint sizes), drawn only among the obstacles whose footprint fits the frame (a short field, such as a loop test's, would otherwise drop every massive one and could end without a placement), biggest first at random spots on unreserved cells, kept by the density map's chance (`IFieldObstacleDensity`) and only with `GiantGapCells` free of other giants around the footprint (40 m >= the ship gap); `FieldObstacleDebrisStep` gives every other unreserved, non-void cell debris (`obstacle_debris_rocks`, recipe `ObstacleDebris_Rocks`: no off-plane rocks, middles up to 0.6 of the radius off the plane, so the plane rocks do not read as a flat line) at `ObstacleDensity` moved by the density noise (`ObstacleDensityVariation`) and lowered by the optional edge ramp (`EdgeRampCells`, `EdgeDensityShare`; no wall at the edge, the real boundary is M3). Small / medium rocks (10-30 / 40-150 m, the profile's `DebrisSizeClasses` with weights) are laid out by every peer from the seed like all debris (`FieldDebrisLayout` reads the profile through `IFieldDebrisCells.Profile`): the bigger rock wins a conflict (priority = diameter + roll) and every rock keeps the full ship gap (16 m) from the giants' cells, so in the flight plane every gap between two rocks, a rock and a giant or two giants is at least ship width + margin; nothing closes the plane, so a way A -> B always exists (`FieldObstacleFieldTests` check it on the laid-out rocks). The dodge metric counts the debris around the straight line plus every giant on it, over the line's real length; with the 16 m gap it saturates near 6-8 per minute (rocks of 10-30 m pack to about 245 per km2), giants lower it; Medium (small rocks 10-18 m, few giants) gives about 6.6-6.9, Dense (10-16 m, even density) about 7.0-7.3. Measured in the debug map (rocks per km2, size classes, largest rock); the 200-seed `[FieldObstacles]` table test was replaced by the fast `FieldObstacleFieldTests` on 2026-10-25. Every profile may bring its own volume (`FieldDifficultyProfile.Volume`, empty = the configuration's): the obstacle profiles use `FieldVolumeConfiguration_Obstacles` (bands from small to big over 25-600 m: 10-30 m up to 260 m, 40-150, 150-350 and 380-500 m up to 600 m, plane clearance 25 m, so a rock either crosses the plane (debris, giant: obstacle in the data) or stands at least 25 m off it); the volume reader adds the giants' spheres so no volume rock comes into a giant, and a volume rock gives way to any rock of a later band (no small rock sticks into a big one); a profile's volume stays below the configuration's top (planets unchanged). Debris is not a structure: a passable cell keeps a debris kind and a density, the network carries runs of debris cells (`FieldDebrisRunEntry`), and every peer lays the rocks out itself from the field seed (`FieldDebrisLayout`, the cells read through `IFieldDebrisCells`: the generator's matrix `MatrixFieldDebrisCells`, a peer's live field `LiveFieldDebrisCells`): a jittered grid of `DebrisGridSpacingMeters` (25 m) over the whole field plane, a point holds a rock with the cell's density, sized by the profile's `DebrisSizeClasses` (the recipe's `SmallDiameterMeters` / `BigDiameterMeters` only bound the neighbour reach), most crossing the flight plane (`CrossShare`), some above / below (`OffPlaneShare`); no rock reaches into an empty cell and none comes closer than the ship gap to a giant's cell; a point closer than the ship gap (ship width + margin, 16 m) to a kept neighbour of higher priority gives way (sequential Matern thinning, decided with an explicit stack in `FieldDebrisWorkspace`, exact priority order), so any two rocks leave the ship room, whatever order the cells are built in. The slalom is checked in the editor by `FieldSlalomAnalyzer` (all rocks laid out as on a peer, the giants' cells closed as they are; `FieldSlalomPathFinder`: A* over a 2.5 m grid, `FieldSlalomGrid`, then pulled straight): a way from the cruise start to B with the ship clearance less half a grid diagonal, its length against the straight line, the narrowest gap and the rocks of the flight plane; `FieldObstacleFieldTests` run it on the obstacle profiles. In the world: `FlightFieldLaunchSystem` (server, `IFlightRouteListener`) has `FlightFieldService` (`IWorldShiftFollower`) generate at launch on a worker thread (the generation touches no Unity object; a loop 9 field takes over 100 ms) and publish the field on the first tick after it is done; the service keeps the field in `FlightFieldServerModel` and publishes it through `FlightFieldPublisher` into the `LiveField` model in batches (`FieldPublishJob`: `PublishEntriesPerBatch` = 300 entries, about 7 KB, one batch every `PublishBatchIntervalSeconds` = 0.1 s, so every Mirror delta stays below Telepathy's 16 KB message limit; FizzySteamworks allows 512 KB; the state with the new field number goes out after the last batch, a new field or `Clear` drops a publish in progress; a loop 9 field of ~53 KB takes about 1 s) (`FlightFieldSync` on the `ShipRunDirector` object); `LiveFieldBuildSystem` streams the local structures on every peer: it builds the placements within `BuildRadius` (1 300 m) of the game camera (`ILocalViewpointSource` from the Contracts, bound by the camera module; the decision is `FieldStreamingRule`, the placement states live in `FlightFieldSceneModel`), at most `StructuresBuiltPerFrame` per frame, and returns the ones beyond `ReleaseRadius` (1 500 m) to the pool (`IFieldStructureSceneBuilder.ReleaseLocalStructure`); the small debris streams the same way (`FieldDebrisStreamer` unrolls the `DebrisRuns` of the live field into cells, `FieldDebrisSceneBuilder` builds them within `DebrisBuildRadius` (900 m), `DebrisCellsBuiltPerFrame` per frame, from its own pool under its own root `FlightFieldDebris` posed like the field root); the live field state also carries the field's `ProfileId`, through `FieldStructureSceneBuilder` + `FieldStructurePool` under one `FlightField` root (`FlightFieldSceneModel`); `FieldStructureSpawnRule` decides local or Mirror (`FieldNetworkedStructures`, prefab marked with `FieldNetworkedStructure`; a client registers the networked prefabs of the catalog with Mirror on every client start, `FieldNetworkedPrefabRegistration` called by `FieldNetworkedPrefabSystem` in GameCore, so they are not in the network manager's spawn prefabs). Rock look: the structure prefab (`GameResources/Prefabs/Rock_*`) is only the trigger; what a player sees comes from its `FieldClusterRecipe` (`GameResources/Rocks/Recipes/`: weighted rock prefabs, shared materials as the tint spread, main rock size and height jitter, satellite count / size, height spread up to 3 cells (2-2.5 in the game recipes, so the field is not one flat line), debris chance / count / size). `FieldClusterLayout` turns the recipe into rock poses from the field seed + placement index (main rock in the middle of the footprint, always across the flight plane; satellites and debris over random footprint cells; every rock stays inside the footprint, because a client does not know which cells around it are kept open; full random 3-axis turn, per-axis scale 0.8-1.2), so every peer sees the same clusters; `FieldStructureSceneBuilder` takes every rock from the pool (one pool per prefab; `TakeRock` re-parents a free instance and keeps its `FieldRockVisual`) and parents it to the trigger, so the root keeps one object per structure, and remembers the objects per placement (`ReleaseLocalStructure`, `ReleaseAll`). Rock prefabs (`GameResources/Rocks/Prefabs/FieldRock_01..11`, meshes of `Global/Packages/PolyOne/Rocks Stylized`, normalised to about 1 m, `FieldRockVisual` at the root, no colliders) and 3 shared URP Lit materials (`GameResources/Rocks/Materials`: the pack's albedo tinted dark blue-grey / rusty brown with a faint warm emission (the nebula's reflection), matte, SRP Batcher compatible: the tint is a material swap, not a `MaterialPropertyBlock`). **Plane cover** (`FieldPlaneCoverLayout`): a cluster recipe with plane rocks closes its footprint at the flight plane with a dense random rock field (quadtree parts, `PlaneGapMeters`, `PlaneMaxSpillMeters`, `ExtraRocksPerCell`); it was the look of the removed walls and ridges, no game recipe uses it now. No cluster has small debris any more. A new look = a new recipe asset. Rocks: trigger colliders of the structure prefabs (`Rock_*`) on the `FieldRock` layer (8; was `Ignore Raycast`), the collision matrix is unchanged (every layer pair collides, triggers push nothing; the aim ignores triggers and the layer is in neither `InteractableMask` nor `AimBlockerMask`; the support and shove probes use explicit masks without it). Decor (visual only, every peer builds it locally, no colliders, not in the matrix, not synced; chunks and planet in `FlightFieldDecorConfiguration` in the `Configurations` group, checked by `FlightFieldDecorConfigurationValidator`): `FieldDecorBuildSystem` keeps the volume of big rocks above and below the flight plane in chunks around the local ship (`IShipPositionSource`; on a client it reads `ShipClientBindingModel`, bound by `ShipRunDirector` on client start) within the view distance, a few chunks per frame, and releases the chunks left behind. The volume (`FieldVolumeConfiguration`, referenced by `FlightFieldConfiguration.Volume`, checked by `FieldVolumeConfigurationValidator`) has bands: near 40-300 m (rocks 50-150 m) and far 300-600 m (120-220 m), mirrored below the plane; `FieldDecorChunkLayout` draws every band's grid points of the chunk (`FieldVolumeCandidates`: a rock with the band's presence at the ridge noise below it, `FieldRidgeNoise`, the same noise the density map is built on, so ridges are denser above and below and glades sparser but never empty; read with the profile and difficulty of the live field, `LiveFieldVolumeSourceReader`), keeps every rock inside its band and at least `PlaneClearanceMeters` (30 m) from the plane, none below the plane within `StationClearanceMeters` of the main route's ends, and thins points that come closer than the band's gap to a neighbour with a higher priority (Matern thinning), so two rocks never fuse and every peer and a rejoining player see the same rocks whatever order the chunks are built in. `FieldDecorPlanetPicker` picks the flight's planet from the field seed (chance, 1 = every flight shows one, max per flight, side of the sky, which planet by the weights of `Planets`, its diameter from that planet's own range, distance from the route) around the route axis (the middle of the field's width): the side comes from the seed and the weights of `PlanetSides` only (default `Side` 0.5 at 5-20 degrees above or below the plane, `Above` 0.4 at 25-60, `Below` 0.1 at 25-50 below), not from the light; within the side the planet turns towards the main light's travel direction (`IFieldDecorLightSource`, `RenderSettings.sun`; seeded tries within `PlanetMaxAngleFromLight` around the vertical, the closest one wins), so the light comes from behind the viewer and the lit side faces the ship (no main light: towards one side of the route); the game scene's sun shines 50 degrees down, so a planet above shows a half to two thirds of its lit side; every planet stays at least `PlanetMinAngleFromCourse` (35 degrees) off the course A -> B as seen from the start of the field (the azimuth is turned out, the along-route offset pulled back), so it never covers station B; planet prefabs `GameResources/Planets/DecorPlanet_*` are the pack's icosphere: six `DecorPlanet_Storm_<Colour>` presets (the pack's "Gas Giant with Storms" shader, project material copies with their own 256x1 gradient in `Planets/Gradients`, slow flow) plus four `DecorPlanet_Earth_*` (Ocean, Alien, Ice, Desert; "Earth-like Lite" shader) and two `DecorPlanet_Moon_*` (BlueGrey, Lilac; "Moon Lite", no fresnel) presets; the baked Earth C / Moon A / yellow gas giants stay on disk but are not in the list; all copied into `GameResources/Planets/Materials` with a wider, brighter fresnel so the night side is not pure black (no Glow / Corona / ring materials). Sizes per planet in `Planets` (gas giants 1600-2600 m, Earth-like 700-1100 m, moons 300-500 m); `PlanetDistance` (2-3.6 km) is to the near surface, so a bigger planet stands farther. A planet never enters the field: its surface stays `PlanetFieldClearance` (300 m) beside this flight's field width (`WidthCells`, so a widened field too) or above / below the top of the volume rock bands (`FieldVolumeConfiguration.TopMeters`, 600 m), and `PlanetStationClearance` (1000 m) off the route axis A -> B (both pads stand on it). Seen from the start of the field its far edge stays within `ViewFarClipPlane` (5000 m, the player camera's) minus `PlanetFarClipMargin` (300 m). Each seeded direction try is kept only if the planet's smallest size fits between these clearances and the reach; the distance is pushed out to the clearances, the along-route offset pulled back, and the diameter shrunk (never below the planet's minimum) to fit; with no fitting try the flight shows no planet (under 1 % of flights on the default width, about 10 % on a field widened to 2.4 km half width, all of them side planets). Every weighted roll of the module (giant obstacle options and their turns, cluster and volume rocks, debris kinds, difficulty profiles of a loop, planet sides, planets) goes through `FieldWeightedPick` over `IFieldWeighted`. `FieldDecorSceneBuilder` builds under its own `FlightFieldDecor` root posed like the field root (so it follows a world shift) with its own `FieldStructurePool` (the field's pool is emptied on every field change); state in `FieldDecorSceneModel`. Debug: **Tools → Flight Field → Debug Map** (editor; profile override, zone or density layer, the main line, the balance numbers, "Rocks and slalom way": every debris rock (in / off the plane) and the slalom way with its numbers; "Live field" shows the running host's field and the ship), `GameResources/Debug/Debug_NetworkBeacon` (networked test structure, not in the catalog) |
| Breakable rocks | `FlightFieldModule/Contracts/` (assembly `Features.FlightFieldModule.Contracts`) and `FlightFieldModule/Scripts/Rocks/`. A rock is a `FieldRockId` (readonly struct: source `Cluster` / `Debris`, the placement index in the field's placement list or the debris cell, the rock index in that placement's cluster or that cell), the same on every peer and across world shifts, because the server reads rocks through the same layouts the peers build them with (`IFieldPlacementRockLayout`: the trigger jitter and the cluster rocks of a placement from the field seed + placement index, used by `FieldStructureSceneBuilder` and the server; `IFieldDebrisLayout` for debris). Server: `IFieldRockQuery` (`FieldRockQuery`) finds the rocks touching a world sphere or turned box from the generated field (`FlightFieldServerModel.Field`, its frame moved by every world shift), not from the scene, looking only at the cells under the volume widened by the farthest a rock reaches past its cell. The field comes from an `IFieldRockFieldSource` (the server: `ServerFieldRockFieldSource`); every peer has the same query over the published field, `IPeerFieldRockQuery` (`PeerFieldRockQuery`: a `FieldRockQuery` with `LiveFieldRockFieldSource`, whose `PublishedFieldRockReader` turns the live placements and debris runs into `FieldRockFieldData` through `PublishedFieldRockCells` and `LiveFieldDebrisCells`), one reader and one set of layouts for both (`FieldRockTests` checks the same rocks by id and pose); the balloons the same way (`IFieldTargetFieldSource`, `IPeerFieldTargetQuery`). Each result (`FieldRockDescription`) has the id, the structure / debris id, the world middle, the diameter, the size (`FieldRockSize` Small / Large: `FieldRockClassifier`, the diameter against `FieldRockDurabilityConfiguration.SmallMaxDiameterMeters` (35 m), large whatever its size when the recipe is `IsAlwaysLarge`) and whether it breaks (the recipe's `IsBreakable`: `FieldClusterRecipe` / `FieldDebrisRecipe`; `ObstacleGiant` is unbreakable and always large); broken rocks are left out; no allocation per call once its lists have grown (`FieldRockReader` lays one placement or debris cell out, `FieldRockFieldData` keeps the per-field lookup and the debris workspace). `IFieldRockBreaker.ServerBreak(id, cause, instigator)` (`FieldRockBreaker`) is the one way to break a rock: `NoField` / `AlreadyBroken` / `NotFound` / `Unbreakable`, else the id goes into the live field model's synced `BrokenRocks` list (`LiveFieldBrokenRocks`; Mirror writer `FieldRockIdSerializer`) and `IFieldRockEvents.RockDestroyed` fires once with `FieldRockDestroyed` (rock, cause `ShipCollision` / `Weapon`, optional `PlayerKey`, field number, loop). The list is emptied when a field is published or taken down (`FlightFieldService` -> `IFieldRockReset`). Every peer: `FieldRockBreakViewSystem` hides a built rock when its id joins the list and plays the pooled `FieldRockBreakEffect` (`GameResources/Rocks/Effects/`: rock chunks in the rock's material + dust, authored for 1 m and scaled by the rock's diameter, under the debris root; `FieldRockBreakEffectPool`); a rock built later (streaming, a late joiner's snapshot) is built hidden. Debug (editor / development): `IFieldRockBreakDebug.DebugBreakNearestRock` (also the debug window's Rocks → "Break the nearest rock", host only, `FieldRockBreakDebugSystem`) breaks the breakable rock nearest to the ship; `FieldRockDestroyedLogSystem` logs every break with `[FieldRock]`. Rock trigger colliders are on the `FieldRock` layer (8). **Weapon damage on rocks:** every rock prefab (`FieldRock_01..11`) has a `FieldRockTarget` on its `Mesh` child (layer `FieldRock`, a convex trigger `MeshCollider`, off by default; `IWeaponTargetCollider` + `IWeaponImpactFeedback`); `FieldStructureSceneBuilder` / `FieldDebrisSceneBuilder` bind it through `IFieldRockTargetBinder` (the rock's weapon target id + the hit feedback) when they build a cluster or debris rock and unbind it (`FieldBuiltObjects.UnbindRocks`: id 0, collider off) when it goes back to the pool, so decor rocks of the same prefabs are never hit. `FieldRockTargetIds` packs a `FieldRockId` into the target id (top byte 0xF1, the debug dummy keeps 0xDE). Server: `FieldRockWeaponTargetSystem` registers once in `IWeaponTargetRegistry` as an `IWeaponTargetSource` for that id range (no per-rock registration, no scene object) and hands the hit path the one injected `FieldRockWeaponTarget` (`IsAlive` = the rock is in `IFieldRockQuery`); its hit goes to `FieldRockDamageService` (`IFieldRockDamageService`): the rock must exist and not be broken (`IFieldRockQuery.TryGetRock`), the reported point lie within its radius + `HitPointToleranceMeters` (3 m), an unbreakable rock ignores the hit; the damage taken is kept only for hit rocks in the server's `FieldRockDamageModel` (bound in `DataInstaller`, not synced); at the rock's hit points (`FieldRockHitPoints`: `FieldRockDurabilityConfiguration.HitPointsByDiameter`, linear 0 m 8, 35 m 12, 60 m 40, 150 m+ 100, so with the minigun's 20 hits/s a small debris rock breaks in about 0.5 s, a 60 m rock in 2 s, a 150 m one in 5 s) it breaks through `IFieldRockBreaker.ServerBreak(id, Weapon, shooter, weaponId)`; `FieldRockDestroyed.WeaponId` carries the weapon (empty for other causes). Both the broken list and the damage are `IFieldRockReset`s that `FlightFieldService` resets with the field. Every peer that fires or replays the stream: the chips and dust are the weapon's impact feel; `FieldRockHitFeedbackSystem` shakes the struck rock's mesh and flashes its emission for `HitFeedbackSeconds` (a property block only while it runs; the giant materials without `_EMISSION` only shake) |
| Target balloons | `FlightFieldModule/Contracts/` (`FieldTarget*`, `IFieldTarget*`) and `FlightFieldModule/Scripts/Targets/`. Bright cartoon balloons (primitives, `GameResources/Targets/`) hang in the passable cells of the flight field; the gunner pops one with the minigun or the ship flies into it, and the crew gets money. **Data:** a kind is one `FieldTargetDefinition` (id, prefab, radius, reward, weapon hit points, spawn weight, pop colour) in `FieldTargetCatalog_Default` (`balloon_small` red 3 m 20 1 HP weight 6, `balloon_medium` blue 4.5 m 50 2 HP weight 3, `balloon_golden` gold with a crate 3.5 m 200 3 HP weight 0.6; `FieldTargetCatalogValidator`); counts, placement, streaming, hit tolerance and feel are in `FieldTargetConfiguration_Default` (both in the `Configurations` group; `FieldTargetConfigurationValidator`): loop 0 gets 6-10 balloons, +1.5 per loop, at most 30, times the stage preset's `FieldTargetPresetBlock.CountScale` (Early 0.7, Mid 1.25, Late 1.6; the applier writes it through `IFieldTargetCountScaleWriter` into `FieldTargetCountScaleModel`, `FieldRequestFactory` puts it into the `FieldRequest`). **Generation:** `FieldTargetStep`, the last generation step, with its own random sequence from the field seed: a kind by weight, a passable cell (no placement, nothing reserved, no giant) within `MaxCourseDistanceMeters` (450 m) of the main course, picked with weight 1 + 3 x its distance share (more of them away from the course, so there is a choice; `FieldWeightedPick.PickIndex` over running weights), a random spot and height (-3..+6 m) in it, kept only `RockClearanceMeters` (8 m) clear of every rock (`FieldTargetRockClearance`: the debris rocks around the spot exactly, through `IFieldRockReader` as the server reads them, cached per generation in `FieldTargetRockWorkspace`; a cell with a placement (a giant) keeps the spot one more cell away instead of laying its rocks out, which would be far too slow; the reader's buffers are per thread, because generations run on worker threads) and `MinSpacingMeters` (60 m) from the other balloons; it changes no cell, so the rocks stay. The balloons are `FlightField.Targets` (`FieldTargetPlacement`: kind + field-space position). **Network, like rocks:** the published field carries them as the live field's `Targets` list (`FieldTargetEntry`), a balloon's id is its index (`FieldTargetId`), popped ones go into the synced `PoppedTargets` list (`LiveFieldPoppedTargets`, Mirror writer `FieldTargetIdSerializer`); every peer builds the balloons within the field's build radius (1 300 m, `IFieldStreamingRule`) of its camera from its own pool under the `FlightFieldTargets` root posed like the field root (`FieldTargetBuildSystem`, `FieldTargetSceneBuilder`, `FieldTargetSceneModel`), never a popped one (streaming, a late joiner), and returns them beyond 1 500 m; the lists are emptied with the field (`IFieldTargetReset` in `FlightFieldService`). **Server:** `IFieldTargetQuery` (`FieldTargetQuery`, from the field data, popped ones left out) and the one pop entry `IFieldTargetPopper.ServerPop(id, cause, shooter, weaponId)` (`FieldTargetPopper`: `NoField` / `AlreadyPopped` / `NotFound` / `Popped`); `IFieldTargetEvents.TargetPopped` fires once per balloon with `FieldTargetPopped` (balloon, kind, reward, position, cause `Weapon` / `ShipCollision`, the shooter's `PlayerKey`, weapon id, field number, loop). Weapon hits: the prefab root is the trigger (`FieldTargetView`, layer `FieldRock`, off until bound) with weapon target id top byte 0xF2 (`FieldTargetWeaponIds`); `FieldTargetWeaponTargetSystem` registers one `IWeaponTargetSource`, `FieldTargetDamageService` checks the hit against the field data (exists, not popped, point within radius + 3 m), adds the damage (`FieldTargetDamageModel`, server only) and pops it at its kind's hit points as a Weapon pop. Fly-in: `ShipFieldTargetCollisionSystem` (ship module, every physics step of takeoff / cruise / landing) takes the same per-step hull sweep as the rock check (`IShipHullStepSource` / `ShipHullStepTracker`: one sweep per physics step for both) and pops every balloon its boxes touch as `ShipCollision`; a balloon never pushes or damages the ship. **Money** is a subscriber: `FieldTargetRewardSystem` (Shop) pays `popped.Reward` into the crew wallet (`ICrewWallet.ServerEarn` through `ICrewWalletBindingModel`, bound by `CrewWallet` on the server). **Feel on every peer:** a balloon bobs gently and squashes when struck (`FieldTargetView`, look only); a pop (`FieldTargetPopViewSystem`) hides it and plays the pooled `FieldTargetPopEffect` (`GameResources/Targets/Effects/`: confetti and scraps in the kind's colour, coins that burst out and are sucked into the ship, a rising "+N" facing the viewer, `PopSound` / `CoinSound` AudioSources as sound hooks, empty for now; numbers in `FieldTargetPopFeel`); the HUD balance flashes and grows on every rise (`BalancePresenter`, `WalletConfiguration.IncomeFlash*`). `FieldTargetPoppedLogSystem` logs every pop with `[FieldTarget]` (editor / development) |
| Weapons | `Assets/Features/WeaponModule/`. A weapon is one `WeaponDefinition` asset (`GameResources/Weapons/`: stable id, name, shots per second, damage per hit, range, the parts, a `WeaponFeelProfile` with the look numbers: muzzle flash, casings, recoil, camera shake, tracer every N, impact) in `WeaponCatalog_Default` (`Configurations` group, `WeaponCatalogValidator` at startup). Parts are `[SerializeReference]` objects inside the definition, picked in the inspector from a type menu (`WeaponPartPickerAttribute`): fire mode (`IWeaponFireModeDefinition` -> runtime `IWeaponFireMode`; `AutoFireMode` with barrel spin-up / spin-down), shot delivery (`IShotDeliveryDefinition` -> `IShotDelivery`, outcome through `IShotOutcomeReceiver`, may come later than the shot; `HitscanDelivery` with a spread cone over an injected `IWeaponRaycast`; `PhysicsWeaponRaycast` takes the nearest solid collider or trigger that carries an `IWeaponTargetCollider`, so a trigger with no target, such as a field structure's volume, lets the shot through, and carries a struck `IWeaponImpactFeedback` in the hit and the outcome, which `WeaponShooter` shows on every peer that fires or replays the shot before passing the outcome on), hit effects (`IHitEffect` list; `DamageHitEffect`); holder and aim (`IWeaponHolder`, `IWeaponAim`) and the client feel (`IWeaponFeel`) are interfaces only. The server checks a reported hit with `WeaponHitRule` (pure: weapon held, target alive, range + tolerance, fire rate budget `WeaponShotBudget` with tolerance and burst from `WeaponHitRuleConfiguration_Default`); targets implement `IWeaponTarget` from `Features.WeaponModule.Contracts`. A new weapon is a new asset and, if needed, one new part type, never a `switch` on a weapon type (`WeaponCatalogGuardTests`). A delivery definition builds its runtime part from an `IShotDeliveryContext` (`PhysicsShotDeliveryContext`: `PhysicsWeaponRaycast` on the delivery's `_hitLayers`, triggers included; the minigun hits only layer `FieldRock`, never the own ship or players; a collider of a target carries an `IWeaponTargetCollider` with its id); `IWeaponShooterFactory` gives every holder its own `WeaponShooter` (fire mode + delivery), whose outcomes go to `ReportedShotOutcomes` on the holding peer (feel + hit report) or `ShownShotOutcomes` on every other peer (feel only). **Network path, one for every weapon:** `WeaponHolderSync` on the player prefab: the owner sends trigger + aim direction in the holder's own frame (`CmdSetAim`, the holder throttles it; other peers read the SyncVars and replay the fire themselves, no message per bullet) and its target hits through `WeaponHitQueue` in fixed-slot `WeaponHitBatch`es (4 hits, no allocation) at most once per network tick (`CmdReportHits`); the server keeps a `ServerWeaponHolding` (weapon, catalog index, shooter key, server-side aim, fresh `WeaponShotBudget`) set by the holder (`ServerBeginHolding` / `ServerEndHolding`), and `WeaponHitService` checks each hit with `WeaponHitRule` and applies the weapon's hit effects to the target from `IWeaponTargetRegistry` (Contracts; targets register by id on the server, `WeaponTargetRegistry`, or a whole family registers once as an `IWeaponTargetSource` that answers for its own id range, as the field rocks do). **Feel** (`Feel/`, client only, everything pooled at `Bind`, 0 B GC per frame of continuous fire): `MinigunFeel` (the slot view drives it with `Advance`) with `BarrelSpin`, `MuzzleFlash`, `RecoilJitter`, the camera shake through `ICameraShakeService` (the seated owner's view only: a continuous fire shake while it looks through the gun and a one-shot shake from impacts within `NearImpactDistanceMeters`; profiles `CameraShake_MinigunFire` / `CameraShake_MinigunNearImpact` in `GameResources/Weapons`, referenced by the `WeaponFeelProfile`), `CasingStream` (casings simulated in the floor's frame, `IWeaponFeelFloor`: they ride with the ship, bounce and pile on the deck and are never physics bodies), `TracerStream`, `ImpactBursts` (flash, sparks, chips, dust in the frame's custom simulation space) and `WeaponFeelAudio` (sound hooks; the minigun's placeholder clips in `GameResources/Audio` are generated, no licence). Every number is in `MinigunFeelProfile`. **Minigun turret on the ship:** `MinigunModule` (slot `Turret`, removable, 600, in the shop, in the station drops, in the `Mid` / `Late` presets) on socket `turret-front` (`ShipPlatform/TurretSocket`, front-left of the deck, mirroring the radar; its child `SeatInteractable` is the seat collider behind the gun). The slot view `MinigunSlotView` (primitives: tripod, yaw pivot, pitch pivot with drum, spinning barrels, ring-and-cross sight, V grips, cyan gloves shown while someone sits; a `ShipSeat`) carries `ShipTurretStation`, the turret's `ISeatStation`: the socket tells it on every peer who sits there by player key (`ShipSocket.SyncStationOccupant`; the station finds that player with `IShipRiders.TryFindByKey`; no role branch in `ShipRider` / `ShipSeats`); on the owner's peer the mouse turns it at once (`TurretAim`, limits and sensitivity in `MinigunTurretDefinition`: yaw free, pitch -10..75), the `Fire` input (LMB / right trigger) fires, interact or jump stands up (`TurretOwnerInput`, `ShipRider.OwnerRequestStand`) and the camera rides the gun's camera point (`IGameCameraService.BeginFixedView` / `EndFixedView`: FP camera, mouse look off); the seat point (`SitPoint`) sits on the yaw pivot and the station has the seated rider follow it on every peer (`ShipRider.FollowSeatPoint`, from the aim that peer shows, no extra messages), so the gunner's body turns with the gun; the owner's own body renderers cast only shadows while it sits (`SeatedOwnerBody`: at a steep pitch the camera drops below the body), and standing up leaves the player where the seat point was, clamped onto the deck walk (`SeatStandingSpot`; only a rider that followed a moving seat point, a fixed seat keeps its spot); other peers smooth the synced aim (`TurretAimSmoothing`) and replay the fire. **Debug:** the debug window's Weapons → "Spawn / Remove the weapon test dummy" on the host (editor / development builds, `WeaponDebugTargetSystem` / `IWeaponDebugTargets`, the spawned dummy in `WeaponDebugTargetModel`) puts a `WeaponDebugTarget` dummy in front of the ship (layer `FieldRock`; it sways from side to side, counts and flashes the hits the server accepted). Everything about it is compiled only in editor / development builds: its prefab and ship offset come from `WeaponDebugTargetConfiguration_Default` (`Configurations` group, bound under the same define), and `WeaponDebugTargetPrefabSystem` registers the prefab with the Mirror client on every client start, so it is not in the network manager's spawn prefabs. Who shoots is only the server's `IWeaponShooterIdentity` (`ServerWeaponHolding`); a peer's `IWeaponHolder` gives just the trigger and the aim. The station talks to its weapon model's feel through `IWeaponFeelPresenter` (`MinigunFeel` is one), so another gun model brings its own presenter |
| Camera shake | **only through `ICameraShakeService`** (contracts in `CameraModule/Contracts/`, assembly `Features.CameraModule.Contracts`; core in `CameraModule/Scripts/Shake/`). A shake is one `CameraShakeProfile` asset (degrees, roll share, metres, seconds, frequency, fade power, length scaled by strength); a new source brings its own profile asset, no code in the camera. One-shot: `AddShake(profile, strength)`, one entry per profile in `CameraShakeModel` (bound in `DataInstaller`): a stronger shake of that profile restarts it, a weaker one is ignored while the current one is still stronger, different profiles add up; it fades as (1 - age / length) ^ power and stays in the model at zero (no allocation per hit). Continuous: `BeginShake(profile)` returns an `ICameraShakeHandle` (`SetIntensity` eases over the profile's seconds, `Stop` removes it). `CameraShakeSampler` (pure, tested in `CameraShakeTests`) moves every shake on and sums them (Perlin noise per axis); `CameraShakeSystem` applies the sum to the look pivot in `LookApplied` and clears the model on `CleanupGameplay` / `RestartGameplay` and every scene load. Sources: rock impacts (`ShipImpactCameraShakeSystem`), the minigun (`MinigunFeel`) |
| Cargo on the deck | `ShipDeckCargo`: items lying on the deck are attached to the ship and travel with it |
| Sonar (navigator) | `Assets/Features/SonarModule/` (`Contracts`, `Data`, `Scripts`). The radar module (`RadarModule.asset`, slot `radar`) is a **console**: its slot view `RadarItemView` is a pedestal with a screen, and `ShipPlatform/RadarSocket/SonarConsole` (trigger box on the screen, layer 7) carries `SonarConsoleInteractable`. The console is active only while a module stands in the socket and the ship flies (`ShipRunPhase.IsFlight()`: Takeoff / Cruise / Landing), so at a station E on the module still takes it off. E sends the use command; the server (`SonarNavigatorSystem`, `SonarConsoleRule`) seats **one navigator by `PlayerKey`** in the synced Shared `SonarConsole` model (`NavigatorKey`; bridge `SonarConsoleSync` on `RadarSocket`, which also binds the console in `SonarConsoleBindingModel`) and opens the console on that player's peer (`TargetOpenConsole` -> `SonarScopeModel.Open`). `SonarWindowSystem` then opens the MVP `SonarWindow` (`SonarScopeView` / `SonarScopePresenter`, prefab `SonarModule/GameResources/Prefabs/SonarWindow`, Addressables `Windows`), blocks walking and aim (`PlayerControlBlockModel`) and frees the cursor; Esc, E or the Leave button close it, `SonarConsoleReachSystem` closes it when the player dies, walks farther than `ConsoleKeepOpenMeters` or the server gave the seat away; closing sends `CmdLeave`; the server also frees the seat on disconnect, distance or landing. **Ping game** (`SonarScopeSystem`, numbers in `SonarConfiguration_Default`, Addressables `Configurations`): Space or the Ping button (cooldown `PingCooldownSeconds`) takes the contacts within range from this peer's own data, a wave runs from the ship to the edge in `PingWaveSeconds` and lights each contact as it passes, an echo fades over `EchoFadeSeconds`; top view around the ship, nose up, `RangeRingCount` rings (`SonarScopeLayout`). The range is the ship's `SonarRange` stat (synced in `ShipStats`; the radar module adds 800 m, never below `MinRangeMeters`). The ping's sound is the view's `AudioSource` (no clip yet). **Contacts** come only through `ISonarContactSource` (Contracts), all bound in `SonarModuleInstaller` and read as one by `SonarContactService`: `RockSonarContactSource` (every large rock of `IPeerFieldRockQuery`, the published field on every peer, is a contact `rock:c:..` / `rock:d:..`; small rocks count per `DebrisCellMeters` world cell, `DebrisMinRocks` make one `debris:x:z` contact), `StationSonarContactSource` (`station`, the run's `TransitDestination`, in flight) and `TargetSonarContactSource` (every balloon of `IPeerFieldTargetQuery` is a `target:<index>` contact; a popped one is gone). A kind is a `SonarContactKind` asset (id, colour, icon, pixels, mark seconds, markable, shown on the edge beyond range: `SonarKind_Station` blue, `SonarKind_LargeRock` red, `SonarKind_Debris` orange, `SonarKind_Target` yellow), never a branch. **Marks:** a click on an echo -> `CmdMark` -> `SonarMarkSystem` with `SonarMarkRule` (`CreateMark` builds the mark; the sender is the navigator, the contact exists now and is markable, at most `MarkLimit` marks; marking a marked contact renews it) -> `SonarMark` (contact id, kind id, position over the object's top, `ExpiresAt` in network time) in the synced `Marks` list, so a late joiner gets the active marks; the server drops a mark when its time is up or its contact is gone (a broken rock). Every peer: `SonarMarkerSystem` lays the marks out on its screen (`SonarMarkerProjection`: over the object, else on the screen edge less `MarkerEdgeMarginPixels` with an arrow) into `SonarMarkerScreenModel`, and `SonarMarkersView` in `GameHudWindow` draws icon, glow, distance and arrow at a constant size. **Broken radar:** `RadarSonarCondition` (`ISonarCondition`) asks `IShipModuleCondition` about the console's socket (`SonarConsoleSync._socket`, bound in `SonarConsoleBindingModel`): a broken radar still opens and pings, but its wave finds nothing and the window shows "NO SIGNAL: the radar needs repair" until a mechanic repairs it (with a tool in the hand the screen's collider offers the socket's repair, not the console). The old minimap (`ShipRadarService`, `ShipRadarScreen`, `ShipRadarCatalog`, the HUD radar) is gone |
| Rock collisions | `ShipModule/Scripts/Impacts/`. `ShipHullVolume` on `ShipPlatform` (root): boxes in ship space around the deck and the modules (gizmo when selected; data only, no collider). Server, every physics step of `Takeoff` / `Cruise` / `Landing` (not `Build` / `Wreck`): `ShipRockCollisionSystem` (`IFixedTickable`) sweeps the hull boxes from the last checked pose to the pose now (`ShipHullSweep`; a jump longer than `MaxSweepMeters`, a world shift or a debug move, is not swept) and `ShipHullRockScanner` asks `IFieldRockQuery` once per step with the box around all of them (field data, no allocation once the lists have grown; about 0.3-0.4 ms per step in a field), then tests every box. `ShipRockImpactClassifier`: a rock is a small hit when the field says `Small` and breakable, else large (giants are always large). Small: `IFieldRockBreaker.ServerBreak` (cause `ShipCollision`, no instigator); only when it broke, `ShipRockDamageRule` damage (linear in diameter 10-35 m from 3 to 12 HP at the reference speed 14 m/s, times speed / 14 clamped to 0.25-2) goes through `IShipHull.ServerApplyDamage` (armor there) and a light push. Large: counted from ahead of the hull along its motion (`ShipRockKnockbackRule.CalculateLargeLookAhead`: 0.8 of the braking distance at the acceleration limit, at most 16 m, + 1 m margin) against 0.8 of the rock's data radius; the first contact knocks the ship away from the rock's middle (5 m/s, horizontal) and back against the heading (1.3 x its forward speed), `LargeRockDamage` (0) if set; while the contact lasts the ship keeps moving out at least 3 m/s; the same rock hits again only after the contact ended + `LargeHitCooldownSeconds` (`ShipRockContactCooldowns`). `ShipKnockback` (pure C#, owned by `ShipFlight`, which only adds its step to the path it follows, so the ship stays where it was pushed and the pad does not move): the target velocity fades as 1 - t^2 over `LargeKnockbackSeconds` (1.4 s), the velocity follows it with at most `LargeMaxAcceleration` (22 m/s2; the takeoff already starts at about 27 m/s2: 350 m forward and 100 m up in 9 s, so the deck feels nothing new; riders are bound kinematically to the deck, so the acceleration only shapes the feel, it cannot throw a player off), the cruise speed comes back as it fades. Poses reach clients through `ShipPoseSync` as before. Feedback on every peer: `ShipImpactSync` (on the `PoseSync` object) sends `RpcImpact(kind, point, strength)` -> `IShipImpactEvents.OnImpact` -> `ShipImpactCameraShakeSystem` (a local player who rides the ship gets `ICameraShakeService.AddShake` with the kind's profile and the hit's strength; profiles `ShipModule/GameResources/CameraShakes/CameraShake_RockImpactSmall` / `Large`) and `ShipImpactEffectSystem` (pooled `ShipImpactEffect`, `GameResources/Effects/ShipImpactEffect.prefab`: dust and chunks, its `AudioSource` plays a clip when one is set). All numbers in `ShipRockImpactConfiguration_Default` (Addressables, `ConfigurationInstaller`, checked by `ShipRockImpactConfigurationValidator`; per kind `ShipRockImpactFeel`: shake profile and effect size). Debug: the debug window's Rocks → "Aim at the nearest small / large rock" on the host in flight put the ship 40 m before the nearest small / large rock at its height and turn it there (`ShipRockImpactDebugSystem`, `IShipRockImpactDebug`, also `DebugMoveShipTo`) |
| Ship damage and repair | `ShipModule/Scripts/Damage/` and `ShipModule/Scripts/Repair/`. **State:** the synced `ShipCondition` model (`BrokenSockets`: socket ids, `OpenBreaches`: damage point ids; bridge `ShipConditionSync` on the ship's `PoseSync` object), so a player joining at a station sees it. **Contracts (every peer):** `IShipModuleCondition.IsWorking(socketId)` + `OnConditionChanged` (read by the turret, `ShipTurretStation`: no fire while broken; the future sonar reads the same), `IShipBreachCondition` (`IsOpen`, `OpenCount`). **Server:** `IShipConditionService` (`ShipConditionService`) is the one writer: break / repair a module (the stat sheet suspends / resumes exactly that socket's modifiers, `ShipStatSheet.SuspendModule` / `ResumeModule`, never losing them), open / repair a breach (a repaired breach heals `HullPerRepairedBreach` through `IShipHullRepair.ServerHeal`), repair all (landing, `ShipLandingRepair` under `ShipHullConfiguration.RepairOnLanding`); a module taken off in `Build` loses its broken state, so it works when reinstalled; a ship reset clears everything. Events for later systems: `IShipDamageEvents` (`OnModuleBroken`, `OnHullBreached`, `OnRepaired`; server only, not sent for the landing repair). **Impacts:** `ShipRockCollisionSystem` publishes every hit on the server (`IShipServerImpactEvents`) -> `ShipImpactDamageSystem` -> `ShipImpactDamageService`: `ShipDamageRule` (pure) picks the installed `CanBreak` module nearest to the hit within `ModuleSearchRadius` with chance `ModuleBreakChance` x impact strength x `ModuleDefinition.BreakChanceMultiplier`, and the nearest free damage point with `BreachChance` x strength while fewer than `MaxOpenBreaches` are open. `ShipHullLeakSystem` leaks `LeakHullPerSecond` per open breach in flight through `IShipHull` (cause `HullLeak`), gathered and dealt in steps of `LeakStepHull` by the pure `ShipHullLeakRule`. Numbers in `ShipDamageConfiguration_Default` (Addressables, `ShipDamageConfigurationValidator`); the helm has `CanBreak` off. **Places on the ship (`ShipPlatform`):** 11 `ShipHullDamagePoint`s around the deck edges under `HullDamagePoints` (one NetworkIdentity; ids `breach-01..11`), each with a dent look, a red outline and `ShipBreachRepairInteractable` (its trigger is on only while the breach is open; both repair places share `ShipRepairInteractableBase`: the tool kind, the hold seconds and the repair volume); every socket has `ShipModuleRepairInteractable` with a `RepairVolume` trigger over the module, on only while it is broken (slot views have no colliders and the engine socket plates sit in the deck), and `ShipSocketDamageView`; the look is `ShipDamageVisual` (`GameResources/Effects/ShipDamageEffects.prefab`: smoke + sparks while damaged, a flash and the repair sound hook `AudioSource` (no clip yet) on repair, on every peer; the module effects live under `ModuleDamageEffects`, outside the sockets, so the socket outline does not paint them). **Tools:** a tool is one `RepairToolDefinition` (`GameResources/Repair/WrenchTool`, `HammerTool`: id, name, price, item prefab, what it repairs (`ShipRepairKind` Module / Breach), speed: repair seconds = `RepairSeconds` / speed); items `WrenchItem` / `HammerItem` (`Grabbable` + `RepairTool`, registered in the network manager's spawn prefabs); `ShipToolRack` on the deck stocks one of each at the run start (`ShipRunService.Bind`, made under the rack so they stay in the game scene; `ConnectionSceneObjectIds` gives no scene id to an object the server already spawned, or a client would look for these tools in its scene); the shop sells them after the modules (`ShopCatalog.Tools`). **Hold to use** is general Grab code: an `InteractableBase` that returns `TryGetHoldSeconds` is used by holding interact: `UseController.CmdBeginHold` / `CmdEndHold`, the server runs `HoldUseProgress` (pure) and cancels it when the target stops being usable, the player leaves reach or another item is in the hand, then calls `ServerUse`; `CmdUse` refuses hold targets. The client ends the hold on key release (`Grab.Canceled`) or when the crosshair leaves the target; the progress is a SyncVar on `UseController` -> the local `HoldUseModel` -> the HUD bar. The progress is not kept across a reconnect. Tooltips: `Tooltip_ModuleBroken` / `Tooltip_ModuleRepairReady`, `Tooltip_Breach` / `Tooltip_BreachRepairReady`, `Tooltip_Wrench` / `Tooltip_Hammer`; `ShipModuleRepairTooltipProvider` shows "Broken: needs a wrench" on any tooltip of a broken module's socket. **HUD:** `ShipConditionView` / `ShipConditionPresenter` in `LobbyModule` under the hull bar: hull, a tile per installed module (`ModuleDefinition.Icon`, its name while it has none; green, red when broken, blinking when it just broke: `ShipHullHudConfiguration.BrokenBlinkSeconds`) and the open breaches; `HoldUseProgressView` under the crosshair. Debug: the debug window's Ship → "Break a module + open a breach" on the host breaks a random working module and opens a random free breach (`ShipDamageDebugSystem`, `IShipDamageDebug`) |
| Deck debugging | `ShipModule/Scripts/Debug/` (editor / development builds only): `DeckFeelHarness` (deck feel measurements), `ShipStatsDebugOverlay` (also the hull), `ShipHullDebugSystem`, `WeaponDebugTargetSystem`, `ShipRockImpactDebugSystem` (see "Rock collisions"), `ShipDamageDebugSystem`: each registers its actions in the debug window (see "Debug window"); `ShipDebugActionConditions` holds their availability checks (host, a ship, in flight). There are no debug keys any more |
| Debug window | `Assets/Features/DebugModule/` (editor / development builds only, the whole assembly). A small see-through **DEBUG** button in the bottom-right corner of the game HUD (`DebugWindow`, prefab `GameResources/Prefabs/DebugWindow.prefab`, Addressables group `Windows`, opened by `DebugWindowSystem` for the whole session) opens and closes the debug panel; the panel frees the cursor while it is open (`CursorModel`; walking stays on, a seated gunner's turret ignores the mouse as with any free cursor). **Tab** frees the cursor to reach the button, **Esc** or the button closes the panel. The panel lists the registered actions by section in the order the sections were first registered (Ship, Rocks, Weapons, Player, Shop): a button with a clear label, a number field for an action with a value (the hull hit's damage), a hover hint with the description, and a greyed-out button with the reason next to it when the action cannot run now (host only, not in flight, no local player...; `DebugActionAvailabilitySystem` asks every action each frame while the panel is open, `DebugActionsModel` tells the panel only changes). Register an action: inject `IDebugActionRegistry`, call `Register(new DebugActionDefinition(section, label, description, readUnavailableReason, run))` in `Initialize` and dispose the returned handle in `Dispose`; the class and its binding stay inside `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` (a release build has no registry, no button, no window). Actions now: Ship → "Hit the hull" (damage, default `ShipHullConfiguration.DebugDamage`), "Destroy the hull (hull = 0)", "Break a module + open a breach"; Rocks → "Aim at the nearest small rock", "Aim at the nearest large rock" (in flight), "Break the nearest rock"; Weapons → "Spawn the weapon test dummy", "Remove the weapon test dummy"; Player → "Kill me" (any player), "Toggle dead / alive (stand-in life)" (only with the fallback `DebugPlayerLifeQuery`); Shop → "Open the shop here" (`ShopDebugOpenSystem`, the former **L** key, any player). The Ship, Rocks and Weapons actions are host only. The `UiMap/Shop` input action (L) is still in the generated input asset, but nothing listens to it any more |
| Death, damage, spectator | `Assets/Features/PlayerLifeModule/`: `PlayerLifeSystem` (life state machines, all dead → lobby), `PlayerDamageable`, `FallKillSystem`, `SpectatorSystem`, `PlayerLifeConfiguration`, `PlayerDamageConfiguration`; the debug window's Player → "Kill me" (`DebugLocalPlayerKillSystem`) kills the local player in editor / development builds |
| Tooltips | `Assets/Features/TooltipModule/`: `InteractionTooltip` on the object, `TooltipContentService`, `Tooltip_*.asset` in `GameResources/Configurations/` |
| Synced models | `Assets/Features/NetworkModelModule/` (model + bridge + generator), see "Synced models" |
| Interaction | `Assets/Features/GrabModule/`: `LocalPlayerInteraction` (aim, highlight, button; press and release for hold targets), `GrabController` (hand, `PlayerHand` model), `UseController` (single use and the server-run hold to use, `Hold/`), `HeldItemRegistry`, `InteractableBase`, `Grabbable`, `GrabConfiguration`. Seats: `ShipSitInteractable` seats a player (`ShipSeats`); what the seated player does lives in the slot view's `ISeatStation` (a turret: `ShipTurretStation`; the helm: `ShipHelmStation`): a new seat role is a new station, not a branch in the rider or the seats. There is no seat role enum any more (`ShipSeatRole` is gone): `ShipSeat` is only the sit point. **Occupancy** is keyed by the player's stable `PlayerKey` (rule "Player data and rejoin"): `ShipSocketState.OccupantKey` in the synced `ShipSockets` model, written by the server through `ShipSeatRule` (one occupant, only the occupant stands up); every `ShipRider` carries its key in a SyncVar the server writes at spawn (`ShipRider.Key`), and any peer finds the player of a key with `IShipRiders.TryFindByKey` (a lookup in `ShipRiderKeyModel`, where every rider registers itself while spawned on that peer, riding or not). A disconnecting player frees its seat (`ShipRider.OnStopServer` -> `ShipBase.ServerRemoveRider`), so a player who comes back with the same key stands on the deck at a spawn point and the seat is free. **The helm** (`HelmItemView` with `ShipHelmStation`, `ShipModule/Scripts/Helm/`): on the seated owner's peer the move stick's sideways axis (A / D) steers; the host's flight reads its own stick at once, a client pilot sends it at most every `ShipFlightSettings.HelmSteerSendSeconds` (0.03 s) through its rider's generic seat input (`ShipRider.OwnerSendSeatInput` -> `ShipSeats.ServerReceiveSeatInput`, which hands it only to the station of the seat the sender holds, `ISeatStationOwnerInput`); `HelmSteer` keeps that steer for the current pilot only, so a pilot who leaves or stands up steers nothing. The flight asks `IShipSteering` (`ShipSteering`: the first slot view implementing `IShipSteeringStation` with a pilot) for the steer, and the client pose sync asks it whether the local player steers (no interpolation delay then). Jump stands up from any seat (`ShipRider`) |
| Shop and money | `Assets/Features/ShopModule/` (`CrewWallet`, `ShopCatalog`, `WalletConfiguration`); income: `CrewWallet.ServerEarn` (the server reaches the wallet through `ICrewWalletBindingModel`), paid by `FieldTargetRewardSystem` for every popped target balloon; the HUD balance flashes on a rise; the kiosk `ShopKioskInteractable` is part of the `StationPad` prefab, `ShopAccessService` / `ShopAccessRule` decide open, keep-open and purchase (Build, alive, distance from `ShopKioskConfiguration`), the server keeps the opened kiosk per player in `ShopVisitModel`; the debug window's Shop → "Open the shop here" opens it anywhere, only in editor / development builds |
| Buying back a dead crewmate | the shop window's "Crew" section (`CrewReviveView` / `CrewRevivePresenter`, list `DeadCrewModel`, kept by `DeadCrewSystem` from the synced `PlayerLifeRegistry`) → `CrewWallet.CmdRevive` → `ReviveShopSystem` (kiosk check like a purchase, `ReviveRule`, then `ServerTrySpend`, then `IPlayerLifeReviver.ServerRevive(PlayerKey)`); price in `ReviveConfiguration`; the revived body is teleported to the ship's next `ShipDeckSpawnPoint` in turn (`IShipDeckSpawnPoints`); `ReviveConfigurationValidator` fails startup on a price <= 0 |
| Joining a running session | `Connection/Scripts/Game.Connection/Join/`: `JoinRule` (pure: lobby or `RunJoinWindow.AtStation`, free slot out of `ConnectionConfig.MaxConnections`, version, Steam id; reasons `InFlight` / `ServerFull` / `MapChanging`), `ConnectionJoinPolicy` (the rule on the live session, used by `ConnectionAuthenticator` once at authentication; `OnJoinStateChanged` is the one event for "an input of the rule changed": run window, map change, players), `SteamLobbyJoinableSystem` (Steam lobby joinable exactly when the rule lets a new player in), `ConnectionRejectModel` + `ConnectionRejectMessages` (the reason shown in the menu; `RejectedJoinMenuSystem` in GameCore switches a turned-away client to the menu state). Connection reads the run only through the contracts `ISessionJoinGate` (`ShipRunJoinGate`) and `ISessionJoinSpawn` (`ShipDeckJoinSpawn`, the next `IShipDeckSpawnPoints` pose) in `GameCoreModule.Contracts`. A mid-run joiner loads the map through Mirror's scene message and reports `MapLoadedMessage`; `ConnectionLateJoin` makes its player only then, on the deck. The transport allows one connection over the slots so an extra player hears "the crew is full" |
| Inviting friends ("Hire crew") | `Assets/Features/CrewInviteModule/` (`Scripts/Core`: rule and status, `Scripts/Network`: terminal, inviter, roster bridge, `Scripts/UI`: tooltip, `Data`: the generated model): the terminal `CrewInviteInteractable` stands in the lobby and on every `StationPad` next to the kiosk; E sends `CrewInviter.TargetOpenInviteOverlay` to the pressing player, who opens `ISteamLobbyService.OpenInviteOverlay` (Steam invite dialog for the current lobby). `CrewInviteRule` (pure) gives `Available` / `CrewFull` / `NotAtStation` / `NoSteam` / `OverlayDisabled` from the Steam side and the shared `CrewRoster` model (players, slots, join window from `IConnectionJoinPolicy`, written by the `CrewRoster` bridge on the server on `IConnectionJoinPolicy.OnJoinStateChanged`); `CrewInviteTooltipProvider` picks the `Tooltip_CrewInvite*` asset, fills "N/M" and refreshes on the roster and on `ISteamLobbyService.InviteStateChanged` |
| Player names and nameplates | `Assets/Features/PlayerProfileModule/`: `PlayerProfile` (bridge of the `PlayerProfile` model, the server fills the name), `PlayerNameSanitizer`, `PlayerNameplate` (TMP over other players' heads), `PlayerNameplateConfiguration`; the model is in `Data/` |
| Stage presets (playtests) | `Assets/Features/StagePresetModule/`: a preset is one `StagePreset` asset (`GameResources/Presets/Early`, `Mid`, `Late`: id, name, description, start loop, optional field profile id, module per socket id, starting balance, station drops on / off, optional extension blocks) in `StagePresetCatalog_Default` (`Configurations` group; `StagePresetCatalogValidator` checks it at startup, the sockets are checked against the ship at the run start by `StagePresetSocketResolver`). The host's choice is the server-side `StagePresetSelectionModel`; `StagePresetSync` (a scene object in LobbyScene and GameScene) carries it to every client and late joiner in the synced `RunStagePreset` model. `StagePresetApplier` is the one server place that turns it into the run: `IShipRunStage` (Ship: start loop and drops read by `ShipRunService.Bind` / `ShipStationDropService`, modules installed through `ShipSocket.ServerTryInstall` after `ServerResetForBuild`, the field profile through `IFieldProfileDebug`) and `IWalletStartingBalanceSource` (Shop: `CrewWallet`'s starting balance). The run's end (`ShipRunService` reset: back to the lobby or the menu) clears the choice. UI in `GameHudWindow`: `StagePresetPickerView` (host, lobby only, editor / development builds or `StagePresetCatalog._isPickerShownInReleaseBuilds`) and `StagePresetLabelView` ("Preset: Late" for everyone) |
| Scenes | `GameCoreModule/GameResources/Scenes/`: Bootstrap → Menu → Lobby → Game; only Bootstrap is in Build Settings |

### How to verify
Play mode starts from `BootstrapScene`, then menu → Host → game. Two-player multiplayer is checked with the
ParrelSync clone (`MirrorCoopBase_clone_0`).

### Playtest stage presets
To test mid- and late-game fun without flying 15 loops, the host picks a stage preset in the lobby before Start: the
"Stage preset (playtest)" panel under Start offers Normal run, Early (loop 0, two propellers, 300), Mid (loop 4, two
large engines, the radar and the minigun, 1 500) and Late (loop 9, the same modules, 2 400); presets give no free station drops.
Everyone sees "Preset: <name>" under the balance; the choice ends with the run. The panel shows only in editor and
development builds (release: `StagePresetCatalog._isPickerShownInReleaseBuilds`). A new preset is a new `StagePreset`
asset in `StagePresetCatalog_Default`: modules from the `ModuleCatalog`, socket ids of `ShipPlatform` (`engine-left`,
`engine-right`, `radar`, `control`, `turret-front`). A system that needs its own preset setting (hull HP, balloon density, ...) puts a
ScriptableObject block into a preset's extensions and reads it with
`IStagePresetSelectionService.TryGetSelectedExtension<T>`; the preset core does not know the block. The target balloons
do so: `FieldTargetPresetBlock` (`GameResources/Presets/Extensions/`) scales the balloon count, read by
`StagePresetApplier`.

### Loop autotest
The PlayMode test `LoopSmokeTest` (`Assets/Tests/PlayMode/LoopSmoke/`) plays the M1 loop on a host by itself:
menu → Host → lobby → Start → `GameScene`, installs 2 engines, takes the radar off, drops, picks up and reinstalls it by hand through the real third-person
aim (`LoopSmokeHandCheck`), then plays the mechanic by hand (`LoopSmokeRepairCheck`: the debug break entry (`IShipDamageDebug`) breaks the left engine, the
synced speed drops and the condition shows it; the wrench from the deck rack is grabbed through the real aim, a hold on
the engine released early repairs nothing, a full hold repairs it and the speed is back; after a small hull hit the
hammer patches a breach and the hull is full again; in the first cruise a random module breaks and a breach opens, and
the landing repairs both), takes off and lands twice (in the first cruise `LoopSmokeHullCheck` hits the hull through
the debug window as a player does: it clicks the corner button (the panel opens, the cursor is freed), types the damage
into "Hit the hull" and clicks it, clicks the corner button again (closed, cursor released), and checks the armor-reduced drop in the synced model, after the landing that the hull is full again), and checks the phases,
`LoopIndex`, engines after landing, the ship on the new pad, one shop kiosk per pad, the player on deck, no wreckage.
At the first station it reaches it buys back a dead crewmate with a second player, a bot on the host (`LoopSmokeReviveCheck`:
the price is taken once for a double click, no buy-back without money, the revived player stands at a deck spawn
point; then the host dies and the bot buys them back). The bot joins through the server's real receive path
(`LoopSmokeBot`, `LoopSmokeTransportInput`). `LoopSmokeRejoinCheck`: a new player joins at that station on a deck spawn
point, dies, leaves in flight, a join in flight is turned away with `InFlight`, and after landing it comes back dead, in
the Crew list, under its new name. After the second landing everyone dies, the crew returns to
the lobby and `LoopSmokeRunLeakCheck` checks that no object of the first run is left (and that run 2 starts with the same
station items and spawns the same pad and drops on its first landing). Then it starts a second run, flies five loops (after the first one it checks the station's crew terminal and puts the
player back on the deck)
in a row and at the last station buys 5 items at the kiosk through the real aim and the window (`LoopSmokeShopCheck`: delivery
point, window closes when walking away, the bought items come to rest within a timeout, a far purchase is refused), destroys the hull
(`LoopSmokeSession.ReturnToLobbyByHullLossCoroutine`: hull 0 -> the crew in the lobby), returns to the menu and checks that there are no
errors in the log. Before that check `LoopSmokeRejectCheck` joins a silent loopback "host", hands the client an
`InFlight` answer and checks the reason text in the menu. `LoopSmokeCrewInviteCheck`: one "Hire crew" terminal in the
lobby and one per pad, inactive with "Steam is not running." under the real aim (no Steam in the test; E on it asks
`LoopSmokeSteamLobbySpy`, which stands in for `ISteamLobbyService`, for no overlay), the
`CrewRoster` counts filler bots up to the slot limit (crew full) and back, and closes the join window in flight. Every
flight runs `LoopSmokeFieldCheck`: in cruise a new
flight field stands with an active local object for every local placement within the build radius of the camera (none beyond the release radius) and one Mirror object per networked placement;
after landing nothing of it is left (what a player joining at that station gets); in run 2 loop 3 a forced world shift
in cruise keeps the field in place against the ship (`LoopSmokeFieldRunCheck`, which also asserts at the last station of
run 2 that a networked placement was checked); the field pool never grows past what the fields needed. Before
the first launch `LoopSmokeSonarCheck` stands the host behind the reinstalled radar (its console is inactive at the
station); first in the cruise of run 1 loop 1 it takes the console through its use command (what E sends): the
`SonarWindow` opens, the synced model names a navigator and walking / aim are blocked; the window's Ping button
lights the station's echo at the radar's 800 m range, a click on that echo puts a station mark into the synced
`Marks` and a marker on the HUD, the Leave button closes the window, frees the console and the controls, and the mark
runs out (`LoopSmokeSonarConfigOverride` binds a copy of `SonarConfiguration` whose station marks last 2 s). In the
cruise of run 1 loop 1 `LoopSmokeRockBreakCheck` breaks the rock nearest to the ship through `IFieldRockBreakDebug`:
it is in the live field's `BrokenRocks`, `RockDestroyed` fired once (a second break is `AlreadyBroken`), the rock is
hidden on the host, and after landing the broken list is empty. In the same cruise `LoopSmokeRockImpactCheck` flies
into rocks through `IShipRockImpactDebug` (the debug window's rock aim entry; the hit is the real server check): a small rock breaks
once (broken list, `RockDestroyed` once), the synced hull drops and a small impact arrives; a large rock gives a large
impact, stays whole, the ship never gets within the contact radius and moves back out, and the player still rides on the
deck (in ship space); then the ship goes back and faces the destination. Every
takeoff logs its longest frame (`[LoopSmoke] loop N takeoff`). The test shortens flight timings with its own copy of
`ShipRunConfig` and adds the debug networked beacon to its own copy of `FieldStructureCatalog` and, as a giant class of its own, to its own copies of the field configuration's difficulty profiles (weight 2 against about 1 of the game's classes, so nearly every short test field has a networked
placement); assets are not changed. After the shop check the crew returns to the lobby and `LoopSmokeStagePresetCheck` plays a third run with the
`Late` stage preset: the host clicks Late in the lobby picker, the run starts on loop 9 with the preset's modules,
balance and "Preset: Late" label, the first flight's field comes from loop 9's table entry, the first station drops
nothing, and back in the lobby the preset is gone. In run 2 the minigun goes into `turret-front` at the station after loop 1 and the
host stands by it; in the cruise of loop 2 `LoopSmokeTurretCheck` spawns the debug dummy, sits the host at the turret
through the seat's use command, aims it with the station's test hook (`ShipTurretStation.DebugAimAt` /
`DebugHoldTrigger`, editor / development only) until the server has accepted 5 hits on the dummy, then
`LoopSmokeRockShootCheck` brings its own target (the ship is put beside the nearest small breakable rocks in turn
until one is in reach and clear sight of the turret, and put back afterwards) and fires until it breaks (in the broken list,
hidden on the host, `RockDestroyed` once with cause `Weapon`, the host's key and `minigun`), then `LoopSmokeTargetCheck`
puts the ship beside the nearest target balloon in clear sight and fires until it pops, and stands up again; then the
ship is put before another balloon and flies into it (the real hull check). Each balloon is in the synced popped list,
hidden on the host, `TargetPopped` fired once (Weapon by the host with `minigun` / `ShipCollision`) and the synced crew
balance grew by the rewards; after that landing the popped list and the balloons are gone.
The test timeout is 180 s (a run takes about 150 s). The shared steps live in helpers both loop tests use:
`LoopSmokeSession` (menu, Host, lobby, Start, all dead -> lobby, Leave; waits record the run phases),
`LoopSmokeFlight` (engines, boarding, the lever, the phases of a loop with the field checks and the checks after
landing, messages start with "Loop N:"; at the cruise start its pilot faces the ship to the destination once through
`ShipBase.FlightControlDebug`, because the autopilot keeps the takeoff heading), `LoopSmokeShipRunConfigOverride` and `LoopSmokeFieldCatalogOverride` (the
config copies) and `LoopSmokeSpawnCounts`.

**Adding a check to the loop test.** `LoopSmokeTest.cs` only plays the runs (its `_runs` list: flights per run, the
test's rocket engines or a preset's modules, how the run ends) and does not change per feature. The checks register
themselves: `LoopSmokeCheckRegistry` reads every method with `[LoopSmokeStep(phase, run, flight, order)]` in the
assembly and runs it at its slot. A new check is a new class in `Assets/Tests/PlayMode/LoopSmoke/` with one attribute
line per step, nothing else:
- Phases (`LoopSmokePhase`): `InLobby` (before the run's Start), `BeforeTakeoff` (at the station before the flight's
  launch, after the engines and the boarding), `InCruise`, `AfterLanding` (after the flight's landing checks),
  `AfterRun` (back in the lobby) take a run from 1; the flight phases also take the flight of the run from 1 (the n-th
  flight: `LoopIndex` for runs 1 and 2, loop 10 is flight 1 of the Late run 3); `InMenu` (after Leave) takes neither.
- `order`: the steps of one slot run one after the other, lower first; two steps with the same order in a slot fail
  the test at its start, so pick a free number (the existing steps use multiples of 10; the first cruise of run 1:
  sonar 10, hull 20, rock break 30, rock impacts 40, repair 50; the turret cruise of run 2: dummy 10, rock shot 20,
  balloon shot 30, stand up 40, fly-in 50).
- A step takes no arguments and returns `IEnumerator` or `void`. Its class has a public constructor taking
  `LoopSmokeContext` (session, flight helper, field check, Steam lobby spy) or an empty one; it is made at its first
  step and kept for the whole test, so its steps share state (resolve scene services at a step if they cross runs).
- At the end the test fails if a registered step never ran (a slot the runs never reach).
Why attributes and not one list file: a list is the same shared file every feature edits; a check that registers in
its own file merges without conflicts. To change the scenario itself (one more run or flight) edit `_runs`.

The ride test `ShipRideTest` (`Assets/Tests/PlayMode/ShipRide/`, about 75 s, `-Filter ShipRide`) flies six loops on a host on the
same fast timings, one per spot of the deck found from its geometry (`ShipRideDeckSpots`): the nose, the stern, both
side edges, the centre and the stern in a jump at the lever (the lever is pulled at the top of a real jump through
`CharacterInputBuffer`). After takeoff and in cruise the player must ride, be alive and stay on the deck; in cruise it
jumps (from the centre with a sideways hop) and must land back on the deck still riding.

The soak test `LoopSoakTest` (`Assets/Tests/PlayMode/LoopSoak/`, `[Category("Soak")]`, about 3 minutes) flies 15
loops in a row on the same fast timings and catches what only shows up over time. It does not repeat the station
checks of `LoopSmokeTest`. Every loop: the smoke test's flight, field and landing checks; at most 2 landing pads, no
wreckage, and runtime network objects other than items and pads exactly as after loop 1 (`LoopSoakAccumulationCheck`);
the landed ship closer to the origin than `OriginRecenterDistance` and, after a world shift, the player on the deck
(`LoopSoakWorldShiftCheck`). At least one world shift must happen (the default 800 m gives one every second loop) and
at least one networked field placement. It logs a table per loop (`LoopSoakReport`: time, managed and allocated
memory, spawned objects, items and their growth, pads, shifts, the ship's distance from the origin, field placements,
field objects in cruise and pooled field objects) and fails when memory at the last loop is more than 1.5 times that
of loop 2. Station drops are never removed, so items grow by about 10 per loop: reported, not failed.

The whole EditMode suite (about 1 000 tests) takes about 2.5 minutes (2 min 34 s, 1027 tests, on 2026-10-25): `Invoke-AgentEditor.ps1 -AgentDir . -Command run-tests -Mode EditMode`. While a test run is on, an error of the MCP for Unity bridge (message with the `MCP-FOR-UNITY` prefix, e.g. a lost websocket) goes to the log as a warning, so it fails no test, OneTimeSetUp or loop test error log; only that prefix, in one place: `Tests/Shared/Editor/TestLogBridgeErrorFilter` (editor, wraps `Debug.unityLogger.logHandler`, run state from the Test Runner callbacks; `TestLogBridgeErrorFilterTests`).

EditMode guard tests keep the data rules: `ConfigurationLoadingGuardTests` (no `Resources.Load`, every configuration
address loads), `ModuleCatalogGuardTests` (module ids set and unique, shop and drops only use catalog modules, no code
checks a concrete module), `ShipSocketIdTests` (every ship socket has a unique id), `FieldStructureCatalogGuardTests`
and `FieldNetworkedStructureGuardTests` (field structure ids, prefabs and footprints; networked structures are Mirror prefabs registered
with Mirror; no debug structure in the game catalog or the spawn prefabs), `WeaponCatalogGuardTests` (weapon ids set and
unique, every weapon has all its parts, the startup validator passes and fails on a shared id, no weapon code branches on a concrete weapon).
The turret's pure parts have EditMode tests too: `TurretAimTests` (mouse to yaw / pitch, limits, wrap, the synced
direction round trip), `TurretAimSmoothingTests`, `TurretAimSendScheduleTests`, `TurretOwnerInputTests` (interact stands
up, a free cursor keeps the mouse and the trigger off the turret), `SeatedOwnerBodyTests` (the owner's hidden body keeps its shadow and comes back as it was), `SeatStandingSpotTests` (standing up from the turret clamps the spot onto the deck, a fixed seat keeps it), the helm's and the seats' `HelmSteerTests` (the host pilot's stick, a client pilot's sent steer and its send cadence, no steer left from a pilot who stood up) and `ShipSeatRuleTests` (one occupant by key, only the key holds the seat, a rejoined player's key, a freed seat equals a never-seated one), and in the weapon tests `WeaponHitQueueTests`,
`WeaponHitServiceTests` (the server hit path) and `WeaponShooterTests` (only the holding peer reports hits).

Run from the workspace root:
`Invoke-AgentEditor.ps1 -AgentDir . -Command run-tests -Mode PlayMode -Filter LoopSmoke`
(or Test Runner → PlayMode → `LoopSmokeTest`); the soak test with `-Filter LoopSoak` (or `LoopSoakTest`), its table
is in the editor log under `[LoopSoak]`.

**Run it before every merge that touches the ship, landing, shop or session.** The test lives in Assembly-CSharp
under `#if UNITY_INCLUDE_TESTS`, so `playModeTestRunnerEnabled` is on in `ProjectSettings`. New allowed log errors go
only into the `LoopSmokeErrorLog` allow-list, with a reason.

### Two-player test
`CoopTwoProcessTest` (`Assets/Tests/Coop/`) plays the loop with a **real second client process**: the PlayMode test in
the editor is the host, a Windows development build of the game is the client. One command from the workspace root:
`Invoke-AgentEditor.ps1 -AgentDir . -Command run-tests -Mode PlayMode -Filter CoopTwoProcess`
(or Test Runner → PlayMode → `CoopTwoProcessTest`). It takes about 70 s, plus about 30-80 s when the client is rebuilt.

- **The client build.** Before Play mode the test's `PrebuildSetup` (`CoopClientBuild`, editor) builds Addressables
  content and a Win64 development player of the Build Settings scenes into `MirrorCoopBase/Builds/CoopClient/`
  (git-ignored). It skips the build when the content of `Assets`, `Packages` and `ProjectSettings` is the same as
  after the last build (a file hash list in `Builds/CoopClient/build.stamp`; the build's own outputs, the Test
  Runner's scene and render textures are left out). By hand: **Tools → Coop Test → Build Client**. The build re-saves
  a few settings assets with LF line endings (no content change); do not commit them.
- **The client bot** (`Assets/Tests/Coop/Client/`, only under `UNITY_EDITOR || DEVELOPMENT_BUILD`, not in a release
  build) starts when the player is launched with `-coopTestClient -connect <ip> -port <port> -scenario
  leave-and-rejoin -coopDir <folder> -logFile <file>`. It joins through the menu's address field and Join (Telepathy,
  a free port the test picks, not Steam), boards at the stern (`ShipDeckBoarding.PutAboveStern`), rides the loops the host flies, takes the helm after the first landing (put
  beside the seat, the seat's use command, then D held through the scripted move stick, `seated-at-helm`), steers the
  next cruise for 0.5 s and leaves in flight from the helm, is turned away in
  flight, rejoins at the next station on the deck, dies (the dev kill command, like the debug window's "Kill me"), waits to be bought back, rides
  one more loop and quits.
- **No Steam identity.** Both processes run on one Steam account, so `CoopSessionActions.PrepareDirect` sets
  `ConnectionAuthenticator.UsesSteamIdentity = false` (editor / development builds only): they authenticate as
  `CoopHost` / `CoopClient` with no Steam id (a `client:` player key), as without Steam, instead of the persona name.
  The test passes with the Steam client running; Steam itself still starts. Normal runs keep the Steam name.
- **Exchange.** The client appends a JSON line per step to `<coopDir>/client-reports.jsonl` (phase, `LoopIndex`,
  installed sockets, ship stats, crew balance, flight field number / seed / placement count, the nameplate it sees
  over the host, its own position on the deck, life state, spectating, the crew's sonar marks, reject reason, its
  logged errors); the host
  answers with commands in `<coopDir>/host-commands.txt` (`rejoin`, `finish`). The host captures the same fields
  (`CoopObserver`) and compares them at every step (`CoopObservationComparison`), and checks the client's server
  position on the deck, the `InFlight` refusal text in the client's menu, the buy-back at the kiosk through the shop
  window and that neither side logged an error. The host also installs the radar and, in the first cruise, takes the
  sonar console, pings and marks the station (`LoopSmokeSonarCheck.MarkStationInCruiseCoroutine`); the client waits
  for the mark in its own synced `SonarConsole` model and reports `sonar-mark-seen` with it. At the helm (`CoopHelm`)
  the host checks the seat held by the client's key on the server, the client's rider seated, its own helm station
  with a remote pilot and the client's body at the sit point; in the next cruise the client still steers on its peer
  and the ship turned right; after the client left in flight the helm and its station are free (the host faces the
  ship to the destination again), and the rejoined client is not seated and the helm is free.
- The test always kills the client process (also on failure or timeout). A passed run deletes its folder
  `Builds/CoopRuns/<time>/`; a failed one keeps the client's log and the exchange files there, and the failure message
  ends with the tail of the client's log.
- Shared code, one copy each: `Tests.Shared` (`SessionQueries`: menu / game-at-station / back-in-menu checks, the
  network manager, `Resolve<T>` from the project or a scene context, `IsOnDeck` (ship space, over any deck walk box
  within 0.5 m and not 0.5 m below its surface; the world bounds of the first deck box missed the console corner at
  some landing headings), `ContainsXZ`; `ShipDeckBoarding`) is used by the
  loop tests, the coop host test and the client build (no NUnit there); the host side reuses `LoopSmokeSession`,
  `LoopSmokeFlight` and `LoopSmokeShipRunConfigOverride`.
- `Docs/COOP-CHECKLIST.md` is the manual two-player pass; its "Auto" column says what this test already covers.

## Synced models

Data that must match for all players lives in a plain C# model. A **bridge** (`NetworkBehaviour`) carries it over the network. The model is read-only: UI and logic subscribe to its events, and only the bridge may write to it.

There is one flow: server logic → `ServerSet<Field>` (or a server collection method) → SyncVar / SyncList / SyncDictionary / SyncHashSet → hook or callback → model → `On<Field>Changed` and `OnChanged` → UI. On the host the hook is called right from the SyncVar setter (`NetworkServer.activeHost`); on a client, during deserialization. Collection callbacks fire on whoever changes the collection, and on a client on a delta. `OnStartClient` puts a full snapshot into the model once more: a full spawn of SyncList / SyncDictionary / SyncHashSet does not fire callbacks, and a SyncVar hook is skipped when the value equals the field default. This way host, client and late join go through one path. On a dedicated server the SyncVar hook is not called from the setter, so `ServerSet` updates the model itself when `activeHost == false`.

A client does not write to the bridge. A client request is its own `[Command]`: it calls a server service, and the service calls `ServerSet`. This is how purchase works (`CrewWallet.CmdPurchase` → `ServerTrySpend` → `ServerSetBalance`), and the `DebugCounterSample.CmdBump` example.

A model can be marked atomic: all scalar fields travel as one struct in one SyncVar, so a client never sees a mix of old and new values. Collections stay separate sync collections.

A definition has a `Scope`: `Shared` or `PerPlayer`. `Shared` is one model for everyone, like the wallet. `PerPlayer` is a separate model per player. The generator writes a `<Name>Registry` / `IReadOnly<Name>Registry`, bound `AsSingle`. The key is a `PlayerKey` from `IPlayerIdentityService.GetKey`: Steam gives `steam:<steamId>`, direct IP gives `client:<guid>` from `PlayerPrefs` (`network-model.client-id`). Not `netId` and not `connectionId`. The registry provides `TryGet`, `Local`, `IsOnline` and added / removed / online events. The bridge goes on the player prefab: on spawn the server finds or creates the entry, pushes it into the SyncVar, and on disconnect only marks it offline. Clients put their own and other players' models into the local registry. The entry outlives the player object, so rejoining with the same key gets the same fields.

### Adding a model
1. `Tools/Network Models` → Create. The same definition is available in `Tools/Generations/Code generation` as a sub-generator.
2. Name, namespace, folder inside `Assets/`, fields: name, type, optional default as text. The type comes from the list or is a string. Supported: primitives, `string`, enum, `Vector2/3/4`, `Vector2Int/3Int`, `Quaternion`, `Color`, `Color32`, `Rect`, your own serializable struct or class, `List<T>`, `Dictionary<K,V>`, `HashSet<T>`, `NetworkIdentity`, `GameObject`, `uint`. The generator rejects unknown types.
3. Generate or Generate All. Files with the `// <auto-generated>` header and `_g.cs` suffix: `IReadOnly<Name>Model`, `<Name>Model`, `<Name>Bridge`, plus `<Name>State` when atomic, plus `<Name>ModelInstaller`.
4. Add `<Name>ModelInstaller.Install(Container)` to `DataInstaller`.
5. Put `<Name>Bridge` on an object with a `NetworkIdentity`, or inherit the bridge (like `CrewWallet`). For `PerPlayer` the bridge goes on the player prefab, and `DataInstaller` installs the registry, not a single model.

A reference to a network object is a `NetworkIdentity` or `GameObject` field: Mirror stores the netId and resolves the reference once the object is spawned. `uint` is a plain number; you can pass a raw netId with it and look the object up in `NetworkClient.spawned`. Do not put a `NetworkBehaviour` into a SyncVar.

### Do / don't
- Do read `IReadOnly<Name>Model` and listen to `On<Field>Changed` and `OnChanged`. Until the bridge has called `OnStartClient`, `IsAvailable == false`.
- Do change data on the server only through `ServerSet` / `ServerAdd` / `ServerInsert` / `ServerRemove` / `ServerClear`.
- Don't write the model from gameplay, UI or a service.
- Don't give the bridge client setters.
- Don't copy all fields into the model in one hook when only one arrived over the network: a hook writes only its own field, or the model is atomic and the hook gets the whole struct.

The ship's broken modules and open breaches are the shared `ShipCondition` model (see "Ship damage and repair").

The shop wallet is the `Wallet` model (`long Balance`). The start value comes from `WalletConfiguration`, a purchase lowers the balance, HUD and shop read `IReadOnlyWalletModel`.

## Working rules
- Work branch: `GameCore`. Feature branches are created from it and merged back by pull request.
- Prefer conservative fixes, no rewrites. Change feel values (speeds, springs) only deliberately and write
  "was → now".
- **Never use singletons.** Neither your own (`static Instance`, `Singleton`) nor built-in ones
  (Mirror's `NetworkManager.singleton`). Dependencies come through Zenject: `[Inject]`, constructor, a model from
  `DataInstaller`. For example, `ConnectionNetworkManager` comes from `ConnectionSessionModel.NetworkManager`.
  The only exception is third-party code (Mirror, Zenject, Steamworks.NET); we do not edit it.
- **Code is granular: a human should read it easily.** One class, one responsibility that can be named in one
  phrase without "and". Guidelines: class up to ~300 lines, method up to ~40 lines, one class per file. If a class
  grows past the guideline or gets a second concern (e.g. "flight" and "rocks", "riders" and "stats"), extract that
  concern into a separate service or component with its own interface instead of adding a `#region`.
  Don't build a new feature into an existing big class; put it next to it and connect it through DI or an interface.
  Names say what the class or method does; no abbreviations and no generic words like `Manager`, `Helper`, `Utils`.
- **Third-party asset packs (Asset Store `.unitypackage`) go into `Assets/Global/Packages/<Publisher>/`**, not into
  the `Assets/` root (older packs such as `SpaceSkies Free`, `Mini Mapa(Radar)`, `QuickOutline`, `StarSparrow` are still
  there and are not moved yet). Import only what the game uses: leave out demo scenes, sample skyboxes, sample URP
  assets and unused heavy textures. Do not edit pack files; the one allowed exception is fixing a hard-coded
  `Assets/<Pack>/...` path (e.g. a shader `#include`) after the move. Our own prefabs and materials built from a pack
  live in the feature module, not in the pack folder.
- New tuning numbers go into configs (ScriptableObject), not into `const`.
- **All configurations (ScriptableObject configs and catalogs that code receives through DI) are loaded only through
  Addressables and bound only in `ConfigurationInstaller`** (`Container.BindConfigurationFromAddressables<T>(...)`).
  The address is a constant from the generated `Address.Configurations.*` (`Address_g.cs`, generator in
  `Tools/Generations/Code generation`), not a hand-written string. Not allowed: `Resources.Load`, `Resources/`
  folders for configs, loading a config in a module installer via `FromMethod`, your own address-constant classes.
  New config: asset into the `Configurations` Addressables group → regenerate `Address_g.cs` → a line in
  `ConfigurationInstaller`.
- **Zenject bindings:**
  - Every model is bound in `DataInstaller`, including generated `<Name>ModelInstaller` calls.
  - A module binds its own `IGameplaySession` implementations in its own installer (as `ConnectionModuleInstaller`
    and `TooltipModuleInstaller` do), not in GameCore.
  - When a class is needed both as itself and through its interfaces, use one
    `Container.BindInterfacesAndSelfTo<T>().AsSingle();`, not `Bind<T>()` plus `Bind(typeof(I...)).To<T>().FromResolve()`.
  - No `.NonLazy()` on a class that implements `IInitializable` or `ITickable`: Zenject already creates it at
    startup. `NonLazy` is only for a class with none of these interfaces that still has to exist from the start.
- Verify every platform bug and feature in Play mode.
- **All repository text is in English:** `.md` files (AGENTS.md, CLAUDE.md, docs, READMEs), code comments, commit
  messages. This holds even when the task or chat is in another language.
- **All code comments are in English, with no exceptions:** `//` and `/* */` comments, XML doc comments, `TODO`
  notes, `[Tooltip]` / `[Header]` texts, and comments in shaders, asmdefs, scripts and generated code templates.
  When you touch a file with a non-English comment, translate that comment.

## Player data and rejoin (rule for all new features)
Players can **join a running session while the crew is at a station** (Build phase; joining is closed in flight, see
"Joining a running session" in Key places). A player who disconnected and joined again **gets their data back**: life
state, name and the shared crew state come from the models; the hand is empty (the item dropped on leaving); the
player appears at a deck spawn point.

Therefore:
- **All synced player state is stored in that player's model** (one model or a set of models per player), not only in
  fields of their player prefab. This covers: what they hold, personal resources, seat or role, progress, inventory,
  status.
- **The model key is a stable player ID:** SteamID or the authenticator ID. Not `netId`, not `connectionId` and not
  the player object: all of these change on reconnect.
- **Player models live on the server longer than the player object.** On disconnect the data is not deleted; the
  player is marked offline. The player object is only a bridge. When the object spawns, it takes its state from the
  model; when the player changes something, it goes through server commands into the model.
- **Restored on reconnect:** position (at the current station), resources, role. State that cannot be restored is
  decided explicitly. For example, an item held at disconnect drops at the station.
- **Shared crew state** (crew balance, ship modules, run phase) is stored in shared models. Any joining player gets
  it, and on late join the full current state arrives right away.
- Models are synced through the network model module (`NetworkModelModule`: model + bridge + generator, see
  "Synced models"; for player data `Scope = PerPlayer`). Check every new feature with the question:
  **"what will a player who joined or rejoined right now see?"**
