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
  crew finds it **by radar**: the arrow and the beacon at the minimap edge (`ShipRadarService`, `Aux_MP` with
  `Arrow = 1`) and the arrow on the radar module's screen (`ShipRadarScreen`);
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
  The rocks do nothing to the ship yet (see "Known limits");
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
  `ShipStationDropService.SpawnDrops`), so the shop is not needed yet;
- there is no income, only the starting balance (`WalletConfiguration`, 1 000 000 now);
- the final ship stats are synced to clients (`ShipStats` model), but only `FlightSpeed` (cruise speed) and
  `DodgeRange` (side sway) affect the game, and no module changes `DodgeRange`; `Thrust`, `Handling` and `Armor` are
  not read by gameplay;
- a crew that does not steer to the destination can fly forever: the autopilot only keeps the heading and there is no
  timeout;
- the run has no goal: it ends only when the whole crew is dead (`PlayerLifeSystem` → lobby);
- players can join or come back in the lobby or while the ship stands at a station (`Build`); in flight, in a wreck,
  while the map changes and with a full crew they are turned away with a reason the menu shows (see "Player data and
  rejoin");
- there is no sound or VFX.

### Known limits
- **The ship flies through the flight field's rocks.** Rocks have no damage, no bounce and no effect on the ship or
  the players: their colliders are triggers on the built-in `Ignore Raycast` layer (see "Flight field" in Key places).
  Damage and armor are separate tasks.
- The field has no "left the field" event and the rocks are not on the radar yet.
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
     `Features.AddressablesConstantGeneratorModule` (`Address_g.cs`), `Features.MvpModule` (AssetLoader),
     `Features.FloatingControllerModule`, `Features.MatrixModule` (no references: generic grid `IMatrix<T>` with
     shape insert and A* path finding, copied from MuffinProject's `MatrixSubmodule`; keep its public API),
     `Features.PlayerProfileModule.Data` (NetworkModel; the generated `PlayerProfile` model and
     `IPlayerProfileSource`), `Features.CrewInviteModule.Data` (NetworkModel; the generated `CrewRoster` model);
     third-party `QuickOutline` and `MiniMapModular`
     (`Mini Mapa(Radar)`) also have their own asmdef.
  2. `Features.CharacterMovableModule` (Contracts, FloatingController, Address, Stats, Input),
     `Features.GameFlowStateMachineModule` (Contracts, SceneLoader),
     `Features.FlightFieldModule` (Matrix, Zenject, Contracts, NetworkModel, Mirror: the flight field model and its
     generation in pure C#, the `LiveField` model, the server service and the local building; it does not reference
     the ship module, the run talks to it through the Contracts; its editor tools are
     `Features.FlightFieldModule.Editor`).
  3. `Features.CameraModule` (CharacterMovable, Contracts, Input) → `Features.GrabModule` (Camera, CharacterMovable,
     Contracts, NetworkModel).
  4. `Features.ShipModule` (Camera, Grab, CharacterMovable, FloatingController, GameFlow, Stats, Connection, MiniMap,
     NetworkModel),
     `Features.MenuModule` (GameFlow, Mvp, Connection),
     `Features.PlayerProfileModule` (PlayerProfileModule.Data, Camera, NetworkModel, TextMeshPro).
  5. `Features.PlayerLifeModule` (Ship, Grab, Camera, Mvp, NetworkModel, Connection, PlayerProfileModule.Data),
     `Features.LobbyModule` (Ship,
     GameFlow, Mvp, Connection).
  6. `Features.ShopModule` (PlayerLife, Ship, Camera, GameFlow, Mvp, NetworkModel, PlayerProfileModule.Data) →
     `Features.TooltipModule` (Shop,
     Ship, Grab, Mvp) → `Features.CrewInviteModule` (CrewInviteModule.Data, Tooltip, Grab, Connection, NetworkModel: the "Hire
     crew" terminal; above Tooltip because it plugs a state tooltip into it through `ITooltipStateProvider`).
  7. Composition root: `Features.GameCoreModule` (all modules) and `Features.BootstrapersModule` (Contracts,
     GameFlow, SceneLoader).
  Tests: `Features.CharacterMovableModule.Editor.Tests`, `Features.PlayerLifeModule.Editor.Tests`,
  `Features.ShipModule.Editor.Tests`, `Features.ShopModule.Editor.Tests`, `Features.GameCoreModule.Editor.Tests`
  (configuration loading and module catalog guards), `Game.Connection.Editor.Tests`,
  `Features.PlayerProfileModule.Editor.Tests`, `Features.CrewInviteModule.Editor.Tests`, `NetworkModelModule.Editor.Tests`, `Features.MatrixModule.Editor.Tests`
  (matrix creation, neighbours, full-shape insert incl. L-shaped shapes, path finding incl. a 40x40 timing check),
  `Features.FlightFieldModule.Editor.Tests` (generation on 200 seeds at loops 0, 5 and 10 for every difficulty
  profile: main-route passage with clearance, empty station zones, takeoff and main corridors, no overlaps, wall band
  without holes, no forced passage, "the route matters" metric, zone shares, corridor debris gap, determinism, and a
  balance table per profile in the log (`[FieldBalance]`); profile and difficulty -> corridor shape, winding main
  route, alternatives and dead ends off the main route, two main routes, profile picked from the loop entry by seed,
  40x40 timing; the closed flight plane (rock mesh bounds sampled in and between occupied cells, the main centerline
  clear); structure catalog guards; networked structures registered with Mirror; structure layout; cluster layout: the same seed gives the same rocks, the giant crosses the flight plane, outcrops and satellites stay over the footprint, giants stick out by about a cell at most, every local structure has a complete recipe of collider-free rocks; volume: a chunk gives the same rocks by seed whatever was built before, rocks stay in their band and clear of the plane, never fuse across chunks, none below the plane near the route's ends, every profile has rocks; decor: the planet choice depends on the seed only).
- Player identity and name come from Connection only through NetworkModel contracts: `IPlayerIdentityService`
  (`PlayerKey`) and `IPlayerNameSource` (the authenticated name), implemented in Connection. Other modules read a
  player's name through `IPlayerProfileSource` / `IReadOnlyPlayerProfileModel` (PlayerProfileModule.Data), not
  through the `PlayerProfile` bridge.
- The camera does not know about Grab: `IGameCameraService.LookApplied` fires in `PresentNow()` after
  `ApplyLookRig()` and before the brain update; `HeldItemFollowSystem` (Grab) listens to it. The frame order is the
  same as before.
- `LoopSmokeTest` stays in `Assembly-CSharp`: every assembly is visible from there. Whatever the test touches in a
  module must be `public` (not `InternalsVisibleTo`).

### Key places
| What | Where |
|---|---|
| Ship, flight, riders | `Assets/Features/ShipModule/Scripts/`: `ShipBase` (ties its parts: `ShipDeckGeometry`, `ShipModules`, `ShipRiders`, `ShipSeats`, `ShipFlightControl`, `ShipDeckCargo`), `ShipFlight`, `ShipPoseSync`, `ShipRider` (+ `ShipDeckRideRule`, `ShipRideDiagnostics`, see "Riding the ship"), `ShipRunDirector`, `ShipRunService` (phase machine) with `ShipStationPads`, `ShipStationDropService`, `ShipRoute`, `ShipFlightStatService`, `ShipWorldShiftService`, `ShipRunRiderService`, `ShipRunBindingModel` |
| Flight and landing settings | `ShipFlightSettings` (+ `ShipFlightConfig_Default.asset`), `ShipRunConfig` |
| Modules and slots | a module is one `ModuleDefinition` asset (`ShipModule/GameResources/Modules/`: id, slot type, item prefab, slot view, name, price, stat modifiers); `ModuleCatalog` lists them all and resolves a module id (`ModuleCatalogValidator` checks it at startup); `ShipItem`, `ShipSocket` (+ `ShipSocketRule`, state in the `ShipSockets` model), `Ship*Interactable`; catalogs in `ShipModule/GameResources/Configurations/` (Addressables) |
| Stats | `Assets/Features/StatsModule/`, `ShipStatType`, `ShipStatEntity`, `ShipStatSheet` (server: base values from `ShipAccumulativeStatsConfiguration` plus module modifiers), synced `ShipStats` model; player movement stats are `PlayerStatType` / `PlayerStatsConfiguration` in `CharacterMovableModule/Scripts/PlayerStats/` |
| World shift | `WorldShift` (count and total of the shifts), `ShipWorldShiftService`, `ShipPoseSync`; content outside the ship module follows a shift through `IWorldShiftFollower` (server: `ShipWorldShiftService.ApplyWorldShift`, remote clients: `ShipRunDirector.RpcShiftWorld`) |
| Flight field | `Assets/Features/FlightFieldModule/`: model (`FieldFrame` cell <-> world, a rectangle: `LengthCells` along A -> B = the distance in cells + the margins behind A and B, `WidthCells` across = `FieldWidthCells` (61), widened when the cruise start lies far off the axis so `SideMarginCells` stay beside it; `FieldCell`, `FieldPlacement`, `FieldRoute`, `FieldRequest`, `FlightField`); a structure is one `FieldStructureDefinition` (id, kind, footprint rows, prefab, `IsNetworked`, visual yaw / scale jitter, cluster recipe) in `FieldStructureCatalog`, which also lists the kinds of small debris (`FieldDebrisDefinition`: id + `FieldDebrisRecipe`); tuning in `FlightFieldConfiguration` (both in the `Configurations` group; the ship gaps: ship width, debris gap margin, `MaxPlaneGapMeters`, plane half thickness; the takeoff corridor of `FieldTakeoffCorridorStep` runs from A along the takeoff heading past the cruise start by `TakeoffRunOutLength`, so the pilot has room to find B and turn); `IFlightFieldGenerator` runs the `IFieldGenerationStep`s in the order of their bindings in `FlightFieldModuleInstaller`. **Structure of a field:** every number comes from its `FieldDifficultyProfile` (`Configurations/Profiles/FieldProfile_Open/Belt/Canyon/Maze` and the four Channels stages `FieldProfile_ChannelsWarmup/Fork/Lanes/Braid`, all listed in `FlightFieldConfiguration.DifficultyProfiles`; every value is a `FieldDifficultyRange` read at the field's difficulty 0..1, the hardest of its route difficulties); every `_routesByLoop` entry has route difficulties and weighted profiles, the Channels stage of those loops at weight 3 beside the classic ones at 1 (loop 0 Warmup/Open; 1-2 Fork/Open/Belt; 3-5 Lanes/Belt/Canyon; 6+ Braid/Canyon/Maze). **Channels** (the core of the flight is a slalom): blocked share 1, so every cell outside the corridors is wall; the profile's `ChannelCount` main corridors per route part over `ChannelSplitShare` of the route after A and meet over the same share before B, `ChannelSpreadShare` of the field width apart, each with its own random sine phases (`MeanderWaves` full swings over the route, amplitude `MeanderAmplitudeCells`), so on Braid (spread 0.15, amplitude 9 cells) they cross and merge mid-route; stages are data only: Warmup 1 straight channel 7 cells, density 0.2, `ChannelRocks_Light` (big 15 %); Fork 2 channels 11 cells around a central wall, swing 2, density 0.5, `ChannelRocks_Medium` (big 40 %); Lanes 3 channels, swing 6, density 0.6; Braid 3 channels, swing 9, density 0.7, `ChannelRocks_Heavy` (big 45 %), `FieldProfileSelector` picks one from the field seed (editor / development: `IFieldProfileDebug.DebugUseProfile(id)` forces one from the next launch). The steps: frame, station zones, takeoff corridor, `FieldRouteStep` (winding main corridors, one per channel: straight A -> B plus the channel's sideways offset plus two sines 75/25 fading at the stations, `FieldMeanderPlanner`; width changes along it by noise, never below the passage clearance; `IFieldRouteShaper` = `ProfileFieldRouteShaper`), `FieldBranchStep` (`AlternativeRoutes` that leave and rejoin the main route, narrower, more debris, on high difficulty sometimes closed by a ridge in the middle; `DeadEnds` that end in a ridge cap), `FieldDensityMapStep` (low-frequency value noise with contrast and stretch, a shoulder of higher density beside every corridor, the glade layer and a few large voids, `FieldDensityMap`), `FieldWallStep` (a band of `WallThicknessCells`), `FieldZoneStep` (the generator decides only the cell class, `FieldCellClass`: open cells are closed densest first until the profile's blocked share, glades and the densest rest get debris until the debris share, voids stay empty; corridors carry debris at their route's density), `FieldGiantPlacementStep` (`FieldGiantPacker` merges neighbouring closed cells into the biggest footprints first, 8x8 down to 1x1, from the profile's wall / ridge obstacle options: the obstacle type is data, a new type is a new asset), `FieldPassageStep` (main routes only), `FieldPathMetricStep` ("the route matters": L_route and L_free = the shortest way with clearance outside every corridor; while L_free / L_route is below the profile's minimum, or a free way exists where the profile forbids it, ridges are put on the free way, a limited number of rounds; also straight A -> B blocked) and `FieldBalanceMetricStep` (zone shares, smallest gap between corridor debris, dodges per minute on the main centerline at cruise speed); the numbers are in `FlightField.Metrics`. Debris is not a structure: a passable cell keeps a debris kind and a density, the network carries runs of debris cells (`FieldDebrisRunEntry`), and every peer lays the rocks out itself from the field seed (`FieldDebrisLayout`, the cells read through `IFieldDebrisCells`: the generator's matrix `MatrixFieldDebrisCells`, a peer's live field `LiveFieldDebrisCells`): a jittered grid of `DebrisGridSpacingMeters` (25 m) over the whole field plane, a point holds a rock with the cell's density, small (`SmallDiameterMeters`, 10-35 m) or big (`BigDiameterMeters`, 50-140 m) by the recipe's `BigShare`, most crossing the flight plane (`CrossShare`), some above / below (`OffPlaneShare`); no rock reaches into an empty cell and none comes closer than `DebrisWallClearanceMeters` (5 m) to a closed one, so no free lane runs along the walls; a point closer than the ship gap (ship width + margin, 16 m) to a kept neighbour of higher priority gives way (sequential Matern thinning, decided with an explicit stack in `FieldDebrisWorkspace`, exact priority order), so any two rocks leave the ship room, whatever order the cells are built in. The slalom is checked in the editor by `FieldSlalomAnalyzer` (all rocks laid out as on a peer, walls = closed cells grown by the largest wall-rock spill; `FieldSlalomPathFinder`: A* over a 2.5 m grid, `FieldSlalomGrid`, then pulled straight): a way from the cruise start to B with the ship clearance less half a grid diagonal, its length against the straight line, the narrowest gap, rocks per km of main channel; `FieldSlalomTests` run it on 200 seeds at loops 0, 1, 5, 10 for the Channels stage of each loop and log a `[FieldSlalom]` table. In the world: `FlightFieldLaunchSystem` (server, `IFlightRouteListener`) has `FlightFieldService` (`IWorldShiftFollower`) generate at launch on a worker thread (the generation touches no Unity object; a loop 9 field takes over 100 ms) and publish the field on the first tick after it is done; the service keeps the field in `FlightFieldServerModel` and publishes it through `FlightFieldPublisher` into the `LiveField` model in batches (`FieldPublishJob`: `PublishEntriesPerBatch` = 300 entries, about 7 KB, one batch every `PublishBatchIntervalSeconds` = 0.1 s, so every Mirror delta stays below Telepathy's 16 KB message limit; FizzySteamworks allows 512 KB; the state with the new field number goes out after the last batch, a new field or `Clear` drops a publish in progress; a loop 9 field of ~53 KB takes about 1 s) (`FlightFieldSync` on the `ShipRunDirector` object); `LiveFieldBuildSystem` streams the local structures on every peer: it builds the placements within `BuildRadius` (1 300 m) of the game camera (`ILocalViewpointSource` from the Contracts, bound by the camera module; the decision is `FieldStreamingRule`, the placement states live in `FlightFieldSceneModel`), at most `StructuresBuiltPerFrame` per frame, and returns the ones beyond `ReleaseRadius` (1 500 m) to the pool (`IFieldStructureSceneBuilder.ReleaseLocalStructure`); the small debris streams the same way (`FieldDebrisStreamer` unrolls the `DebrisRuns` of the live field into cells, `FieldDebrisSceneBuilder` builds them within `DebrisBuildRadius` (900 m), `DebrisCellsBuiltPerFrame` per frame, from its own pool under its own root `FlightFieldDebris` posed like the field root); the live field state also carries the field's `ProfileId`, through `FieldStructureSceneBuilder` + `FieldStructurePool` under one `FlightField` root (`FlightFieldSceneModel`); `FieldStructureSpawnRule` decides local or Mirror (`FieldNetworkedStructures`, prefab marked with `FieldNetworkedStructure`; a client registers the networked prefabs of the catalog with Mirror on every client start, `FieldNetworkedPrefabRegistration` called by `FieldNetworkedPrefabSystem` in GameCore, so they are not in the network manager's spawn prefabs). Rock look: the structure prefab (`GameResources/Prefabs/Rock_*`) is only the trigger; what a player sees comes from its `FieldClusterRecipe` (`GameResources/Rocks/Recipes/`: weighted rock prefabs, shared materials as the tint spread, main rock size and height jitter, satellite count / size, height spread up to 3 cells (2-2.5 in the game recipes, so the field is not one flat line), debris chance / count / size). `FieldClusterLayout` turns the recipe into rock poses from the field seed + placement index (main rock in the middle of the footprint, always across the flight plane; satellites and debris over random footprint cells; every rock stays inside the footprint, because a client does not know the corridor cells; full random 3-axis turn, per-axis scale 0.8-1.2), so every peer sees the same clusters; `FieldStructureSceneBuilder` takes every rock from the pool (one pool per prefab; `TakeRock` re-parents a free instance and keeps its `FieldRockVisual`) and parents it to the trigger, so the root keeps one object per structure, and remembers the objects per placement (`ReleaseLocalStructure`, `ReleaseAll`). Rock prefabs (`GameResources/Rocks/Prefabs/FieldRock_01..11`, meshes of `Global/Packages/PolyOne/Rocks Stylized`, normalised to about 1 m, `FieldRockVisual` at the root, no colliders) and 3 shared URP Lit materials (`GameResources/Rocks/Materials`: the pack's albedo tinted dark blue-grey / rusty brown with a faint warm emission (the nebula's reflection), matte, SRP Batcher compatible: the tint is a material swap, not a `MaterialPropertyBlock`). The edge wall uses its own structures `rock_wall_1x1` / `2x2` / `3x3` (denser recipes); ridges use `rock_1x1` ... `rock_8x8`. **Walls = a dense random rock field, the closed flight plane:** every cluster recipe has plane rocks (the chunky prefabs `FieldRock_06` / `07`, `FieldPlaneCoverLayout`): the footprint (ridges use squares 1x1-8x8 and long 1x2-3x4 footprints) is split like a quadtree into parts, each closed by one rock of random prefab, material, full random 3-axis turn and size (`PlaneSizeVariation`), big enough that its smallest one-sided extent over any turn (`PlaneHalfExtentShare`) covers the part with at most `PlaneGapMeters` to the next one (at most `MaxPlaneGapMeters` = 6 m, below the 10 m ship) and at least `PlaneMinHalfHeightMeters` above and below the plane; a part splits again while its rock would spill more than `PlaneMaxSpillMeters` (35 m) past the footprint; plus `ExtraRocksPerCell` rocks above and below the plane. No grid, rows or giants to see: walls and channels are one asteroid field, packed in the walls and sparse in the channels. No cluster has small debris any more. A new look = a new recipe asset. Rocks: trigger colliders on the built-in `Ignore Raycast` layer, the collision matrix is unchanged (triggers push nothing; the aim ignores triggers and that layer is not in `InteractableMask`). Decor (visual only, every peer builds it locally, no colliders, not in the matrix, not synced; chunks and planet in `FlightFieldDecorConfiguration` in the `Configurations` group, checked by `FlightFieldDecorConfigurationValidator`): `FieldDecorBuildSystem` keeps the volume of big rocks above and below the flight plane in chunks around the local ship (`IShipPositionSource`; on a client it reads `ShipClientBindingModel`, bound by `ShipRunDirector` on client start) within the view distance, a few chunks per frame, and releases the chunks left behind. The volume (`FieldVolumeConfiguration`, referenced by `FlightFieldConfiguration.Volume`, checked by `FieldVolumeConfigurationValidator`) has bands: near 40-300 m (rocks 50-150 m) and far 300-600 m (120-220 m), mirrored below the plane; `FieldDecorChunkLayout` draws every band's grid points of the chunk (`FieldVolumeCandidates`: a rock with the band's presence at the ridge noise below it, `FieldRidgeNoise`, the same noise the density map is built on, so ridges are denser above and below and glades sparser but never empty; read with the profile and difficulty of the live field, `LiveFieldVolumeSourceReader`), keeps every rock inside its band and at least `PlaneClearanceMeters` (30 m) from the plane, none below the plane within `StationClearanceMeters` of the main route's ends, and thins points that come closer than the band's gap to a neighbour with a higher priority (Matern thinning), so two rocks never fuse and every peer and a rejoining player see the same rocks whatever order the chunks are built in. `FieldDecorPlanetPicker` picks the flight's planet from the field seed (chance, 1 = every flight shows one, max per flight, side of the sky, which planet by the weights of `Planets`, its diameter from that planet's own range, distance from the route) around the route axis (the middle of the field's width): the side comes from the seed and the weights of `PlanetSides` only (default `Side` 0.5 at 5-20 degrees above or below the plane, `Above` 0.4 at 25-60, `Below` 0.1 at 25-50 below), not from the light; within the side the planet turns towards the main light's travel direction (`IFieldDecorLightSource`, `RenderSettings.sun`; seeded tries within `PlanetMaxAngleFromLight` around the vertical, the closest one wins), so the light comes from behind the viewer and the lit side faces the ship (no main light: towards one side of the route); the game scene's sun shines 50 degrees down, so a planet above shows a half to two thirds of its lit side; every planet stays at least `PlanetMinAngleFromCourse` (35 degrees) off the course A -> B as seen from the start of the field (the azimuth is turned out, the along-route offset pulled back), so it never covers station B; planet prefabs `GameResources/Planets/DecorPlanet_*` are the pack's icosphere: six `DecorPlanet_Storm_<Colour>` presets (the pack's "Gas Giant with Storms" shader, project material copies with their own 256x1 gradient in `Planets/Gradients`, slow flow) plus four `DecorPlanet_Earth_*` (Ocean, Alien, Ice, Desert; "Earth-like Lite" shader) and two `DecorPlanet_Moon_*` (BlueGrey, Lilac; "Moon Lite", no fresnel) presets; the baked Earth C / Moon A / yellow gas giants stay on disk but are not in the list; all copied into `GameResources/Planets/Materials` with a wider, brighter fresnel so the night side is not pure black (no Glow / Corona / ring materials). Sizes per planet in `Planets` (gas giants 1600-2600 m, Earth-like 700-1100 m, moons 300-500 m); `PlanetDistance` (2-3.6 km) is to the near surface, so a bigger planet stands farther. A planet never enters the field: its surface stays `PlanetFieldClearance` (300 m) beside this flight's field width (`WidthCells`, so a widened field too) or above / below the top of the volume rock bands (`FieldVolumeConfiguration.TopMeters`, 600 m), and `PlanetStationClearance` (1000 m) off the route axis A -> B (both pads stand on it). Seen from the start of the field its far edge stays within `ViewFarClipPlane` (5000 m, the player camera's) minus `PlanetFarClipMargin` (300 m). Each seeded direction try is kept only if the planet's smallest size fits between these clearances and the reach; the distance is pushed out to the clearances, the along-route offset pulled back, and the diameter shrunk (never below the planet's minimum) to fit; with no fitting try the flight shows no planet (under 1 % of flights on the default width, about 10 % on a field widened to 2.4 km half width, all of them side planets). Every weighted roll of the module (giant obstacle options and their turns, cluster and volume rocks, debris kinds, difficulty profiles of a loop, planet sides, planets) goes through `FieldWeightedPick` over `IFieldWeighted`. `FieldDecorSceneBuilder` builds under its own `FlightFieldDecor` root posed like the field root (so it follows a world shift) with its own `FieldStructurePool` (the field's pool is emptied on every field change); state in `FieldDecorSceneModel`. Debug: **Tools → Flight Field → Debug Map** (editor; profile override, zone or density layer, every route by kind, the L_route and L_free ways, the metric and balance numbers, "Rocks and slalom way": every debris rock (in / off the plane) and the slalom way with its numbers; "Live field" shows the running host's field and the ship), `GameResources/Debug/Debug_NetworkBeacon` (networked test structure, not in the catalog) |
| Cargo on the deck | `ShipDeckCargo`: items lying on the deck are attached to the ship and travel with it |
| Radar | `ShipRadarService`, `ShipRadarScreen`, `ShipRadarCatalog` |
| Deck debugging | `ShipModule/Scripts/Debug/` (editor / development builds only): `DeckFeelHarness` (deck feel measurements), `ShipStatsDebugOverlay` |
| Death, damage, spectator | `Assets/Features/PlayerLifeModule/`: `PlayerLifeSystem` (life state machines, all dead → lobby), `PlayerDamageable`, `FallKillSystem`, `SpectatorSystem`, `PlayerLifeConfiguration`, `PlayerDamageConfiguration`; **F9** kills the local player in editor / development builds |
| Tooltips | `Assets/Features/TooltipModule/`: `InteractionTooltip` on the object, `TooltipContentService`, `Tooltip_*.asset` in `GameResources/Configurations/` |
| Synced models | `Assets/Features/NetworkModelModule/` (model + bridge + generator), see "Synced models" |
| Interaction | `Assets/Features/GrabModule/`: `LocalPlayerInteraction` (aim, highlight, button), `GrabController` (hand, `PlayerHand` model), `UseController`, `HeldItemRegistry`, `InteractableBase`, `Grabbable`, `GrabConfiguration` |
| Shop and money | `Assets/Features/ShopModule/` (`CrewWallet`, `ShopCatalog`, `WalletConfiguration`); the kiosk `ShopKioskInteractable` is part of the `StationPad` prefab, `ShopAccessService` / `ShopAccessRule` decide open, keep-open and purchase (Build, alive, distance from `ShopKioskConfiguration`), the server keeps the opened kiosk per player in `ShopVisitModel`; **L** opens the shop only in editor / development builds |
| Buying back a dead crewmate | the shop window's "Crew" section (`CrewReviveView` / `CrewRevivePresenter`, list `DeadCrewModel`, kept by `DeadCrewSystem` from the synced `PlayerLifeRegistry`) → `CrewWallet.CmdRevive` → `ReviveShopSystem` (kiosk check like a purchase, `ReviveRule`, then `ServerTrySpend`, then `IPlayerLifeReviver.ServerRevive(PlayerKey)`); price in `ReviveConfiguration`; the revived body is teleported to the ship's next `ShipDeckSpawnPoint` in turn (`IShipDeckSpawnPoints`); `ReviveConfigurationValidator` fails startup on a price <= 0 |
| Joining a running session | `Connection/Scripts/Game.Connection/Join/`: `JoinRule` (pure: lobby or `RunJoinWindow.AtStation`, free slot out of `ConnectionConfig.MaxConnections`, version, Steam id; reasons `InFlight` / `ServerFull` / `MapChanging`), `ConnectionJoinPolicy` (the rule on the live session, used by `ConnectionAuthenticator` once at authentication; `OnJoinStateChanged` is the one event for "an input of the rule changed": run window, map change, players), `SteamLobbyJoinableSystem` (Steam lobby joinable exactly when the rule lets a new player in), `ConnectionRejectModel` + `ConnectionRejectMessages` (the reason shown in the menu; `RejectedJoinMenuSystem` in GameCore switches a turned-away client to the menu state). Connection reads the run only through the contracts `ISessionJoinGate` (`ShipRunJoinGate`) and `ISessionJoinSpawn` (`ShipDeckJoinSpawn`, the next `IShipDeckSpawnPoints` pose) in `GameCoreModule.Contracts`. A mid-run joiner loads the map through Mirror's scene message and reports `MapLoadedMessage`; `ConnectionLateJoin` makes its player only then, on the deck. The transport allows one connection over the slots so an extra player hears "the crew is full" |
| Inviting friends ("Hire crew") | `Assets/Features/CrewInviteModule/` (`Scripts/Core`: rule and status, `Scripts/Network`: terminal, inviter, roster bridge, `Scripts/UI`: tooltip, `Data`: the generated model): the terminal `CrewInviteInteractable` stands in the lobby and on every `StationPad` next to the kiosk; E sends `CrewInviter.TargetOpenInviteOverlay` to the pressing player, who opens `ISteamLobbyService.OpenInviteOverlay` (Steam invite dialog for the current lobby). `CrewInviteRule` (pure) gives `Available` / `CrewFull` / `NotAtStation` / `NoSteam` / `OverlayDisabled` from the Steam side and the shared `CrewRoster` model (players, slots, join window from `IConnectionJoinPolicy`, written by the `CrewRoster` bridge on the server on `IConnectionJoinPolicy.OnJoinStateChanged`); `CrewInviteTooltipProvider` picks the `Tooltip_CrewInvite*` asset, fills "N/M" and refreshes on the roster and on `ISteamLobbyService.InviteStateChanged` |
| Player names and nameplates | `Assets/Features/PlayerProfileModule/`: `PlayerProfile` (bridge of the `PlayerProfile` model, the server fills the name), `PlayerNameSanitizer`, `PlayerNameplate` (TMP over other players' heads), `PlayerNameplateConfiguration`; the model is in `Data/` |
| Scenes | `GameCoreModule/GameResources/Scenes/`: Bootstrap → Menu → Lobby → Game; only Bootstrap is in Build Settings |

### How to verify
Play mode starts from `BootstrapScene`, then menu → Host → game. Two-player multiplayer is checked with the
ParrelSync clone (`MirrorCoopBase_clone_0`).

### Loop autotest
The PlayMode test `LoopSmokeTest` (`Assets/Tests/PlayMode/LoopSmoke/`) plays the M1 loop on a host by itself:
menu → Host → lobby → Start → `GameScene`, installs 2 engines, takes the radar off, drops, picks up and reinstalls it by hand through the real third-person
aim (`LoopSmokeHandCheck`), takes off and lands twice, and checks the phases,
`LoopIndex`, engines after landing, the ship on the new pad, one shop kiosk per pad, the player on deck, no wreckage.
At the first station it reaches it buys back a dead crewmate with a second player, a bot on the host (`LoopSmokeReviveCheck`:
the price is taken once for a double click, no buy-back without money, the revived player stands at a deck spawn
point; then the host dies and the bot buys them back). The bot joins through the server's real receive path
(`LoopSmokeBot`, `LoopSmokeTransportInput`). `LoopSmokeRejoinCheck`: a new player joins at that station on a deck spawn
point, dies, leaves in flight, a join in flight is turned away with `InFlight`, and after landing it comes back dead, in
the Crew list, under its new name. After the second landing everyone dies, the crew returns to
the lobby and the test checks that no object of the first run is left. Then it starts a second run, flies five loops (after the first one it checks the station's crew terminal and puts the
player back on the deck)
in a row and at the last station buys 5 items at the kiosk through the real aim and the window (`LoopSmokeShopCheck`: delivery
point, window closes when walking away, the bought items come to rest within a timeout, a far purchase is refused), returns to the menu and checks that there are no
errors in the log. Before that check `LoopSmokeRejectCheck` joins a silent loopback "host", hands the client an
`InFlight` answer and checks the reason text in the menu. `LoopSmokeCrewInviteCheck`: one "Hire crew" terminal in the
lobby and one per pad, inactive with "Steam is not running." under the real aim (no Steam in the test; E on it asks
`LoopSmokeSteamLobbySpy`, which stands in for `ISteamLobbyService`, for no overlay), the
`CrewRoster` counts filler bots up to the slot limit (crew full) and back, and closes the join window in flight. Every
flight runs `LoopSmokeFieldCheck`: in cruise a new
flight field stands with an active local object for every local placement within the build radius of the camera (none beyond the release radius) and one Mirror object per networked placement;
after landing nothing of it is left (what a player joining at that station gets); in run 2 loop 3 a forced world shift
in cruise keeps the field in place against the ship; the field pool never grows past what the fields needed. Every
takeoff logs its longest frame (`[LoopSmoke] loop N takeoff`). The test shortens flight timings with its own copy of
`ShipRunConfig` and adds the debug networked beacon to its own copy of `FieldStructureCatalog` and, as a rare ridge obstacle, to its own copies of the field configuration's difficulty profiles; assets are not
changed. The test timeout is 180 s (a run takes about 110 s). The shared steps live in helpers both loop tests use:
`LoopSmokeSession` (menu, Host, lobby, Start, all dead -> lobby, Leave; waits record the run phases),
`LoopSmokeFlight` (engines, boarding, the lever, the phases of a loop with the field checks and the checks after
landing, messages start with "Loop N:"; at the cruise start its pilot faces the ship to the destination once through
`ShipBase.FlightControlDebug`, because the autopilot keeps the takeoff heading), `LoopSmokeShipRunConfigOverride` and `LoopSmokeFieldCatalogOverride` (the
config copies) and `LoopSmokeSpawnCounts`.

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

EditMode guard tests keep the data rules: `ConfigurationLoadingGuardTests` (no `Resources.Load`, every configuration
address loads), `ModuleCatalogGuardTests` (module ids set and unique, shop and drops only use catalog modules, no code
checks a concrete module), `ShipSocketIdTests` (every ship socket has a unique id), `FieldStructureCatalogGuardTests`
and `FieldNetworkedStructureGuardTests` (field structure ids, prefabs and footprints; networked structures are Mirror prefabs registered
with Mirror; no debug structure in the game catalog or the spawn prefabs).

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
  a free port the test picks, not Steam), boards at the stern (`ShipDeckBoarding.PutAboveStern`), rides the loops the host flies, leaves in flight, is turned away in
  flight, rejoins at the next station on the deck, dies (the dev kill command, like F9), waits to be bought back, rides
  one more loop and quits.
- **No Steam identity.** Both processes run on one Steam account, so `CoopSessionActions.PrepareDirect` sets
  `ConnectionAuthenticator.UsesSteamIdentity = false` (editor / development builds only): they authenticate as
  `CoopHost` / `CoopClient` with no Steam id (a `client:` player key), as without Steam, instead of the persona name.
  The test passes with the Steam client running; Steam itself still starts. Normal runs keep the Steam name.
- **Exchange.** The client appends a JSON line per step to `<coopDir>/client-reports.jsonl` (phase, `LoopIndex`,
  installed sockets, ship stats, crew balance, flight field number / seed / placement count, the nameplate it sees
  over the host, its own position on the deck, life state, spectating, reject reason, its logged errors); the host
  answers with commands in `<coopDir>/host-commands.txt` (`rejoin`, `finish`). The host captures the same fields
  (`CoopObserver`) and compares them at every step (`CoopObservationComparison`), and checks the client's server
  position on the deck, the `InFlight` refusal text in the client's menu, the buy-back at the kiosk through the shop
  window and that neither side logged an error.
- The test always kills the client process (also on failure or timeout). A passed run deletes its folder
  `Builds/CoopRuns/<time>/`; a failed one keeps the client's log and the exchange files there, and the failure message
  ends with the tail of the client's log.
- Shared code, one copy each: `Tests.Shared` (`SessionQueries`: menu / game-at-station / back-in-menu checks, the
  network manager, `Resolve<T>` from the project or a scene context, `ContainsXZ`; `ShipDeckBoarding`) is used by the
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
