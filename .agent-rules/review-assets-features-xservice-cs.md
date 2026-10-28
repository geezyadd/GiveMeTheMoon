<!-- globs: Assets/Features/**/*Service.cs; rules: 3; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/Features/**/*Service.cs`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### ROLE-001: Services are stateless
- **Priority:** Architecture
- **Rule:** A *Service holds no mutable game/session state in fields; it computes and returns, or mutates state passed to it (models). Only injected dependencies and readonly config may be fields. A Service also has no `event`s, no `Action` callback fields, no cached dictionaries and does not implement `IInitializable`/`IDisposable` — it is "input → output".
- **Why:** Stateful services become hidden shared singletons — the state belongs in a Model where it can be synced/reset/observed.
- **Bad → Good:** `private int _score;` mutated across calls / `LoadingScreenService { public event Action OnEndedFadeIn; private Action _pendingHideComplete; }` → move `_score` and the events into a Model (`LoadingScreenModel`); the Service reads/writes the model.
- **Fix:** Move mutable fields and events to a *Model; keep the service pure.


### ROLE-002: Services don't subscribe to events — they are called
- **Priority:** Architecture
- **Rule:** A *Service must not subscribe to events, network buses, or lifecycle callbacks; it exposes methods that Systems (or other services) call.
- **Why:** Event-driven flow is the System's job; a subscribing service hides control flow and has no clear teardown point.
- **Bad → Good:** `evt.OnFired += Handle;` inside a Service → the System subscribes and calls `service.Do(...)` in the handler.
- **Fix:** Remove subscriptions from the service; move them to the owning System's Initialize/Dispose.


### ROLE-006: Services are per-call — no cross-call state, no tick loop
- **Priority:** Architecture
- **Rule:** A *Service computes and returns within a single call; it must not cache or accumulate state across calls, nor expose a tick/update-driven step. Ongoing or time-based processing lives in a System that owns the loop and keeps its state in a Model.
- **Why:** A caching or ticking service is a hidden stateful process outside the System→Model flow — it can't be tested in isolation and its lifecycle/reset has no clear owner.
- **Bad → Good:** `fearFleeService.Tick(state, dt)` called every frame from a state, the service holding scratch buffers/timers → a *System owns the per-tick loop and writes results into a *Model; the service exposes only pure, single-call helpers.
- **Fix:** Move the loop into a System and the accumulated data into a Model; reduce the service to stateless pure methods (or drop it).
