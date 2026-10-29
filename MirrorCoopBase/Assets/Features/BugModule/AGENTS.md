# BugModule — basic bugs

The first enemy prototype: small flying bugs with a flexible tail on Animation Rigging. They come out from behind
rocks, fly at the ship, grab a cargo on board and carry it away or land and gnaw the ship until the crew shoots them.
A bug is a field agent kind ([FieldAgentModule](../FieldAgentModule/AGENTS.md)): its movement, rock avoidance, deck
mode, hidden spawn, batched sync and pooled looks are the field agents'; this module adds what a bug wants, its health,
its bites and its look. Read the [root AGENTS.md](../../../../AGENTS.md) first. Change this module's description here.

## Assembly
`Features.BugModule` (layer 6; references: GameCoreModule.Contracts, FieldAgentModule, Ship, ShipDamageModule,
WeaponModule.Contracts, SonarModule.Contracts, DebugModule.Contracts, EffectPoolModule, NetworkModel, Mirror, Zenject; the
tail rig's components (package `com.unity.animation.rigging` 1.3.1) live only on the prefab, so no script references
the rigging assembly). Its own module rather than more code in
FieldAgentModule: the base stays free of the ship damage, the weapons, the sonar and the rigging package, and later
creatures (drones, the nest) sit beside it the same way. Installed by `BugModuleInstaller` in `GlobalSceneInstaller`;
`BugServerModel` and `BugRandomSpawnModel` in `DataInstaller`; `BugConfiguration` in `ConfigurationInstaller`
(`BugConfigurationValidator` checks it at startup). Tests: `Features.BugModule.Editor.Tests`.

