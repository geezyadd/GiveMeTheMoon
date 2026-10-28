# FieldAgentModule — flying field agents

The shared base for everything that flies through the flight field on its own: bugs, later drones, pirates, fish. The
server moves all agents in one pass over plain arrays, sends them to the clients as one batch, and every peer shows
them through pooled looks. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this
module's description here, not in the root file.

## Assembly
`Features.FieldAgentModule` (layer 5; references: GameCoreModule.Contracts, FlightFieldModule.Contracts,
DebugModule.Contracts, NetworkModel, Mirror, Zenject, ShipModule, SpaceHazeModule). Installed by
`FieldAgentModuleInstaller` in `GlobalSceneInstaller`; the models (`FieldAgentsModelInstaller`,
`FieldAgentServerModel`, `FieldAgentDiagnosticsModel`, `FieldAgentShownModel`; in editor / development builds
`FieldAgentFlockDebugModel`, `FieldAgentDeckTripDebugModel`) in `DataInstaller`; `FieldAgentConfiguration` in
`ConfigurationInstaller`. Tests: `Features.FieldAgentModule.Editor.Tests`. No `.Contracts` assembly yet: the features
built on it (bugs, the nest, parcel theft) sit above it and reference it directly; `IFieldAgentControl`,
`IFieldAgentBehaviour` and `IFieldAgentGroupSpawnService` are their API.

