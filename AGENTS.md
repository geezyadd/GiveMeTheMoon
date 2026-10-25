# GiveMeTheMoon — project context

Read before any task in this repository: humans, Claude and worker agents.

**Where things are described.** This file holds what every task needs: the game, milestones, tech, rules and the module
map. Each module's details (key places, status, known limits, tests) live in its own `AGENTS.md` in the module folder
(`MirrorCoopBase/Assets/Features/<Module>/AGENTS.md`), the loop tests in
`MirrorCoopBase/Assets/Tests/PlayMode/LoopSmoke/AGENTS.md` and `MirrorCoopBase/Assets/Tests/Coop/AGENTS.md`. **Rule: a
module's description lives in its own AGENTS.md.** Change it there, not here; add a line to the module map below only
for a new module. Before working on a module, read its file.

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
is not final yet and needs evaluation. How the flight works in code now:
[ShipModule](MirrorCoopBase/Assets/Features/ShipModule/AGENTS.md).

## Loop status (2026-10-11)
The loop runs from start to end, but it does not match the design yet:
- a crew that does not steer to the destination can fly forever: the autopilot only keeps the heading and there is no
  timeout;
- the run has no goal: it ends only when the whole crew is dead or the hull is destroyed (`PlayerLifeSystem` →
  lobby);
- players can join or come back in the lobby or while the ship stands at a station (`Build`); in flight, in a wreck,
  while the map changes and with a full crew they are turned away with a reason the menu shows (see "Player data and
  rejoin");
- there is no sound except the minigun turret's placeholder sounds (see "Weapons" in
  [WeaponModule](MirrorCoopBase/Assets/Features/WeaponModule/AGENTS.md)); the only VFX are
  the turret's, the rock break and the rock impact effects (the impact effect's `AudioSource` is the hook for a
  sound, empty for now).

The per-module status (modules, slots, station drops, stats and hull in
[ShipModule](MirrorCoopBase/Assets/Features/ShipModule/AGENTS.md); income in
[ShopModule](MirrorCoopBase/Assets/Features/ShopModule/AGENTS.md); breakdowns and breaches in
[ShipDamageModule](MirrorCoopBase/Assets/Features/ShipDamageModule/AGENTS.md)) and the known limits are in the module
files.

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
  button in the in-game debug window (see [DebugModule](MirrorCoopBase/Assets/Features/DebugModule/AGENTS.md)). One
  registration per action, no window edit.
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
- Module assemblies by layer (an assembly references only lower layers; module references in brackets for the modules
  without their own file; the references and contents of the others are in their module's file):
  1. `Features.GameCoreModule.Contracts`, `Features.AddressablesConstantGeneratorModule`, `Features.MvpModule`
     (AssetLoader), `Features.FloatingControllerModule`, `Features.MatrixModule`, `Features.PlayerProfileModule.Data`,
     `Features.CrewInviteModule.Data`, `Features.StagePresetModule.Data`, `Features.FlightFieldModule.Contracts`,
     `Features.WeaponModule.Contracts`, `Features.CameraModule.Contracts`, `Features.EffectPoolModule` (no references),
     `Features.DebugModule.Contracts`, `Features.SonarModule.Contracts`, `Features.SonarModule.Data`; third-party
     `QuickOutline` also has its own asmdef (the unused `Mini Mapa(Radar)` pack was deleted on 2026-10-25).
  2. `Features.CharacterMovableModule` (Contracts, FloatingController, Address, Stats, Input),
     `Features.GameFlowStateMachineModule` (Contracts, SceneLoader), `Features.FlightFieldModule`,
     `Features.WeaponModule`.
  3. `Features.CameraModule`, `Features.GrabModule`, `Features.DebugModule`.
  4. `Features.ShipModule`, `Features.MenuModule` (GameFlow, Mvp, Connection), `Features.PlayerProfileModule`.
  5. `Features.ShipImpactModule`, `Features.ShipDamageModule`, `Features.TurretModule`, `Features.PlayerLifeModule`,
     `Features.LobbyModule` (Ship, Grab, GameFlow, Mvp, Connection).
  6. `Features.StagePresetModule`, `Features.ShopModule`, `Features.TooltipModule`, `Features.CrewInviteModule`,
     `Features.SonarModule`.
  7. Composition root: `Features.GameCoreModule`, `Features.BootstrapersModule` (Contracts, GameFlow, SceneLoader).
  Every feature module's tests are a test assembly `Features.<Module>.Editor.Tests` (and `Game.Connection.Editor.Tests`,
  `NetworkModelModule.Editor.Tests`); its file says what they cover.

