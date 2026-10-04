# GiveMeTheMoon — project context

Read before any task in this repository: humans, Claude and worker agents.

## The game
A co-op **friendslop** in the vein of Peak, YAPYAP and Lethal Company. A crew of players flies a platform-ship from
station to station and upgrades the ship with modules along the way.

### Core loop (current focus)
1. Start at the first station with base money, enough for the simplest upgrades.
2. **Management phase.** Players buy modules in the shop (**L** key) and install them in ship slots: engines,
   propeller, helm, radar, more later. The balance is shared by the crew.
3. **Flight phase.** The ship flies to the next station while players stand and walk on the deck.
4. Arrival at the next station: new purchases and upgrades, then the loop repeats.

Intent: in flight the ship stays almost still and the world moves past it, so players on deck get less jitter. This
is not final yet and needs evaluation. **How the code works now** (`ShipFlight`, `ShipRunService`):
- on takeoff the ship moves over 9 s to a hover point (+350 m forward, +100 m up);
- in cruise it hangs at that point: only heading and side sway during dodging change;
- flight progress is a timer (`ShipTransit`), not distance;
- only rocks fly past the ship;
- on landing the next pad spawns and the ship moves onto it over 9 s.

## Loop status (2026-10-01)
The loop runs from start to end, but it does not match the design yet:
- on every landing `ServerResetForBuild` removes **all** installed modules;
- modules drop for free at a station (`ShipStationCatalog.Drops`);
- the shop opens anywhere;
- there is no income, only the starting balance;
- only the `FlightSpeed` stat affects the game;
- rocks are harmless;
- there is no sound or VFX.

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
  1. `Features.GameCoreModule.Contracts` (`IGameplaySession`, `SceneNames`),
     `Features.AddressablesConstantGeneratorModule` (`Address_g.cs`), `Features.MvpModule` (AssetLoader),
     `Features.FloatingControllerModule`; third-party `QuickOutline` and `MiniMapModular` (`Mini Mapa(Radar)`) also
     have their own asmdef.
  2. `Features.CharacterMovableModule` (Contracts, FloatingController, Address, Stats, Input),
     `Features.GameFlowStateMachineModule` (Contracts, SceneLoader).
  3. `Features.CameraModule` (CharacterMovable, Contracts, Input) → `Features.GrabModule` (Camera, CharacterMovable).
  4. `Features.ShipModule` (Camera, Grab, CharacterMovable, FloatingController, GameFlow, Stats, Connection, MiniMap),
     `Features.MenuModule` (GameFlow, Mvp, Connection).
  5. `Features.PlayerLifeModule` (Ship, Grab, Camera, Mvp, NetworkModel, Connection), `Features.LobbyModule` (Ship,
     GameFlow, Mvp, Connection).
  6. `Features.ShopModule` (PlayerLife, Ship, Camera, GameFlow, Mvp, NetworkModel) → `Features.TooltipModule` (Shop,
     Ship, Grab, Mvp).
  7. Composition root: `Features.GameCoreModule` (all modules) and `Features.BootstrapersModule` (Contracts,
     GameFlow, SceneLoader).
  Tests: `Features.CharacterMovableModule.Editor.Tests`, `Features.PlayerLifeModule.Editor.Tests`,
  `Features.ShipModule.Editor.Tests`, `Game.Connection.Editor.Tests`.
- The camera does not know about Grab: `IGameCameraService.LookApplied` fires in `PresentNow()` after
  `ApplyLookRig()` and before the brain update; `HeldItemFollowSystem` (Grab) listens to it. The frame order is the
  same as before.
- `LoopSmokeTest` stays in `Assembly-CSharp`: every assembly is visible from there. Whatever the test touches in a
  module must be `public` (not `InternalsVisibleTo`).

### Key places
| What | Where |
|---|---|
| Ship, flight, riders | `Assets/Features/ShipModule/Scripts/`: `ShipBase`, `ShipFlight`, `ShipPoseSync`, `ShipRider`, `ShipRunService`, `ShipRunDirector` |
| Flight and landing settings | `ShipFlightSettings` (+ `ShipFlightConfig_Default.asset`), `ShipRunConfig` |
| Modules and slots | `ShipItem`, `ShipSocket`, `Ship*Interactable`, catalogs in `ShipModule/GameResources/Resources/` |
| Stats | `Assets/Features/StatsModule/`, `ShipStatType`, `ShipStatEntity` |
| Interaction | `Assets/Features/GrabModule/` (`UseController`, `InteractableBase`, `Grabbable`) |
| Shop and money | `Assets/Features/ShopModule/` (`CrewWallet`, `ShopCatalog`, `WalletConfiguration`) |
| Scenes | `GameCoreModule/GameResources/Scenes/`: Bootstrap → Menu → Lobby → Game; only Bootstrap is in Build Settings |

### How to verify
Play mode starts from `BootstrapScene`, then menu → Host → game. Two-player multiplayer is checked with the
ParrelSync clone (`MirrorCoopBase_clone_0`).

### Loop autotest
The PlayMode test `LoopSmokeTest` (`Assets/Tests/PlayMode/LoopSmoke/`) plays the M1 loop on a host by itself:
menu → Host → lobby → Start → `GameScene`, installs 2 engines, takes off and lands twice, and checks the phases,
`LoopIndex`, engines after landing, the ship on the new pad, the player on deck, no wreckage, return to menu and no
errors in the log. The test shortens flight timings with its own copy of `ShipRunConfig`; assets are not changed. The
test takes about 30 s.

Run from the workspace root:
`Invoke-AgentEditor.ps1 -AgentDir . -Command run-tests -Mode PlayMode -Filter LoopSmoke`
(or Test Runner → PlayMode → `LoopSmokeTest`).

**Run it before every merge that touches the ship, landing, shop or session.** The test lives in Assembly-CSharp
under `#if UNITY_INCLUDE_TESTS`, so `playModeTestRunnerEnabled` is on in `ProjectSettings`. New allowed log errors go
only into the `LoopSmokeErrorLog` allow-list, with a reason.

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
- Development branch: `GameCore`.
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
- New tuning numbers go into configs (ScriptableObject), not into `const`.
- **All configurations (ScriptableObject configs and catalogs that code receives through DI) are loaded only through
  Addressables and bound only in `ConfigurationInstaller`** (`Container.BindConfigurationFromAddressables<T>(...)`).
  The address is a constant from the generated `Address.Configurations.*` (`Address_g.cs`, generator in
  `Tools/Generations/Code generation`), not a hand-written string. Not allowed: `Resources.Load`, `Resources/`
  folders for configs, loading a config in a module installer via `FromMethod`, your own address-constant classes.
  New config: asset into the `Configurations` Addressables group → regenerate `Address_g.cs` → a line in
  `ConfigurationInstaller`.
- Verify every platform bug and feature in Play mode.
- **All repository text is in English:** `.md` files (AGENTS.md, CLAUDE.md, docs, READMEs), code comments, commit
  messages. This holds even when the task or chat is in another language.
- **All code comments are in English, with no exceptions:** `//` and `/* */` comments, XML doc comments, `TODO`
  notes, `[Tooltip]` / `[Header]` texts, and comments in shaders, asmdefs, scripts and generated code templates.
  When you touch a file with a non-English comment, translate that comment.

## Player data and rejoin (rule for all new features)
Future goal: players can **join a running session while the crew is at a station** (Build phase; joining is closed in
flight). A player who disconnected and joined again **gets their data back**.

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
