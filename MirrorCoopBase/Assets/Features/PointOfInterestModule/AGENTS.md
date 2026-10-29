# PointOfInterestModule — points of interest

A reason for the crew to leave the straight course: one to three points per flight, bright on the sonar, off the
course to the station, each a zone with content the server starts when the ship enters it and a reward container in
the middle that pays the crew once. The only content now is variant D, **the bug nest**. Read the
[root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in
the root file.

## Assembly
`Features.PointOfInterestModule` (layer 7; references: GameCoreModule.Contracts, FlightFieldModule.Contracts,
CutsceneModule.Contracts, Bug, FieldAgent, Ship, Shop (the crew wallet), SonarModule.Contracts, WeaponModule.Contracts,
DebugModule.Contracts, NetworkModel, Mirror, Zenject). Installed by `PointOfInterestModuleInstaller` in
`GlobalSceneInstaller`; the models (`PointsOfInterestModelInstaller`, `PointOfInterestServerModel`,
`PointOfInterestSceneModel`) in `DataInstaller`; `PointOfInterestConfiguration` and `BugNestConfiguration` in
`ConfigurationInstaller`. Tests: `Features.PointOfInterestModule.Editor.Tests`.

## Key places
| What | Where |
|---|---|
| Placement | Not here: the flight field's own generation step `FieldPointOfInterestStep` ([FlightFieldModule](../FlightFieldModule/AGENTS.md)) places the points from the field seed and keeps a circle round each free of rocks; the server reads them through `IFieldPointOfInterestSource` (FlightFieldModule.Contracts) |
| Data | `PointOfInterestConfiguration_Default` (`GameResources/Configurations`, Addressables `Configurations`, checked by `PointOfInterestConfigurationValidator`): the contents (picked by weight), the zone radius (100 m), the ship's ram reach (9 m), the hit tolerance (3 m), the sonar kind, the debug approach (25 m). A content is a `PointOfInterestContentDefinition` asset: id, weight, the look prefab, the reward container's radius (4 m), hit points (60: five pistol shots, three seconds of minigun) and reward (150, about one good flight of ore). `BugNestConfiguration_Default`: its content (`PointOfInterestContent_BugNest`, id `bug_nest`), 6 bugs 12 m behind the container, the flock search radius for the cutscene (160 m), `PlayEntryCutscene` (on) and the cutscene's camera and seconds (look at the nest 1.6 s from 22 m behind and 12 m above the ship, follow the flock 2 s from 0.4 of the way from the nest to the ship; with the 0.8 s return blend about 4.4 s) |
| Network | One synced model `PointsOfInterest` (generated from `NetworkModelModule/GameResources/Definitions/PointsOfInterest.asset`, Shared, list `Points` of `PointOfInterestEntry`: id, content id, world position, `PointOfInterestState` Dormant / Triggered / Opened). Bridge `Network/PointOfInterestSync` on the ship's `PoseSync` network identity, found through `IComponentRegistry<PointOfInterestSync>`; read and written through `LivePointsOfInterest` (`IPointsOfInterest`, `IPointOfInterestWriter`). A joining player gets the points and their states with the ship |
| Server | `Server/PointOfInterestWorldShiftSystem` (`IWorldShiftFollower`) moves every point with a world shift. `Server/PointOfInterestServerSystem` (every frame, server with the ship's identity): a new field number places the field's points (`PointOfInterestPlacementService`: the old ones cleared, each new one Dormant with a content rolled from the field number); then the ship's middle inside a dormant point's zone (`PointOfInterestZoneRule`) starts it once (`PointOfInterestTriggerService`: Triggered, then the content's `ServerEnter`), and the hull reaching a standing container (container radius + ram reach) opens it. `PointOfInterestOpenService` is the one way to open a container: Opened, then the content's reward into the crew wallet (`ICrewWalletBindingModel`), once (`PointOfInterestOpenResult`) |
| Content | `Content/IPointOfInterestContent` (content id + `ServerEnter`): a new kind is one more class bound in `InstallContents` and a content definition, never a branch on the type. `BugNestContent` (variant D): the bugs come out 12 m behind the container, seen from the ship, through `IBugSpawnService.ServerSpawnGroupAt` ([BugModule](../BugModule/AGENTS.md)) and attack as every bug group does; with `PlayEntryCutscene` on the throttle goes to Stop (`IShipThrottleStop`, ShipModule: the cruise eases to a halt) and the entry cutscene plays for everyone (`ICutsceneDirector`, [CutsceneModule](../CutsceneModule/AGENTS.md); shots from `BugNestCutsceneShots`); with it off the ship keeps flying and no cutscene plays. The cutscene's follow shot names the target `nest-flock:<id>` (`BugNestFlockTargetIds`); `BugNestFlockTargetSource` answers on every peer with the middle of the agents it shows within the flock search radius of the nest |
| Reward container (weapons) | `Weapons/`: `PointOfInterestWeaponIds` (weapon target ids with top byte 0xC5, low 32 bits the point id), `PointOfInterestWeaponTargetSystem` registers once as an `IWeaponTargetSource`; `PointOfInterestWeaponTarget` (`IWeaponDamageTarget`) hands a hit to `PointOfInterestDamageService`: the point stands and the reported point lies on the container (radius + tolerance), the damage adds up in `PointOfInterestServerModel`, at the hit points it opens. Any weapon works (pistol, turret) |
| Look (every peer) | `Client/PointOfInterestViewSystem`: one look per standing point from its content's prefab (made through `IInstantiator`, kept in `PointOfInterestSceneModel`), taken away when its container opens or its field goes. `GameResources/Prefabs/PointOfInterest_BugNest` (primitives only): a heavy dark metal capsule (`BugNest_Hull`, metallic) with glowing green bands (`BugNest_Glow`, HDR emission) and glowing amber cores at its ends (`BugNest_Core`), tilted; at the root a trigger sphere on the `FieldRock` layer (off until shown) with `PointOfInterestView` (`IWeaponTargetCollider`). Opaque URP Lit, so the [space haze](../SpaceHazeModule/AGENTS.md) hides it beyond the clear distance like every rock: by eye it shows only up close |
| Sonar | `Sonar/PointOfInterestSonarContactSource` (bound in this installer as one more `ISonarContactSource`): every point whose container stands is a contact `poi:<id>` of `SonarKind_PointOfInterest` ("Point of interest", bright pink, `Sonar_Target` icon, 30 px, markable, shown on the scope's edge beyond the range, pulsing 1.2 times a second by 0.45 of its size), listed in `SonarConfiguration.Kinds`, so it has its legend row |
| Debug | Debug window → Points of interest (host in flight, `PointOfInterestDebugSystem`, `IPointOfInterestDebug` for the tests): "Move the ship to the nearest point of interest" (the nearest standing one; the ship 25 m outside its zone, facing it: the cruise flies it in), "Trigger the point of interest now" (the nearest dormant one, as if the ship entered its zone). The test cutscene is CutsceneModule's |

## Known limits
- The points hold world positions: placed at the field's publish, cleared with the field; a world shift moves them on
  the server (`PointOfInterestWorldShiftSystem`) and the clients get the moved list, so a client's look may lag the
  shift by its sync.
- Ramming is the ship's middle against a sphere (ram reach 9 m), not the hull boxes; the ship flies through the
  container's look.
- The bug nest's bugs fly at the ship and stay like any group; nothing else guards the nest after they come out.
- A container opens without an effect (no balloons, confetti or numbers, by design); its look just goes.
- No sounds.

## Tests
- `Features.PointOfInterestModule.Editor.Tests` (`PointOfInterestServerTests`: the zone starts the content once, inside
  and outside the zone, the container pays once when opened twice, hits up to the hit points open it and pay once, a
  hit off the container does nothing, the ram reach; `BugNestContentTests`: the bugs come out behind the nest, the
  cutscene on stops the ship and plays a 3-5 s cutscene that looks at the nest and follows the flock, off neither stops
  nor plays but the bugs come out).
- PlayMode `Tests/PlayMode/PointOfInterest/PointOfInterestNestTest` (host, the field agent tests' fast flight; screenshots
  in the workspace's `.agent-orchestrator/screenshots/poi/`): the debug window's move, the zone entered by the cruise,
  the entry cutscene (controls blocked, HUD hidden, throttle at Stop, ends by itself), the nest's bugs out, the
  container shot open with the pistol through the fire input, the balance up by the reward once; the debug test
  cutscene skipped with Space.
- Loop test: `LoopSmokePointOfInterestCheck` (see [Tests/PlayMode/LoopSmoke](../../Tests/PlayMode/LoopSmoke/AGENTS.md)).
- Coop: `CoopClientPointOfInterestTest` (see [Tests/Coop](../../Tests/Coop/AGENTS.md)).
