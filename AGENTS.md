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
- **No world shift in flight.** On loop 4 the ship is ~8-9 km from the origin before the landing recenter (float
  step there is ~1 mm); deck and camera jitter at that distance is not checked yet.
- A host and a real second client seeing the same field number, seed and placement count is checked by
  `CoopTwoProcessTest` (see "Two-player test"); the same field after a world shift in flight is covered only by the
  design (one placement list, the origin in the model, pads-like shift of Mirror objects) and by the host checks of
  `LoopSmokeTest`.

Known issue: on the first landing of a `DeckFeelHarness` run the player jumps about 136 m in one frame, dies and the
crew returns to the lobby. Seen on 2026-10-06, also on the base commit; not fixed and not investigated.

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
     `IFlightRouteListener` + `FlightLaunch`, `IWorldShiftFollower`, `IShipPositionSource`),
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
  `Features.FlightFieldModule.Editor.Tests` (generation on 200 seeds at loops 0, 3 and 10: passage with clearance,
  empty station zones and takeoff corridor, no overlaps, wall without holes, determinism; difficulty -> corridor width,
  two routes, the forced passage, 40x40 timing; structure catalog guards; networked structures registered with
  Mirror; structure layout).
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
| Ship, flight, riders | `Assets/Features/ShipModule/Scripts/`: `ShipBase` (ties its parts: `ShipDeckGeometry`, `ShipModules`, `ShipRiders`, `ShipSeats`, `ShipFlightControl`, `ShipDeckCargo`), `ShipFlight`, `ShipPoseSync`, `ShipRider`, `ShipRunDirector`, `ShipRunService` (phase machine) with `ShipStationPads`, `ShipStationDropService`, `ShipRoute`, `ShipFlightStatService`, `ShipWorldShiftService`, `ShipRunRiderService`, `ShipRunBindingModel` |
| Flight and landing settings | `ShipFlightSettings` (+ `ShipFlightConfig_Default.asset`), `ShipRunConfig` |
| Modules and slots | a module is one `ModuleDefinition` asset (`ShipModule/GameResources/Modules/`: id, slot type, item prefab, slot view, name, price, stat modifiers); `ModuleCatalog` lists them all and resolves a module id (`ModuleCatalogValidator` checks it at startup); `ShipItem`, `ShipSocket` (+ `ShipSocketRule`, state in the `ShipSockets` model), `Ship*Interactable`; catalogs in `ShipModule/GameResources/Configurations/` (Addressables) |
| Stats | `Assets/Features/StatsModule/`, `ShipStatType`, `ShipStatEntity`, `ShipStatSheet` (server: base values from `ShipAccumulativeStatsConfiguration` plus module modifiers), synced `ShipStats` model; player movement stats are `PlayerStatType` / `PlayerStatsConfiguration` in `CharacterMovableModule/Scripts/PlayerStats/` |
| World shift | `WorldShift` (count and total of the shifts), `ShipWorldShiftService`, `ShipPoseSync`; content outside the ship module follows a shift through `IWorldShiftFollower` (server: `ShipWorldShiftService.ApplyWorldShift`, remote clients: `ShipRunDirector.RpcShiftWorld`) |
| Flight field | `Assets/Features/FlightFieldModule/`: model (`FieldFrame` cell <-> world, a rectangle: `LengthCells` along A -> B = the distance in cells + the margins behind A and B, `WidthCells` across = `FieldWidthCells` (61), widened when the cruise start lies far off the axis so `SideMarginCells` stay beside it; `FieldCell`, `FieldPlacement`, `FieldRoute`, `FieldRequest`, `FlightField`); a structure is one `FieldStructureDefinition` (id, kind, footprint rows, prefab, `IsNetworked`, weight, visual yaw / scale jitter) in `FieldStructureCatalog`; tuning in `FlightFieldConfiguration` (both in the `Configurations` group; the takeoff corridor of `FieldTakeoffCorridorStep` runs from A along the takeoff heading past the cruise start by `TakeoffRunOutLength`, so the pilot has room to find B and turn); `IFlightFieldGenerator` runs the `IFieldGenerationStep`s in the order of their bindings in `FlightFieldModuleInstaller`; `IFieldRouteShaper` turns route difficulty into corridor parameters. In the world: `FlightFieldService` (server, `IFlightRouteListener` + `IWorldShiftFollower`) generates at launch on a worker thread (the generation touches no Unity object; a loop 9 field takes over 100 ms), keeps the field in `FlightFieldServerModel` and publishes it through `FlightFieldPublisher` into the `LiveField` model in batches (`FieldPublishJob`: `PublishEntriesPerBatch` = 300 entries, about 7 KB, one batch every `PublishBatchIntervalSeconds` = 0.1 s, so every Mirror delta stays below Telepathy's 16 KB message limit; FizzySteamworks allows 512 KB; the state with the new field number goes out after the last batch, a new field or `Clear` drops a publish in progress; a loop 9 field of ~53 KB takes about 1 s) (`FlightFieldSync` on the `ShipRunDirector` object); `LiveFieldBuildSystem` streams the local structures on every peer: it builds the placements within `BuildRadius` (1 300 m) of the game camera (`IFieldStreamingViewpoint`), at most `StructuresBuiltPerFrame` per frame, and returns the ones beyond `ReleaseRadius` (1 500 m) to the pool (`IFieldStructureSceneBuilder.ReleaseLocalStructure`), through `FieldStructureSceneBuilder` + `FieldStructurePool` under one `FlightField` root (`FlightFieldSceneModel`); `FieldStructureSpawnRule` decides local or Mirror (`FieldNetworkedStructures`, prefab marked with `FieldNetworkedStructure`; a client registers the networked prefabs of the catalog with Mirror on every client start, `FieldNetworkedPrefabRegistration` called by `FieldNetworkedPrefabSystem` in GameCore, so they are not in the network manager's spawn prefabs). Rocks: trigger colliders on the built-in `Ignore Raycast` layer, the collision matrix is unchanged (triggers push nothing; the aim ignores triggers and that layer is not in `InteractableMask`). Debug: **Tools → Flight Field → Debug Map** (editor; "Live field" shows the running host's field and the ship), `GameResources/Debug/Debug_NetworkBeacon` (networked test structure, not in the catalog) |
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
`ShipRunConfig` and adds the debug networked beacon to its own copy of `FieldStructureCatalog`; assets are not
changed. The test timeout is 180 s (a run takes about 110 s). The shared steps live in helpers both loop tests use:
`LoopSmokeSession` (menu, Host, lobby, Start, all dead -> lobby, Leave; waits record the run phases),
`LoopSmokeFlight` (engines, boarding, the lever, the phases of a loop with the field checks and the checks after
landing, messages start with "Loop N:"; at the cruise start its pilot faces the ship to the destination once through
`ShipBase.FlightControlDebug`, because the autopilot keeps the takeoff heading), `LoopSmokeShipRunConfigOverride` and `LoopSmokeFieldCatalogOverride` (the
config copies) and `LoopSmokeSpawnCounts`.

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
  a free port the test picks, not Steam), boards, rides the loops the host flies, leaves in flight, is turned away in
  flight, rejoins at the next station on the deck, dies (the dev kill command, like F9), waits to be bought back, rides
  one more loop and quits.
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