## Module map
| Module (details) | What it is |
|---|---|
| [ShipModule](MirrorCoopBase/Assets/Features/ShipModule/AGENTS.md) | the ship core: hull, flight, riders, slots and modules, seats and the helm, stats, world shift, the run, deck debugging |
| [ShipImpactModule](MirrorCoopBase/Assets/Features/ShipImpactModule/AGENTS.md) | the hull's rock and balloon collisions, knockback feedback, impact shake and effects |
| [ShipDamageModule](MirrorCoopBase/Assets/Features/ShipDamageModule/AGENTS.md) | module breakdowns, hull breaches, the leak, repair with the wrench and the hammer |
| [TurretModule](MirrorCoopBase/Assets/Features/TurretModule/AGENTS.md) | the minigun turret's seat station (`ShipTurretStation`) |
| [FlightFieldModule](MirrorCoopBase/Assets/Features/FlightFieldModule/AGENTS.md) | the flight field: generation, breakable rocks, target balloons |
| [WeaponModule](MirrorCoopBase/Assets/Features/WeaponModule/AGENTS.md) | the weapon core, its network path, the client feel |
| [SonarModule](MirrorCoopBase/Assets/Features/SonarModule/AGENTS.md) | the navigator's sonar console on the radar |
| [DebugModule](MirrorCoopBase/Assets/Features/DebugModule/AGENTS.md) | the in-game debug window and its action registry |
| [StagePresetModule](MirrorCoopBase/Assets/Features/StagePresetModule/AGENTS.md) | playtest stage presets |
| [ShopModule](MirrorCoopBase/Assets/Features/ShopModule/AGENTS.md) | the station shop, the crew wallet, buying back a dead crewmate |
| [GrabModule](MirrorCoopBase/Assets/Features/GrabModule/AGENTS.md) | interaction: aim, grab, use, hold to use |
| [CameraModule](MirrorCoopBase/Assets/Features/CameraModule/AGENTS.md) | the camera and the camera shake |
| [PlayerLifeModule](MirrorCoopBase/Assets/Features/PlayerLifeModule/AGENTS.md) | death, damage, spectator |
| [Connection](MirrorCoopBase/Assets/Features/Connection/AGENTS.md) | connection, authentication, joining a running session |
| [CrewInviteModule](MirrorCoopBase/Assets/Features/CrewInviteModule/AGENTS.md) | the "Hire crew" terminal |
| [NetworkModelModule](MirrorCoopBase/Assets/Features/NetworkModelModule/AGENTS.md) | synced models: model + bridge + generator |
| [PlayerProfileModule](MirrorCoopBase/Assets/Features/PlayerProfileModule/AGENTS.md) | player names and nameplates |
| [TooltipModule](MirrorCoopBase/Assets/Features/TooltipModule/AGENTS.md) | interaction tooltips |
| [GameCoreModule](MirrorCoopBase/Assets/Features/GameCoreModule/AGENTS.md) | composition root (installers), shared contracts, scenes |
| [MatrixModule](MirrorCoopBase/Assets/Features/MatrixModule/AGENTS.md) | generic grid with shape insert and A* path finding |
| [Loop tests](MirrorCoopBase/Assets/Tests/PlayMode/LoopSmoke/AGENTS.md) | `LoopSmokeTest` and its checks, the ride test, the soak test, adding a loop test check |
| [Two-player test](MirrorCoopBase/Assets/Tests/Coop/AGENTS.md) | `CoopTwoProcessTest` with a real second client process |
| `Features.AddressablesConstantGeneratorModule` | `Address_g.cs`; editor tools and tests load a configuration at its address with `AddressableConfigurationLoader` in `Features.AddressablesConstantGeneratorModule.EditorLoading` |
| `Features.EffectPoolModule` | `SelfDisablingEffectPool<TEffect>` over `ISelfDisablingEffect`, the pool of effects that disable themselves when over; the rock break and the rock impact effects use it |
| `Features.CharacterMovableModule` | player movement; tests: `Features.CharacterMovableModule.Editor.Tests` |