## Key places
| What | Where |
|---|---|
| Data | `FieldAgentConfiguration_Default` (Addressables `Configurations`; checked at startup by `FieldAgentConfigurationValidator`): the kinds, steering weights, rock query interval (with its margin and the per-id spread), send rate, interpolation delay, deck NavMesh anchor, hidden spawn, debug flock. A kind is one `FieldAgentProfile` asset (`GameResources/Configurations/`): flight limits (max speed, acceleration, turn rate), radius, rock look-ahead and clearance, separation, wander, deck walk speed, landing / take-off timings, the view feel and the view prefab. The kind number on the network is the profile's index in the list, so only append. One kind now: `test_agent` (`GameResources/Prefabs/TestAgent.prefab`, primitives only) |
| Server state | `Agents/FieldAgentServerModel`: parallel arrays (`FieldAgentState` struct, `IFieldAgentBehaviour`, `FieldAgentDeckPath`, `FieldAgentRockCache`) kept in id order; no object per agent in the scene. `FieldAgentState.Position` / `Velocity` / `Rotation` are world values in `Flight` and ship-local values in `Landing`, `Deck`, `TakingOff` (`FieldAgentMode`) |
| The tick | `Server/FieldAgentSimulationSystem` (server, `ITickable`): one pass over every agent by its mode (`FieldAgentFlightStepService`, `FieldAgentDeckStepService`, `FieldAgentTransitionService`), the cost into `FieldAgentDiagnosticsModel`, and a batch every 1 / `SendRate` s (`FieldAgentBatchPublishService`). No bound ship (lobby, menu) = no agents |
| Flight steering | `Steering/`: seek with arrival (`FieldAgentSteering`) + rock avoidance (`FieldAgentRockAvoidance`) + separation (`FieldAgentSeparation`, a plain pass over the flying agents) + Perlin wander (`FieldAgentWander`), clamped to the max speed, then `Vector3.RotateTowards` limits turn rate and acceleration. A goal in ship space (the ship, the orbit, a deck point) also gets the ship's velocity: the max speed is against the ship, so agents keep up with a ship in cruise |
| Rock avoidance | From the field data only (`IFieldRockQuery`, no colliders, no NavMesh in flight). Each agent queries the rocks around it every `RockQuerySeconds` (0.25 s, spread a little by id) into its `FieldAgentRockCache`, wide enough for where it can fly until the next query. A widened rock sphere (rock radius + agent radius + clearance) that the goal's course runs into within the look-ahead (`LookAheadSeconds` x speed, at least `MinLookAheadMeters`) bends the course onto the sphere's tangent, harder the nearer: the agent slides round the rock to its goal (a plain push away holds an agent in front of a 300 m giant). Inside the widened sphere it is pushed straight out; inside the rock sphere itself a last-resort push puts it on the surface (`FieldAgentDiagnosticsModel.RockPushOuts`, 0 in the tests) |
| Long path | No A* over the field matrix: the obstacle field is passable by design (FlightFieldModule: "nothing closes the plane, a way A -> B always exists") and the agents also pass above and below rocks; the tangent sliding goes round giants. Add a coarse matrix path here if a field profile ever builds walls |
| Behaviours | `Behaviours/`: small swappable classes behind `IFieldAgentBehaviour` (goal + finished): `SeekBehaviour` (world or ship-space point), `OrbitBehaviour` (ship-space circle round the ship), `AttachBehaviour` (land at a ship point and hold on), `CarryBehaviour` (drag an `IFieldAgentCargo`), `FleeBehaviour`, `IdleBehaviour`. The owner swaps them through `IFieldAgentControl.SetBehaviour` (no switch on types). A world point in a behaviour follows a world shift (`IWorldShiftFollower`) |
| Deck mode | The ship prefab's `NavMeshSurface` (on `ShipPlatform`, disabled, baked into `ShipModule/GameResources/NavMesh/ShipDeckNavMesh.asset`, physics colliders of the Default and Deck layers, `RideVolume` ignored by a `NavMeshModifier`; `ShipBase.DeckNavMeshData`). `Deck/FieldAgentDeckNavMeshSystem` (server) adds that data once at `DeckNavMeshAnchor` (0, -20000, 0) in ship space, so a ship-local point + the anchor is a NavMesh point however the ship flies or turns (`FieldAgentDeckNavigation`). A deck agent walks the corners of a cached `NavMesh.CalculatePath` (`FieldAgentDeckStepService`), so it only ever stands on the NavMesh. Landing: an `AttachBehaviour` goal flies the agent to `LandingApproachHeight` above its deck point, then eases it down in ship space (`LandingSeconds`) into `Deck`. Take-off (`IFieldAgentControl.TakeOff`): eases up `TakeOffHeight` in ship space, then flies on in the world with the ship's velocity plus `TakeOffSpeed` up; `KnockOff` puts it into flight at once with a given velocity. To re-bake after a deck change: select `ShipPlatform`, enable the surface, Bake, disable it again |
| World shift | `Server/FieldAgentWorldShiftSystem` (`IWorldShiftFollower`): flying agents and world-anchored behaviours move, rock caches are dropped; ship-space agents ride the ship. Clients shift their kept batches (`FieldAgentClientSystem` -> `FieldAgentFrameBuffer.Shift`) |
| Hidden spawn | `Spawn/FieldAgentHiddenSpawnService`: seeded candidates on a shell (`SpawnMinMeters`..`SpawnMaxMeters`) round a centre, clear of rocks; first a point a rock sphere hides from every player view (the line from the view to the point crosses the sphere), else a point past the haze's `HiddenMeters` (SpaceHaze) from every view. Views: `FieldAgentPlayerViewpointSource` (every connection's player + eye height; the camera itself is not synced). `FieldAgentGroupSpawnService` spawns a group round one hidden point |
| Network | One synced model `FieldAgents` (generated from `NetworkModelModule/GameResources/Definitions/FieldAgents.asset`, Shared, field `Frame`): a `FieldAgentFrame` (sequence, server network time, `FieldAgentSnapshot[]` sorted by id: id, kind, mode, position, Mirror-compressed rotation; 22 B per agent). Bridge `Network/FieldAgentSync` on the ship's `PoseSync` network identity. No NetworkIdentity or NetworkTransform per agent. A joining player gets the agents with the current frame |
| Client view | `Client/FieldAgentClientSystem` (every peer with a client) keeps the last `FrameBufferSize` batches (`FieldAgentFrameBuffer`) and the shown looks in `FieldAgentShownModel` (also `IFieldAgentShownAgents`, what a player sees), shows each agent `InterpolationDelaySeconds` (0.25 s) behind the server's network time between two batches (lerp / slerp while the mode stays, else the newer pose; no extrapolation), ship-space agents on this peer's own ship. Looks from `FieldAgentViewPool` (per kind). `FieldAgentView` + `FieldAgentViewFeel`: bank on turns, pitch with acceleration and a light bob, local only |
| Debug | Debug window → Agents (host with a ship): "Spawn test agents (N)" (default 20; `FieldAgentFlockDebugSystem`: hidden spawn, seek the ship, orbit when arrived), "Remove all field agents", "Land an agent on the deck" (`FieldAgentDeckTripDebugSystem`: one agent lands by the stern, walks to the helm, stands, walks back, takes off, orbits); availability from the ship's `IShipDebugActionConditions`. Their state lives in `FieldAgentFlockDebugModel` / `FieldAgentDeckTripDebugModel`; the tests read those models and `IFieldAgentShownAgents`, and call `IFieldAgentFlockDebug` / `IFieldAgentDeckTripDebug` |

