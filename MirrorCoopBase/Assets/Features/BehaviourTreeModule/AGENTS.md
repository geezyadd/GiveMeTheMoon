# BehaviourTreeModule — code-built behaviour trees for future enemies

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

Status: the framework only. No enemy, scene, prefab or installer uses it yet.

## How a tree is written
A tree is a plain C# class derived from `BtTree`. It takes its services and configs through the constructor and
describes its nodes in `Describe` with the fluent `BtBuilder`:

```csharp
public sealed class GruntTree : BtTree {
    private readonly IGruntSenses _senses;

    public GruntTree(IGruntSenses senses) =>
        _senses = senses;

    protected override BtNode Describe(BtBuilder builder) =>
        builder
            .Priority("Root")
                .Guard("Sees player", _senses.SeesPlayer).Recharge("Bite recharge", 2f).Add(new BiteStep())
                .Sequence("Wander")
                    .Check("Is idle", _senses.IsIdle)
                    .Add(new PickWanderPointStep())
                    .Delay("Rest", 1f)
                .End()
            .End()
            .Build();
}
```

Composites (`Priority`, `Sequence`, `ScoredChoice`) stay open until `End()`; decorators (`Guard`, `Not`, `Recharge`,
`Scored`) wrap the next node and close by themselves; leaves (`Check`, `Delay`, `Do`, or any node through `Add`) go
where the builder stands. `Build()` throws when something is left open. Tuning numbers (delays, recharge times, tick
rate) come from the tree's config, not literals, once a real tree exists.

## Nodes (`Scripts/Nodes`)
| Node | Behaviour |
|---|---|
| `BtPriority` | reactive selector: every tick from the first child; a higher child that can enter interrupts the running lower one, and the lower branch's `OnStop` runs before the higher one's `OnStart` |
| `BtSequence` | runs children in order, remembers the running step (succeeded steps are not re-run), re-checks the pure checks before that step every tick; a check that stops holding interrupts the step and fails the sequence |
| `BtScoredChoice` + `BtScoredBranch` | the option with the highest positive score runs; the running option gets `stickiness` on top of its score against flip-flop; a failing option hands over to the next best in the same tick |
| `BtCheck` | leaf from a named `Func<bool>`; the only built-in pure check |
| `BtGuard` | runs its child only while a `Func<bool>` holds, else interrupts it and fails |
| `BtNot` | flips Success / Failure |
| `BtDelay` | Running for N seconds of tree clock from the tick it starts, then Success |
| `BtRecharge` | after its child succeeds, fails for N seconds of tree clock without ticking the child; the clock runs while the branch is skipped |
| `BtAction` (`Do`) | inline leaf from `Func<float, BtStatus>` |

A custom node derives from `BtNode` (leaf), `BtDecorator` (one child) or `BtComposite` (many) and overrides
`OnRun(deltaTime)` and optionally `OnStart` / `OnStop`. `OnStop` runs on finish **and** on interruption, after the
node's running children were stopped. `Children` is a read-only list: iterate it with `for`.

Queries a priority uses so it does not interrupt a running branch for nothing (both must have no side effects):
- `CanEnter`: whether starting the node now could do anything but fail at once ("would it fail now" is its negation).
  A guard can enter when its predicate holds and its child can enter; a recharge only when it is ready.
- `IsPureCheck`: the node finishes in the tick it starts and has no side effects (`BtCheck`, `BtNot` over a check),
  so a sequence may re-check it every tick.

## Timing and allocation rules
- `BtTree.Update(dt)` advances the tree's `BtClock` (`TickCount`, `ElapsedSeconds` = sum of dt) and ticks the root.
  Nodes measure time only with dt or `Clock`, never `Time.deltaTime` / `Time.time`. It throws before `Build()` and
  after `Dispose()`.
- `BtTickRateLimiter`: `BtTree.TicksPerSecond` 0 ticks on every host update; otherwise it ticks at that rate and
  passes all the time gathered since the previous tick, so the tree clock never loses time.
- Nodes do not move anything smoothly themselves: they command components that do.
- No per-tick allocations in nodes; delegates and nodes are created once in `Describe`. `DebugText` may allocate (only
  the editor inspector reads it).
- A node that starts and finishes in one tick with its previous result raises no `OnStatusChanged`, so re-run checks
  do not flood the inspector's log.

## Hosts and injection
- `BtTreeRunner<TTree>` (MonoBehaviour, single peer): builds the tree in `Start`, ticks it in `Update` through the
  rate limiter, halts it in `OnDisable`, disposes it in `OnDestroy`.
- `NetworkBtTreeRunner<TTree>` (`Scripts/Network`, Mirror): the tree exists and ticks only on the server, created in
  `OnStartServer` and disposed in `OnStopServer`; clients have no tree and nothing of it is synced. What the tree
  commands is synced by the components it drives.
- Unity cannot add a generic component: every tree gets a one-line subclass of a runner.
- Both runners get `DiContainer` through `[Inject] InjectDependencies` and use `BtTreeFactory`: the tree is created with
  `DiContainer.Instantiate<TTree>()` (constructor injection), built, and then every node is injected with
  `DiContainer.Inject`. **Exception to the constructor-injection rule:** nodes are created with `new` inside
  `Describe` (their constructor takes plain values only), so a node gets its services through an `[Inject]` method
  named `InjectDependencies`. Services are not available in a node's constructor.
- Both runners implement `IBtTreeHolder` (`TryGetTree`), which the editor inspector reads.

## Editor inspector (`Scripts/Editor`)
**Window > GiveMeTheMoon > BT Inspector**: in Play mode select a GameObject with a runner → live top-down graph of
its tree, boxes coloured by status (grey inactive, yellow running, green success, red failure), dimmed when not
ticked on the last tick; drag (left / middle button) to pan, scroll to zoom, F or **Frame** to fit; the side panel
lists the last 60 status changes with tree clock time. A client shows nothing for a network runner (no tree there).
Files: `BtInspectorWindow` (wiring), `BtGraphLayout`, `BtGraphCamera`, `BtGraphPainter`, `BtStatusChangeLog`.

## Assemblies
| Assembly | References |
|---|---|
| `Features.BehaviourTreeModule` (layer 1, `Scripts/`) | Zenject |
| `Features.BehaviourTreeModule.Network` (`Scripts/Network/`) | core, Mirror, Zenject |
| `Features.BehaviourTreeModule.Editor` (`Scripts/Editor/`, Editor only) | core |
| `Features.BehaviourTreeModule.Editor.Tests` (`Scripts/Editor/Tests/`, EditMode) | core, Zenject, test runner |

## Tests
`Features.BehaviourTreeModule.Editor.Tests` (`run-tests -Mode EditMode -Filter BehaviourTree`): the rate limiter,
priority interruption order (stop before start) and no interruption for a recharging branch, sequence memory and
re-checked checks, guard, delay, recharge with a skipped branch, scored choice stickiness and fall-through, the tree's
clock / build / dispose rules, the factory's constructor and node injection.

## Known limits
- The editor inspector was checked only for opening; it has not been tried on a live tree (no runner exists in a
  scene yet).
- When a higher priority branch can enter but then fails in the same tick, the lower branch it interrupted starts
  again (OnStart) in that tick: keep `CanEnter` accurate for custom nodes.