Other modules (Menu, Lobby, Mvp, Input, Stats, GameFlow, Bootstrapers, ...) have no file yet; add one when there is
something to describe.

## How to verify
Play mode starts from `BootstrapScene`, then menu → Host → game. Two-player multiplayer is checked with the
ParrelSync clone (`MirrorCoopBase_clone_0`).

The whole EditMode suite (about 1 000 tests) takes about 2.5 minutes (2 min 34 s, 1027 tests, on 2026-10-25): `Invoke-AgentEditor.ps1 -AgentDir . -Command run-tests -Mode EditMode`. While a test run is on, an error of the MCP for Unity bridge (message with the `MCP-FOR-UNITY` prefix, e.g. a lost websocket) goes to the log as a warning, so it fails no test, OneTimeSetUp or loop test error log; only that prefix, in one place: `Tests/Shared/Editor/TestLogBridgeErrorFilter` (editor, wraps `Debug.unityLogger.logHandler`, run state from the Test Runner callbacks; `TestLogBridgeErrorFilterTests`).

EditMode guard tests keep the data rules; each module's file lists its own.

Before every merge that touches the ship, landing, shop or session run `LoopSmokeTest` (see
[Tests/PlayMode/LoopSmoke](MirrorCoopBase/Assets/Tests/PlayMode/LoopSmoke/AGENTS.md)).

## Synced models

Data that must match for all players lives in a plain C# model. A **bridge** (`NetworkBehaviour`) carries it over the network. The model is read-only: UI and logic subscribe to its events, and only the bridge may write to it.

A client does not write to the bridge. A client request is its own `[Command]`: it calls a server service, and the service calls `ServerSet`. This is how purchase works (`CrewWallet.CmdPurchase` → `ServerTrySpend` → `ServerSetBalance`), and the `DebugCounterSample.CmdBump` example.

How the sync flow, atomic models, `Scope` / `PerPlayer` registries and the generator work:
[NetworkModelModule](MirrorCoopBase/Assets/Features/NetworkModelModule/AGENTS.md).

### Do / don't
- Do read `IReadOnly<Name>Model` and listen to `On<Field>Changed` and `OnChanged`. Until the bridge has called `OnStartClient`, `IsAvailable == false`.
- Do change data on the server only through `ServerSet` / `ServerAdd` / `ServerInsert` / `ServerRemove` / `ServerClear`.
- Don't write the model from gameplay, UI or a service.
- Don't give the bridge client setters.
- Don't copy all fields into the model in one hook when only one arrived over the network: a hook writes only its own field, or the model is atomic and the hook gets the whole struct.

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
  the `Assets/` root (older packs such as `SpaceSkies Free`, `QuickOutline`, `StarSparrow` are still
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
"Joining a running session" in [Connection](MirrorCoopBase/Assets/Features/Connection/AGENTS.md)). A player who
disconnected and joined again **gets their data back**: life
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
- Player identity and name come from Connection only through NetworkModel contracts: `IPlayerIdentityService`
  (`PlayerKey`) and `IPlayerNameSource` (the authenticated name), implemented in Connection. Other modules read a
  player's name through `IPlayerProfileSource` / `IReadOnlyPlayerProfileModel` (PlayerProfileModule.Data), not
  through the `PlayerProfile` bridge.