## Measured (editor, host, 2026-10-28)
- 50 agents in cruise: server tick avg 0.45-0.9 ms, max 1.4-3.5 ms (`FieldAgentDiagnosticsModel`; before the per-agent
  rock cache it was 3.8 ms: the field query is the cost).
- Batch: 1 011 B for 50 agents (payload) x ~11-12 per second = ~11 KiB/s per client, plus Mirror's message header.

## Known limits
- Modules installed on the deck at runtime are not NavMesh obstacles (the NavMesh is baked with empty sockets); a deck
  agent walks through them.
- The ship's own hull is not avoided in flight: an agent seeking the ship can fly through the hull on its way.
- Separation is O(n²) over the flying agents: fine for 50-200, a grid if it ever gets more.
- The hidden-spawn views are the players' positions plus eye height, not their cameras.
- Agents stay when the ship lands at a station; they are cleared only when the run's ship goes (lobby, menu).
- No sounds.

## Tests
- `Features.FieldAgentModule.Editor.Tests`: `FieldAgentRockAvoidanceTests` (a rock on the course bends it away and
  brakes, a dead-centre course turns to the agent's side, rocks beside / behind / far give nothing, a seek through a
  rock flies round it without entering and arrives, the push out, the turn-rate limit), `FieldAgentSeparationTests`
  (push away from a close agent, none from far or deck agents, a flock seeking one point keeps its distance),
  `FieldAgentHiddenSpawnTests` (a rock between every camera and the point hides it, one camera past the rock does not,
  a rock round the point does not, every found point is clear and hidden from every camera, past the haze without
  rocks), `FieldAgentFrameBufferTests` (interpolation, mode change, new / gone agents, before / after the batches,
  old batches ignored, the ring, world shift), `FieldAgentModeTransitionTests` (landing in ship space onto the deck
  point, take-off back to the world with the ship's velocity, attach asks for a landing, model removal keeps id order).
- PlayMode `Tests/PlayMode/FieldAgent/` (host, fast loop timings; run one class per run; shared steps in
  `FieldAgentTestFlight` and `FieldAgentFlockSteps`; screenshots in the workspace's
  `.agent-orchestrator/screenshots/field-agents/`): `FieldAgentFlockTest` (the debug window's spawn button: 20 agents
  from a hidden point fly to the ship in a slow cruise, never inside a rock sphere and without a last-resort push, and
  orbit it; the host shows all of them), `FieldAgentRockOrbitTest` (the stopped ship beside a rock so the orbit runs
  through its sphere: every agent bends round it), `FieldAgentLoadTest` (50 agents: no rock hit, the tick under 2 ms
  on average, the batch size and rate logged) and `FieldAgentDeckTest`
  (the debug window's deck trip with the ship turning all the time: lands, walks to the helm and back on the deck
  surface on the server and as the host shows it, takes off).
- Coop: `CoopClientFieldAgentsTest` (`field-agents`, see [Tests/Coop](../../Tests/Coop/AGENTS.md)).