## Key places
| What | Where |
|---|---|
| Data | `BugConfiguration_Default` (Addressables `Configurations`): the kind (`FieldAgentProfile_Bug`, id `bug`, appended to `FieldAgentConfiguration.Profiles`: fast and twitchy, max speed 14, acceleration 40, turn rate 540, radius 0.8), the sonar kind, the death effect, health (10: one pistol shot, ten minigun hits), the approach ring (22 m, 5 m up, spread over 200 degrees), the darting flight, the bites, the cargo carry, leaving, the random spawns and the debug group size (4). The look numbers are `BugViewFeel` (referenced by the prefab) |
| The mind (server) | `Brain/`: `BugServerModel` (a `BugAgent` per field agent id: health, `BugStage`, gnaw point, cargo, time to the next bite), `BugBrainSystem` (every frame; forgets a bug whose agent is gone and every bug with the run's ship, through `BugRemovalService.ServerForgetVanished` / `ServerForgetAll`, so a carried cargo is let go where the bug was last, `BugAgent.LastWorldPosition`) -> `BugBrainService`: **Approach** (its own point on the ring round the ship) -> `BugTargetRule` (pure): a free cargo on board, the nearest -> **SeekCargo** -> grab (`ServerTake`) -> **Carry** (`CarryBehaviour` 400 m straight away from the ship, out past the haze, at `CarrySpeedShare` 0.35 of the top speed = 4.9 m/s, slower than the ship's 8 m/s cruise; was 0.6 = 8.4 m/s, faster than the ship; the darting flight and the rock avoidance make it weave) -> gone, the cargo let go there; no cargo -> the damageable module or hull point nearest to its side (else any, else the deck middle) -> **Land** (`AttachBehaviour`, deck mode) -> **Gnaw** (a bite every `BiteSeconds`). A cargo another bug took or a knock off the deck makes it choose again; once the ship no longer flies every bug takes off and **Leaves** (`FleeBehaviour`, gone at 300 m) |
| Flight feel | `Behaviours/BugDartBehaviour`: a decorator over every stage's field agent behaviour (`BugBehaviourFactory`): per cycle (1.5 s, own phase per bug) it nearly hangs still (30 % of the cycle at 8 % speed), then dashes to a side of its line (5 m, a new side each cycle) with a small loop (2.5 m, 0.9 s) on its course; the steering aims 10 m ahead, so it shows at any distance; within 10 m of the goal (a landing, a grab) it flies straight in. The bank in turns is the field agent feel (bank 0.25 deg per deg/s, up to 55 degrees). The group gathers on its arc of the ring, so it parts before the attack and comes at the ship from different sides |
| Bites | `Gnaw/BugGnawService`: the hull loses `BiteHull` (0.75 HP, cause `ShipDamageCause.Gnaw`, the bug's id as the source) through `IShipHull`, and the bite is a `ShipDamageModule` impact of `BiteStrength` (0.05) at the bug: its `ShipDamageRule` breaks the module beside it with 0.5 x 0.05 x the module's multiplier and opens a breach at the nearest free hull point with 0.6 x 0.05 per bite. Places to gnaw: `IShipDamageCandidateSource` (ShipDamageModule) |
| Health and death | `Health/`: `BugWeaponTargetSystem` registers once as an `IWeaponTargetSource` for the bug id range (`BugWeaponIds`: top byte 0xB0, low 32 bits the agent id); `BugWeaponTarget` -> `BugDamageService` (the hit's damage off the health) -> at 0 `BugRemovalService.ServerKill`: a cargo it carries is let go, the bug leaves the model and the field, and `BugDeathSync` (a `NetworkBehaviour` on the ship's `PoseSync` object, next to `FieldAgentSync`) sends `RpcDeath(id)` to every peer: a one-shot, nothing a late joiner needs |
| Look (every peer, local) | `GameResources/Prefabs/Bug.prefab` (primitives only, layer `FieldRock`): `FieldAgentView` + `BugBodyView` + `BugTargetCollider` (the trigger sphere a weapon ray strikes, its target id from the agent id; a strike makes the bug flinch) + `Animator` (no controller) + `RigBuilder`. Under the field agent body a `Frame` holds the thorax, head, red eyes, mandibles, two wings, six legs and the tail: `TailBase` with six `TailBone`s, each a `DampedTransform` on the `TailRig` (source the bone before, damp position 0.15, rotation 0.55, keep aim), so the tail trails the body and swings with every turn and dash. `BugViewMotion` (plain C#) gives the wing beat (18 Hz, folded with a buzz on the ship), the legs (dangling, scrabbling on the ship), the tail base's Perlin twitch (stronger while gnawing) and the gnawing nod and sway. Death: `BugDeathViewSystem` on `IBugDeathEvents` plays `BugDeathEffect` (a pooled particle burst, `BugDeathEffectPool`) where this peer shows the bug and hides its look until the next batch leaves it out |
| Spawn | `Spawn/BugSpawnService`: `IFieldAgentGroupSpawnService.SpawnHiddenGroup` (behind a rock from every player, else past the haze), then each bug gets its ring point. `BugRandomSpawnSystem` (server, in flight): a roll every `RandomSpawnIntervalSeconds` (60 s) with `RandomSpawnChance` (0.1), times `CargoSpawnChanceMultiplier` (3) while a cargo is on board (`BugRandomSpawnRule`), a group of 3-5; on while `BugRandomSpawnModel.IsEnabled` (from `IsRandomSpawnEnabled`, on by default; the loop tests switch it off in `LoopSmokeSession.StartGameCoroutine`) |
| Cargo | the contract is FieldAgentModule's `IFieldAgentCargoTarget` (`Cargo/`): a cargo on the deck registers in `IFieldAgentCargoTargetRegistry`; the real one is the delivery parcel ([ParcelModule](../ParcelModule/AGENTS.md)). The grab tells the cargo its carrier's agent id (`ServerTake(carrierAgentId)`), so every peer hangs the crate under that bug's look. Every way a carrier ends lets the cargo go: shot down, gone, its agent removed from outside ("Remove all field agents"), the ship gone; when the agent can no longer say where it is (`TryGetWorldPosition` fails) at the bug's last position. The bug tests use a stand-in (`Tests/PlayMode/Bug/BugTestCargo`) |
| Sonar | `Sonar/BugSonarContactSource` (bound in this installer as one more `ISonarContactSource`): every bug of this peer's last agent batch is a contact `bug:<id>` of `SonarKind_Bug` ("Bug", green, `GameResources/Sprites/Sonar_Bug.png`, not markable), listed in `SonarConfiguration.Kinds`, so it has its legend row |
| Debug | Debug window -> Agents (`Debug/BugDebugSystem`): "Spawn bugs (N)" (host with a ship, default 4), "A bug steals the parcel" (host with a ship and a free cargo on board: one hidden bug flies straight for it, `IBugBrainService.ServerGoForCargo`), "Random bugs: on / off" (host) |

## Known limits
- The ship's hull is not avoided in flight (a field agent limit): a bug can fly through the hull to its gnaw point.
- A bug landing on a module or a hull point off the deck NavMesh lands on the nearest NavMesh point (the field
  agents' landing sample).
- The carried-away cargo is only let go (`ServerRelease`); what happens to the parcel then is the parcel's (it is lost by distance or time before the bug is 400 m out).
- Bugs that are on the ship when the run ends go with the ship; at a station they take off and leave.
- No sounds.

## Tests
- `Features.BugModule.Editor.Tests`: `BugTargetRuleTests` (cargo before gnawing, the nearest free cargo, a taken one
  skipped, the damageable place nearest to its side, else any, else the deck middle), `BugGnawTests` (a bite takes the
  hull's HP with cause Gnaw from the bug; with a roll under the bite chance the real `ShipDamageRule` breaks the module
  beside it and opens a breach, above it only the hull; a bite is far weaker than a rock), `BugHealthTests` (the
  weapon target source joins and leaves the hit path, the id round trip, minigun hits leave health, the hit to 0 kills
  once, one pistol shot kills), `BugRandomSpawnRuleTests` (off = none, the cargo boost, the default asset on and rare,
  the group size range), `BugDartBehaviourTests` (freezes its share of every cycle, dashes to both sides, straight in
  near the goal, finished with its inner behaviour), `BugCargoReleaseTests` (a carrier shot down lets the cargo go where it
  flies, one whose agent was removed or cannot say where it is where it was last, the ship gone lets every cargo go, a bug
  without a cargo lets nothing go).
- PlayMode `Tests/PlayMode/Bug/` (host, the field agent tests' fast flight `FieldAgentTestFlight`, shared steps
  `BugTestSteps`; run one class per run; screenshots in the workspace's `.agent-orchestrator/screenshots/basic-bugs/`):
  `BugGnawTest` (the pistol bought at the kiosk; in a slow cruise the debug window switches the random bugs on and off
  and sends 4 bugs; one lands and gnaws, the synced hull drops; the host turns its view onto it through the look input
  and kills it with the pistol through the fire input; `bugs-flock-incoming`, `bug-gnawing-the-ship`,
  `bug-shot-with-pistol`), `BugCargoTest` (a stand-in cargo on the deck, one bug sent: it grabs the cargo, the cargo
  follows it, it carries it 60 m away, then "Remove all field agents" takes its agent and the cargo is let go;
  `bug-carrying-cargo`. It waited for the whole 400 m carry before and once ran into its 120 s deadline
  (`Assert.Less(Time.time, deadline)`: "Expected less than 154.773 But was 154.774"); the carrier is slower now).
- Coop: `CoopClientBugsTest` (`bugs`, see [Tests/Coop](../../Tests/Coop/AGENTS.md)).
