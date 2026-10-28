<!-- globs: Assets/**/*.cs; rules: 74; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/**/*.cs`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### ADDR-001: Release every Addressables handle you load
- **Priority:** Critical
- **Rule:** Every `LoadAssetAsync`/`InstantiateAsync` handle must be released (`Addressables.Release` / `ReleaseInstance`) when done.
- **Why:** Unreleased handles leak memory and keep bundles resident — a top cause of runaway memory.
- **Bad → Good:** loading then never releasing → store the handle, release in `OnDestroy`/on unload.
- **Fix:** Keep the `AsyncOperationHandle`; release it in the matching teardown.


### ADDR-002: Check handle status before using the result
- **Priority:** Critical
- **Rule:** After awaiting a load, verify `handle.Status == Succeeded` before using `handle.Result`.
- **Why:** A failed load leaves `Result` null → `NullReferenceException` downstream.
- **Fix:** Branch on status; log and bail on `Failed`.


### BUILD-001: No UnityEditor usage in runtime code
- **Priority:** Critical
- **Rule:** `UnityEditor`, `UnityEditor.*`, and editor APIs (`[MenuItem]`, `AssetDatabase`, `EditorGUILayout`, …) may only appear in files under an `/Editor/` folder (Editor asmdef) or wrapped in `#if UNITY_EDITOR`.
- **Why:** Editor types don't exist in a player build — a stray reference fails the build (or strips at runtime and throws).
- **Bad → Good:** `using UnityEditor;` in a runtime `*System.cs` → move the file to `/Editor/`, or guard the block with `#if UNITY_EDITOR ... #endif`.
- **Fix:** Relocate to an Editor asmdef, or fence with #if UNITY_EDITOR.


### BUILD-002: Editor-only members guarded, not just the using
- **Priority:** Critical
- **Rule:** When a runtime file uses editor APIs behind `#if UNITY_EDITOR`, every call site AND the field/method declarations that touch editor types are inside the same guard.
- **Why:** Guarding only the `using` still leaves editor-typed fields/params in the compiled player build → compile error.
- **Fix:** Wrap the full declaration + all usages, not just the import.


### BUILD-006: No runtime type generation
- **Priority:** Critical
- **Rule:** No `TypeBuilder` / `System.Reflection.Emit` in runtime code.
- **Why:** IL2CPP (mobile/console) doesn't support it — it works in the editor and fails on device.
- **Fix:** Use edit-time codegen or a non-emitting approach.

### CFG-006: Runtime code never mutates config / DB objects
- **Priority:** Critical
- **Rule:** Data taken from an SO or DB table is copied before being modified; configs are exposed read-only.
- **Why:** In the editor the change leaks into the asset; at runtime it leaks across sessions.
- **Bad → Good:** `List<Bonus> bonuses = _config.Bonuses; bonuses[0].Value += x;` → work on a copy.
- **Fix:** Clone before mutating; expose `IReadOnly*`.


### CORO-001: Stop coroutines you start when the object is disabled/destroyed
- **Priority:** Critical
- **Rule:** Keep a reference to started coroutines and stop them in `OnDisable`/`OnDestroy` (and in the `Exit` of the state that started them), or use a cancellation path. Stop the previous run before starting the same coroutine again, and set the stored handle to `null` after `StopCoroutine` or when the coroutine finishes — Unity doesn't.
- **Why:** Coroutines touching a destroyed object throw or act on stale state → NRE / logic bugs; a second start runs two copies in parallel.
- **Bad → Good:** `StartCoroutine(Fade())` never stopped / started twice → `if (_fade != null) StopCoroutine(_fade); _fade = StartCoroutine(FadeCoroutine());` and `StopCoroutine` + `_fade = null` in `OnDisable`.
- **Fix:** Track the `Coroutine` handle, stop it in teardown and before restart, null it afterwards.


### ERR-001: Impossible state throws a specific exception — no LogError-and-default
- **Priority:** Critical
- **Rule:** When a method can't fulfil its contract, throw the most specific exception (`ArgumentOutOfRangeException`, `KeyNotFoundException`, a custom one) with context (what failed, which value); a switch/if-chain over enums or types has a throwing `default`. Runtime logic throws; `Assert` is only for wiring checks in `Awake` and editor code. Offer `TryX` when absence is legitimate; a fallback value is allowed only for purely cosmetic data, with a comment.
- **Why:** Logged errors get ignored while the bad default spreads.
- **Bad → Good:** `if (!_cache.ContainsKey(s)) { Debug.LogError(..); return Vector3.zero; }` / `else throw new NotImplementedException();` → `if (!_cache.TryGetValue(s, out Vector3 p)) throw new KeyNotFoundException($"No spawn point for {s}");` / `default: throw new ArgumentOutOfRangeException(nameof(type), type, null);`
- **Fix:** Replace log-and-default with a specific exception.


### ROLE-004: Every subscription has a matching unsubscription
- **Priority:** Critical
- **Rule:** Every `+=`, `AddListener` or Rx `Subscribe` has a matching unsubscription in the owner's paired teardown: `Initialize`/`Dispose` (Systems, POCOs), `OnEnable`/`OnDisable` or `Awake`/`OnDestroy` (MonoBehaviours), `OnViewSet`/`OnDisposed` (Presenters), `Spawned`/`Despawned` (NetworkBehaviours), window open/close. Handlers are named methods, never lambdas/anonymous delegates; Rx subscriptions go into one `CompositeDisposable` per owner; a conditional subscription caches its condition so the unsubscribe runs under the same condition. A `+=` inside teardown is a bug.
- **Why:** Un-torn-down subscriptions fire on dead objects across sessions → NRE and duplicated handling.
- **Bad → Good:** `+=` in Initialize with no `-=` / `OnViewDisabled() { stat.OnValueChanged += SetToggleValue; }` / `RemoveListener(delegate {...})` → mirror every subscription with `-=` of the same named method in the paired teardown.
- **Fix:** Pair each subscription; verify symmetry.


### UNITY-002: No null checks on guaranteed references — fail fast
- **Priority:** Critical
- **Rule:** References whose existence we guarantee are used directly with no null check, `?.` or `??`: injected dependencies and session invariants (see DI-005), Inspector-wired fields, ScriptableObject data, required components on the same prefab, objects this class creates and owns. At most one `Assert` in `Awake`.
- **Why:** A guard on something that must exist hides a broken binding/wiring and turns it into odd behaviour far from its cause.
- **Bad → Good:** `if (_context == null || _context.HomePrefab == null) return false;` / `_presets ?? Array.Empty<Preset>()` → use directly.
- **Fix:** Delete the guard; if absence is a legitimate state, model it explicitly (Mock / Null object / `TryX`). For references that can vanish, see UNITY-009.


### UNITY-005: Renaming a serialized field must not lose asset values
- **Priority:** Critical
- **Rule:** When a serialized field is renamed, the same PR re-saves the affected prefabs/scenes/SOs with the values preserved. `[FormerlySerializedAs]` is used only when re-saving is impractical (very many assets, assets owned elsewhere) and is removed once assets are migrated. Don't add it by default.
- **Why:** Unity matches serialized data by field name — the value silently resets; the attribute works but clutters the code, so it's a last resort.
- **Bad → Good:** `_maxDamage` → `_damage` with no asset changes in the diff → the diff also contains the prefabs with `_damage:` holding the old values.
- **Fix:** Re-save the affected assets and verify the values; reviewer checks the diff for the asset changes and asks for the attribute only if migrating assets isn't feasible.


### UNITY-009: Null-check references that can disappear outside our control
- **Priority:** Critical
- **Rule:** A reference whose lifetime someone else controls is checked at the point of use after an async gap or on a later frame/tick: objects another player or system can destroy, despawn or pick up (held item, target, grabbed body, NetworkObject), pooled instances, objects from another scene, shared-registry entries, anything held across `await`/`yield`. For `UnityEngine.Object` use `== null` / `TryGetComponent`, never `?.`/`??` (they skip Unity's destroyed-object check).
- **Why:** These objects legitimately go away while we still hold the reference → NRE/MissingReferenceException or acting on a stale object.
- **Bad → Good:** `await UniTask.Delay(t); _grabbedItem.Release();` → `await UniTask.Delay(t); if (_grabbedItem == null) return; _grabbedItem.Release();` — or clear the field on its despawn event.
- **Fix:** Check at the point of use (not once at acquisition), or clear the reference on the owner's despawn/destroy event. Rule of thumb: "can someone else make this vanish while we hold it?" — no → UNITY-002, yes → UNITY-009.


### ARCH-002: No business logic in MonoBehaviours
- **Priority:** Architecture
- **Rule:** MonoBehaviours wire Unity to plain C# services; domain logic lives in testable non-Unity classes.
- **Why:** Logic tied to MonoBehaviour can't be unit-tested and is coupled to the scene lifecycle.
- **Bad → Good:** Damage math inside `Enemy : MonoBehaviour.Update()` → in a `DamageService` the MonoBehaviour calls.
- **Fix:** Move the logic to a POCO service; the MonoBehaviour delegates to it.


### ARCH-003: Depend on abstractions, not concretions
- **Priority:** Architecture
- **Rule:** High-level code depends on interfaces, not concrete classes it constructs itself — including injected factories, builders and containers. An abstract base class with no shared logic should be an interface. Pure data/config classes are exempt.
- **Why:** `new`-ing concrete deps hard-wires the graph and prevents substitution/mocking.
- **Bad → Good:** `SaveRepository repo = new SaveRepository();` inside a service / `(NotificationChannelFactory f)` / `abstract class TableMapperBase` with no logic → inject `ISaveRepository` / `INotificationChannelFactory` / `interface ITableMapper`.
- **Fix:** Introduce an interface and inject it (see `DI-001`).


### ARCH-007: Reuse existing infrastructure; no copy-paste
- **Priority:** Architecture
- **Rule:** Before adding a model, registry, enum, event, spawn-point type, sync component or helper, find and extend the existing one; duplicated blocks go into one method, base class or extension.
- **Why:** Near-duplicates split behaviour; fixes land in only one copy.
- **Bad → Good:** new `SideBossType` enum + `SideBossSpawnPointRegistrar` → reuse `EnemyType` and the existing spawn points.
- **Fix:** Search first; extend the existing type, or justify the new one in the PR.


### ARCH-008: Low-level / generic components don't know their consumers
- **Priority:** Architecture
- **Rule:** A reusable or lower-level component must not reference a specific consumer, a sibling concern or a debug type; a new concern gets its own class.
- **Why:** Reverse dependencies couple independent components and make them untestable.
- **Bad → Good:** `ParticleSpawner` calling `GetComponent<IItem>()`; gameplay reading `LineArmDebugGrabSettingsModel` → the item configures the spawner; the debug tool writes a normal model gameplay already reads.
- **Fix:** Invert the dependency via an interface or condition object.


### ARCH-010: One source of truth for runtime state
- **Priority:** Architecture
- **Rule:** A runtime value lives in exactly one Model — not cached in service/system fields, not mirrored in static/global flags, not changed in a service and a model in parallel.
- **Why:** Copies drift apart.
- **Bad → Good:** static `CoopPromoSessionFlag` + a local copy of tutorial state → read `CoopPromoModel` / `TutorialModel`.
- **Fix:** Delete the copy; read the Model.


### ARCH-011: Split an interface instead of stubbing members
- **Priority:** Architecture
- **Rule:** If an implementer returns null, leaves a member empty or throws `NotImplementedException`, split the interface into role interfaces.
- **Why:** Stubs hide real capabilities, spread boilerplate and crash or silently no-op at runtime.
- **Bad → Good:** `Rigidbody GetRigidbody() => null;` / `IHapticService { Trigger(); TriggerAudioHaptic(); }` with `{ }` stubs → `IPhysicalPart` / `IHapticService` + `IAudioHapticService`.
- **Fix:** Extract the optional members into their own interface.


### CFG-007: Saving has one owner
- **Priority:** Architecture
- **Rule:** Views, presenters and ordinary systems never call the saving service; saving is triggered by a dedicated save system reacting to model events at key moments, not on every change.
- **Why:** Removing a view must not stop the game from saving; saving on every progress tick is expensive.
- **Bad → Good:** `CustomizeWindow` calls `_savingService.SaveDataForGroup(...)` → the save system reacts to the model's change event.
- **Fix:** Move the save call into the save system.

### DI-001: Inject dependencies, don't new them up or use singletons
- **Priority:** Architecture
- **Rule:** Services receive dependencies via constructor/method injection; no `new ConcreteService()` inside consumers and no static singleton access.
- **Why:** Self-constructed/singleton deps hide the graph, prevent substitution, and break tests.
- **Bad → Good:** `AudioManager.Instance.Play()` → inject `IAudioService`.
- **Fix:** Declare the dependency as an injected interface; register it in the DI container.


### DI-003: No service locator anti-pattern
- **Priority:** Architecture
- **Rule:** Don't resolve dependencies by pulling them from a global container at call time. `DiContainer` is allowed only in factories and installers; installers don't call `Container.Resolve` or pass resolved services into Bind helpers; presenters and other consumers don't `new` their collaborators — they go through a factory / `IInstantiator`.
- **Why:** It hides dependencies, defeats the point of DI, and makes wiring implicit.
- **Bad → Good:** `DiContainer` injected into a presenter / `Container.Resolve<IX>()` inside `InstallBindings` → an injected factory / let the bound class receive `IX` itself.
- **Fix:** Declare deps explicitly at construction; let the container inject them.


### DI-006: Constructors, InjectDependencies and installers only wire
- **Priority:** Architecture
- **Rule:** No IO, subscriptions, registrations or heavy work in constructors/`InjectDependencies`; setup goes to `IInitializable.Initialize`, teardown to `IDisposable.Dispose`, and neither is declared on the interface consumers depend on. `InstallBindings` only binds — no `Resolve` or logic.
- **Why:** The container resolves in unpredictable order; exposing Initialize/Cleanup on the contract invites double or missing calls.
- **Bad → Good:** `ctor { _f = f; rewardViewService.RegistryRewardViewCreator(...); }` → registration in `Initialize()`.
- **Fix:** Implement IInitializable/IDisposable; keep constructors to field assignment.


### DI-012: No static helpers with logic as hidden dependencies
- **Priority:** Architecture
- **Rule:** Views, presenters and systems don't call static classes that hold logic (e.g. `UIAnimation`); make it an injected service visible in the constructor.
- **Why:** A hidden dependency is untestable and not substitutable.
- **Bad → Good:** `UIAnimation.PlayFade(_view)` → `_uiAnimationService.PlayFade(_view)`.
- **Fix:** Wrap the helper in a service + interface; inject it.

### ROLE-007: Observable feature state lives in a Model value, not in a stateless event class
- **Priority:** Architecture
- **Rule:** State others observe (cutscene active, game finished, credits shown) is a Model value plus an `OnXChanged` event; the setter doesn't fire when the value is unchanged.
- **Why:** A system that subscribes late misses a pure event and grows workarounds.
- **Bad → Good:** `CreditsEvents { OnShown; OnHidden }` → `CreditsModel.IsActive` + `OnIsActiveChanged`.
- **Fix:** Replace the event class with a Model property.


### UI-002: Outside code talks to an MVP unit only through its Model
- **Priority:** Architecture
- **Rule:** Systems and services never call `window.GetPresenter<T>()`; they write to the Model and the Presenter reacts.
- **Why:** Presenter/View/Model is a closed unit.
- **Bad → Good:** `_loadingScreenWindow.GetPresenter<LoadingScreenPresenter>().Show()` → `_loadingScreenModel.SetState(...)`.
- **Fix:** Expose the state on a Model.


### CORO-002: Don't allocate `new WaitForSeconds` every loop iteration
- **Priority:** Performance
- **Rule:** Cache yield instructions (`WaitForSeconds`, `WaitForEndOfFrame`) outside the loop.
- **Why:** Allocating a yield object each iteration generates avoidable GC pressure.
- **Bad → Good:** `yield return new WaitForSeconds(1f);` in a `while` loop → cache `_wait = new WaitForSeconds(1f)`.
- **Fix:** Create the wait once in a field; reuse it.


### PERF-001: No per-frame allocations in Update/FixedUpdate
- **Priority:** Performance
- **Rule:** Avoid allocating (new arrays/lists, LINQ, string concat, lambdas capturing) inside per-frame methods. This includes frequently called getters: they must not return `new List<>()`/`new Container()` as a fallback or recompute an expensive lookup on every call.
- **Why:** Per-frame garbage triggers GC spikes and stutter, especially on Steam Deck and mobile.
- **Bad → Good:** `foreach (var e in list.Where(x => x.Active))` in `Update()` → pre-filtered cached list, plain `for`. `Items => _items ?? new List<Item>()` → a cached empty instance.
- **Fix:** Hoist allocations out of the loop; reuse buffers; cache empty instances and one-shot lookups; avoid LINQ on hot paths.


### PERF-002: Don't use string-based APIs in hot paths
- **Priority:** Performance
- **Rule:** Avoid `SendMessage`, `Animator.SetFloat("name", ...)`, `CompareTag` string overloads per frame; use cached hashes/ids.
- **Why:** String lookups hash every call and allocate; cached ids are far cheaper.
- **Bad → Good:** `animator.SetBool("IsRunning", v)` each frame → cache `Animator.StringToHash("IsRunning")` once.
- **Fix:** Cache the int id/hash in a static readonly field and use the int overload.


### PERF-003: Prefer allocation-free physics queries on repeated paths
- **Priority:** Performance
- **Rule:** On any path that runs more than once (per-frame, per-tick, per-attack, per-hit), use the buffer-based `NonAlloc` physics query overloads (`RaycastNonAlloc`, `SphereCastNonAlloc`, `OverlapSphereNonAlloc`, `OverlapBoxNonAlloc`, …) with a buffer allocated once and reused; do not use the array-returning variants there.
- **Why:** Array-returning queries allocate a fresh `Collider[]`/`RaycastHit[]` every call → GC spikes and stutter, especially on Steam Deck.
- **Bad → Good:** `Collider[] hits = Physics.OverlapSphere(pos, r, mask);` in an attack/sensor path → `int n = Physics.OverlapSphereNonAlloc(pos, r, _buffer, mask);` iterate `0..n`.
- **Fix:** Allocate the results buffer once in a field; call the `NonAlloc` overload and iterate the returned count.


### PERF-004: Flag array-returning physics queries (RaycastAll / OverlapSphere / *All)
- **Priority:** Performance
- **Rule:** Treat `Physics.RaycastAll`, `SphereCastAll`, `BoxCastAll`, `CapsuleCastAll`, and `OverlapSphere`/`OverlapBox`/`OverlapCapsule` (the array-returning overloads — result assigned to `Collider[]` / `RaycastHit[]`) as an allocation. Allowed only on genuinely one-shot cold paths (editor tools, one-time init); forbidden on repeated paths (see PERF-003).
- **Why:** These variants always allocate a new array — the tell is the `Collider[]`/`RaycastHit[]` return assignment; the assistant should flag by method name in the diff.
- **Bad → Good:** `RaycastHit[] hits = Physics.RaycastAll(origin, dir, dist, mask);` per frame → `RaycastNonAlloc` into a reused buffer; or if the path is provably one-shot, add a comment noting why the allocation is acceptable.
- **Fix:** Switch to the NonAlloc overload with a reused buffer, or confirm and document the cold-path exemption. Editor-only code (`/Editor/`) is exempt.


### PERF-005: No reflection or type-name scans on event / RPC / tick paths
- **Priority:** Performance
- **Rule:** No `PropertyInfo.GetValue` or `GetType().ToString() ==` loops in handlers that fire often; resolve once into a dictionary/delegate, or use codegen.
- **Why:** Reflection allocates and gets slower as the number of models grows.
- **Bad → Good:** `foreach (s) if (s.GetType().ToString() == name)` in an RPC → a `Dictionary<Type, T>` lookup built once.
- **Fix:** Cache or generate.


### PERF-006: Pool frequently spawned objects; keep pool churn cheap
- **Priority:** Performance
- **Rule:** Repeatedly spawned things (decals, particles, destructibles, audio sources) go through a pool; get/return toggles the component rather than `SetActive`/`SetParent` when that suffices.
- **Why:** Instantiate/Destroy and hierarchy changes cause GC and frame spikes, especially on mobile.
- **Bad → Good:** `Instantiate(decal)`; on return `SetActive(false); SetParent(pool)` → pooled instance; `audioSource.enabled = false` on return.
- **Fix:** Route the spawn through the pool.

### UNITY-001: Prefer serialized/injected refs; never GetComponent per frame
- **Priority:** Performance
- **Rule:** Prefer `[SerializeField]` or injection over `GetComponent*`/`FindObjectOfType` even in `Awake`; if a lookup is unavoidable, resolve it once in `Awake`/`Start`, never per frame. `GetComponentsInChildren` on a repeated path (e.g. a cache miss) is cached in a model/registry. Avoid `[RequireComponent]` — it adds components implicitly.
- **Why:** These calls are expensive and allocate; per-frame lookups spike CPU; implicit components surprise designers.
- **Bad → Good:** `GetComponent<Rigidbody>()` in `Update()` / in `Awake()` → `[SerializeField] private Rigidbody _rigidbody;`.
- **Fix:** Serialize or inject the reference; otherwise cache it once during init.


### ADDR-004: Load through the project asset loader; no Resources.Load or hand-rolled config loading
- **Priority:** Best Practices
- **Rule:** Assets load via the project loader / Addressables; configs are bound with `Container.BindConfigurationFromAddressables<T>(Address…)`.
- **Why:** One testable, releasable loading path; no drifting string paths.
- **Bad → Good:** `Resources.Load<Cfg>(PATH)` / `loaderService.LoadAsset<Cfg>(…)` in an installer → `Container.BindConfigurationFromAddressables<Cfg>(Address.Configurations.Cfg_Default).AsSingle()`.
- **Fix:** Switch to the project binding / loader.

### ARCH-009: The owning method handles its own side effects
- **Priority:** Best Practices
- **Rule:** Required extra handling lives inside the callee (optionally behind a parameter); don't widen a shared signature with arguments only one implementation needs.
- **Why:** Call-site workarounds are forgotten by the next caller.
- **Bad → Good:** `SetLinearVelocity(0); ChangePosition(p); SetLinearVelocity(0);` → `ChangePosition` resets the rigidbody itself.
- **Fix:** Move the handling into the owner.


### ARCH-012: abstract only for truly mandatory members; no empty overrides
- **Priority:** Best Practices
- **Rule:** A base member is `abstract` only if every subclass must implement it; otherwise it is `virtual` with an empty/default body. No empty overrides or "just in case" hooks.
- **Why:** A forced empty override is a fake contract.
- **Bad → Good:** `protected override void OnViewSet() { }` → remove the override; the base member becomes `virtual`.
- **Fix:** abstract → virtual; delete empty overrides.


### ARCH-013: Narrow types in APIs; no lookups by object/animation name
- **Priority:** Best Practices
- **Rule:** Don't expose `GameObject` as an API type or find children/animations by name string; use the specific component or a serialized reference.
- **Why:** A rename in the editor silently breaks the code.
- **Bad → Good:** `transform.Find("Particles")` / `GameObject Target` → `[SerializeField] private ParticleSystem _particles` / `Renderer Target`.
- **Fix:** Serialize the typed reference.

### BUILD-004: Guard platform-specific APIs behind platform defines
- **Priority:** Best Practices
- **Rule:** Platform-only APIs (Steamworks, standalone-only paths, editor-only paths) are wrapped in the matching `#if` (`UNITY_STANDALONE`, `UNITY_EDITOR`, …).
- **Why:** Unguarded platform calls break builds for other targets.
- **Fix:** Add the platform define guard around the API usage.


### CORO-004: UniTask only; async void only for event handlers
- **Priority:** Best Practices
- **Rule:** Never use `Task`; awaitable work returns `UniTask`/`UniTask<T>`; fire-and-forget is `UniTaskVoid`; `async void` only for event handlers or overrides of void virtuals.
- **Why:** `async void` swallows exceptions and can't be awaited or cancelled; one async stack across the project.
- **Bad → Good:** `public async void CommitAsync()` / `async Task LoadAsync()` → `public async UniTask CommitAsync()` / `async UniTask LoadAsync()` (method naming per NAME-003 / repo override).
- **Fix:** Change the return type.

### DI-004: Zenject injection method is always named InjectDependencies
- **Priority:** Best Practices
- **Rule:** MonoBehaviour/NetworkBehaviour use `[Inject]` method injection named exactly `InjectDependencies`; plain C# classes use constructor injection. `InjectDependencies` only assigns fields — no subscriptions or logic (see DI-006).
- **Why:** A single consistent name makes injection points greppable and unambiguous.
- **Bad → Good:** `[Inject] void Construct(...)` → `[Inject] public void InjectDependencies(...)`.
- **Fix:** Rename to InjectDependencies (or move to a constructor for POCOs).


### DI-005: Don't null-guard injected dependencies or session invariants
- **Priority:** Best Practices
- **Rule:** Do not write defensive null checks for injected dependencies or session invariants (`NetworkRunner`, `LocalPlayer`) — neither silent (`?.`, `if (x == null) return;`) nor throwing (`x ?? throw new ArgumentNullException(...)`); let a broken binding throw on use. The same goes for "impossible state" checks that the game flow already guarantees.
- **Why:** Guards hide broken wiring/lifecycle and mask the real error far from its cause.
- **Bad → Good:** `if (_mp?.NetworkRunner?.LocalPlayer == null) return;` / `if (!(_stateMachine.Current is LobbyGameState)) return;` in the lobby bootstrap → use it directly.
- **Fix:** Remove the guard; fix the binding if it's actually missing. See UNITY-002 / UNITY-009 for non-injected references.


### DI-007: No optional dependencies — bind a Mock
- **Priority:** Best Practices
- **Rule:** No `[InjectOptional]`, default-null constructor parameters or `context as IOptionalContext` casts; platforms without the feature bind a Mock (e.g. `IsAvailable => false`).
- **Why:** Optional dependencies spread null checks everywhere.
- **Bad → Good:** `[InjectOptional] ISteamLanguageProvider provider = null` → always injected; `MockSteamLanguageProvider` bound on non-Steam platforms.
- **Fix:** Add a Mock + installer; delete the null checks.


### ERR-002: Operations with several failure reasons return a Result
- **Priority:** Best Practices
- **Rule:** A domain operation that can fail for different reasons returns a result object with a status enum (and extra data), not `bool`.
- **Why:** `bool` hides the reason and callers branch wrongly.
- **Bad → Good:** `bool TryAddBooster()` → `BoosterAddResult { Status, … }`.
- **Fix:** Introduce a result type.


### LOG-001: No leftover debug logs; real failures are errors, expected states aren't
- **Priority:** Best Practices
- **Rule:** Delete ad-hoc `Debug.Log`/`LogWarning` traces before merge; diagnostics that must stay go through the project Logger/toggleable debug system. A real failure or missing config is `LogError`/exception with a message, never `LogWarning`; an expected state is not logged as an error.
- **Why:** Trace spam hides real problems; warnings get lost; false errors train people to ignore the console.
- **Bad → Good:** `Debug.LogWarning($"[LoadingScreen] Show({type})")` → removed; `LogWarning("preset not configured")` → exception / `Assert` with a message in `Awake`.
- **Fix:** Delete the log, or raise it to an error/exception.

### NAME-005: Names match behaviour
- **Priority:** Best Practices
- **Rule:** `Get*`/`Is*`/`Calculate*` return a value without mutating state; methods that create new instances are `Create*`; `Send*Event` only sends; booleans are positive `Is`/`Has`/`Can`; events are `OnPascalCase` ending with what happened (`OnXChanged`, `OnXShown`).
- **Why:** Hidden side effects and negated names invert logic at call sites.
- **Bad → Good:** `void GetAllCsv()` that writes files / `NotAvailable()` / `GetSlot()` that creates / `OnChangeHealth` → `WriteAllCsv()` / `IsAvailable()` / `CreateSlot()` / `OnHealthChanged`.
- **Fix:** Split out the side effect or rename.


### PR-002: No dead code, unused members or test scaffolding
- **Priority:** Best Practices
- **Rule:** Before review, delete commented-out code, unused fields/parameters/usings/injections/classes, empty Unity callbacks (`void Update() { }`), tool-generated empty installers, test fields and hardcoded test data. An empty interface implementation signals the wrong abstraction (ARCH-011).
- **Why:** Dead code misleads about dependencies; git keeps the history.
- **Bad → Good:** `//_baseTutorialService.ActivateFirstStep();`, an injected model that is never read, `void Update() { }` → removed (or the commented-out call restored).
- **Fix:** Grep the diff for `//` code lines, unused members and `Debug.Log` before pushing.


### STYLE-002: Enums assign explicit values and start with None = 0
- **Priority:** Best Practices
- **Rule:** Every enum assigns explicit integer values and declares `None = 0` first; `[Flags]` enums use bit-shift values. Existing values are never renumbered — only append new ones.
- **Why:** Serialized/networked enums are stored as ints in prefabs and saves; renumbering silently changes data. A zero-value None gives a safe default.
- **Bad → Good:** `enum State { Alive, Dead }` → `enum State { None = 0, Alive = 1, Dead = 2 }`.
- **Fix:** Add None = 0 and explicit values; never change an existing value.


### STYLE-007: No magic numbers or strings — class-level named constants
- **Priority:** Best Practices
- **Rule:** Meaningful literals (thresholds, counts, IDs, sentinels, log/exception messages, paths, GameObject/animator names) become class-level `private const`; a value shared across files lives in one constants class. Obvious `0`/`1`, `/2f`, `1f -` are exempt; use `nameof(_field)` for property names and named arguments for unclear parameters. Designer-tunable values go to config instead (CFG-001).
- **Why:** Unnamed literals hide intent and drift when copied.
- **Bad → Good:** `if (activeParticleCount < 2)` / `return -1;` → `MIN_ROPE_PARTICLES` / `NOT_FOUND_ID`.
- **Fix:** Extract an UPPER_SNAKE const.


### STYLE-011: Narrowest mutability and visibility
- **Priority:** Best Practices
- **Rule:** Constructor-assigned fields are `readonly`; internally-written properties use `private set`; interfaces expose `{ get; }` only; collections are exposed read-only; members used only inside the class are `private`.
- **Why:** Wider-than-needed mutability lets any caller change owned state.
- **Bad → Good:** `private DiContainer _diContainer;` / `public bool IsRun = false;` → `private readonly DiContainer _diContainer;` / `public bool IsRun { get; private set; }`.
- **Fix:** Add readonly / private set; drop setters from interfaces.


### STYLE-016: Return empty collections, not null
- **Priority:** Best Practices
- **Rule:** Collection-returning members (including Null-Object/Mock classes) return an empty (cached) collection, never null.
- **Why:** Null collections force every client to null-check.
- **Bad → Good:** `GetTagFilter() => null;` → `=> Array.Empty<Tag>();`
- **Fix:** Return a cached empty instance.


### STYLE-018: Compare computed floats with a tolerance
- **Priority:** Best Practices
- **Rule:** Use `Mathf.Approximately` (or an epsilon) instead of `==`/`!=` on computed floats.
- **Why:** Float rounding makes exact comparison fail unpredictably.
- **Bad → Good:** `totalWeight != TOTAL_WEIGHT` → `!Mathf.Approximately(totalWeight, TOTAL_WEIGHT)`.
- **Fix:** Use a tolerance.

### UNITY-003: Don't use public fields for inspector data — use [SerializeField] private
- **Priority:** Best Practices
- **Rule:** Expose inspector-editable data on MonoBehaviours and configurations via `[SerializeField] private` plus a read-only getter (`IReadOnlyList<T>` for collections), not `public` fields or `[field: SerializeField]` with a public `List`. Exception: behaviour-free `[Serializable]` data classes (DTOs) may use `public` fields.
- **Why:** `public` breaks encapsulation and lets any code mutate state the component owns.
- **Bad → Good:** `public float speed;` → `[SerializeField] private float _speed; public float Speed => _speed;`.
- **Fix:** Make the field private with `[SerializeField]`; expose a read-only property if needed.


### UNITY-004: No heavy work in OnValidate / constructors
- **Priority:** Best Practices
- **Rule:** Keep `OnValidate` and MonoBehaviour constructors free of allocations, IO, or scene queries.
- **Why:** They run in the editor at unexpected times; heavy work freezes the editor or throws.
- **Fix:** Move work to `Awake`/`Start` or an explicit init method.


### UNITY-006: Only inspector-configured data is [SerializeField]
- **Priority:** Best Practices
- **Rule:** Fields assigned at runtime and technical constants (epsilons) are not serialized; when data moves to another source, delete the old serialized field.
- **Why:** An empty inspector slot looks like forgotten wiring; two sources of truth confuse designers.
- **Bad → Good:** `[SerializeField] private float _minTeleportDeltaSqr = 0.0001f` → `private const float MIN_TELEPORT_DELTA_SQR = 0.0001f`.
- **Fix:** Drop the attribute or the field.


### NAME-001: Private fields use `_camelCase`
- **Priority:** Style
- **Rule:** Private/serialized fields are `_camelCase`; public members are `PascalCase`.
- **Why:** Consistent naming makes scope obvious at a glance and matches the team style.
- **Bad → Good:** `private float Speed;` → `private float _speed;`.
- **Fix:** Rename to the convention (IDE rename to keep references).


### NAME-002: No abbreviations, single-letter or vague names
- **Priority:** Style
- **Rule:** Use descriptive names; avoid `mgr`, `tmp`, `Calc`, `x` (except `i`/`j` loop indices) and vague generic names (`_settings`, `Usable`, `Data`) that don't say what the thing is or does.
- **Why:** Cryptic names raise the reading cost for everyone, especially new team members.
- **Bad → Good:** `EnemyManager em = new EnemyMgr();` / `_settings` → `EnemyManager enemyManager = new EnemyManager();` / `_loadingScreenSettings`.
- **Fix:** Rename to a full, intention-revealing name.


### NAME-003: Async methods end with `Async`
- **Priority:** Style
- **Rule:** Methods returning `Task`/`UniTask` are suffixed `Async`.
- **Why:** The suffix signals awaitable semantics and prevents accidental fire-and-forget.
- **Fix:** Rename `Load()` returning a task → `LoadAsync()`.


### NAME-004: Constants are UPPER_SNAKE_CASE
- **Priority:** Style
- **Rule:** `const` fields are UPPER_SNAKE_CASE; `static readonly` fields are `_camelCase`.
- **Why:** Separates compile-time constants from state at a glance.
- **Bad → Good:** `const int IterationCycleStop = 200;` → `const int ITERATION_CYCLE_STOP = 200;`
- **Fix:** Rename.


### NAME-006: Type suffix states its role; injected params are named after their type
- **Priority:** Style
- **Rule:** Coordinators are `*System`, state holders `*Model`, creators `*Factory`, NetworkBehaviour bases `Network*`; injected concrete dependencies are named after their type in lowerCamelCase — no shortened aliases.
- **Why:** The role contract (ROLE-*) is readable from the name.
- **Bad → Good:** `BlocksMinedCounter` (subscribes) / `(PlayerFollowState follow)` → `BlocksMinedCounterSystem` / `(PlayerFollowState playerFollowState)`.
- **Fix:** Rename.


### NAME-007: IEnumerator coroutine methods end with "Coroutine"
- **Priority:** Style
- **Rule:** Methods returning `IEnumerator` for `StartCoroutine` are suffixed `Coroutine`.
- **Why:** Makes it obvious the method must be started, not called.
- **Bad → Good:** `TrajectoryMovement()` → `TrajectoryMovementCoroutine()`.
- **Fix:** Rename.

### PR-004: Code you touch meets the current rules (boy-scout)
- **Priority:** Style
- **Rule:** Methods/lines the PR touches are brought to the current rules even if they are legacy written by someone else; untouched code is not reformatted (PR-001).
- **Why:** Legacy converges to the standard without big-bang reformatting.
- **Fix:** Tidy the touched method only.


### STYLE-001: Explicit types, never var
- **Priority:** Style
- **Rule:** Use explicit types for locals, `foreach` variables, and `out` targets — never `var`.
- **Why:** Team standard; keeps types visible at the read site without IDE hover.
- **Bad → Good:** `var pool = config.Pool;` → `List<StoreCardData> pool = config.Pool;`.
- **Fix:** Replace var with the concrete type.


### STYLE-003: Comments only for non-obvious "why"
- **Priority:** Style
- **Rule:** Default to no comments; add one only to explain a hidden constraint/invariant/workaround, placed above the member (not trailing the line). Never describe what the code does. Explicitly flag: commented-out code, stale `TODO`/`FIXME`/`NOT USED??` markers, AI-style `/// <summary>` blocks and step-by-step narration. `/// <summary>` is allowed only on public API of shared modules when the contract isn't clear from the name. An agreed tech-debt TODO with a ticket may stay.
- **Why:** What-comments duplicate the code and rot; names should carry intent.
- **Bad → Good:** `// InjectionSystem.Inject(controller);` / `/// <summary>Gets the value.</summary>` → delete; if the code needs explaining, extract a named method.
- **Fix:** Delete descriptive comments and dead code; keep only why-notes.


### STYLE-004: Early return over nested guards; max nesting depth 2
- **Priority:** Style
- **Rule:** Prefer early returns (and `continue` in loops); use `TryGetComponent`/`TryGetValue` over Get-then-null-check. Nesting deeper than 2 levels is a finding.
- **Why:** Reduces nesting and makes the happy path obvious.
- **Bad → Good:** nested `if (TryGetComponent... != null) {...}` → `if (!TryGetComponent(out NetworkObject o)) return;`; `foreach { if (a) { if (b) {…} } }` → `foreach { if (!a) continue; Apply(x); }`.
- **Fix:** Flip conditions to guard-and-return/continue, or extract a method.


### STYLE-005: Single-statement members use expression bodies
- **Priority:** Style
- **Rule:** Methods, constructors, properties, `InjectDependencies` and Unity callbacks with a single-statement body use `=>`, with the expression on the next indented line.
- **Why:** Team convention flagged on almost every PR; removes brace noise and keeps signatures short.
- **Bad → Good:** `private void Awake() { Initialize(); }` → `private void Awake() =>\n    Initialize();`
- **Fix:** Rider "To expression body"; keep a block body when it contains a nested `if`.


### STYLE-006: Single-statement if/else/for bodies omit braces and go on the next line
- **Priority:** Style
- **Rule:** A single-statement body has no braces and sits on its own indented line, never on the condition line; opening braces stay on the same line (K&R).
- **Why:** Project style; same-line bodies hide control flow in review.
- **Bad → Good:** `if (x) { return; }` / `if (x) return;` → `if (x)\n    return;`
- **Fix:** Reformat with the team Rider profile.


### STYLE-008: One top-level type per file
- **Priority:** Style
- **Rule:** Every class, struct, enum, interface and exception lives in its own `<TypeName>.cs`; no private/nested helper types. Exception: small schema classes nested inside a single configuration file.
- **Why:** Types are found by file name; contract edits don't touch the implementation file.
- **Bad → Good:** `CoinsModel.cs` with `ICoinsModel` + `CoinsModel` → `ICoinsModel.cs` and `CoinsModel.cs`.
- **Fix:** Split into files.


### STYLE-009: Every type is in a namespace that matches its folder
- **Priority:** Style
- **Rule:** No types in the global namespace; format `Features.<Module>.Scripts…` / `Global.<Module>.Scripts…` (or the package's `RSG.Muffin.<Module>`).
- **Why:** Global types collide and hide module ownership.
- **Bad → Good:** top-level `public class AutoColumnWidth` → wrapped in the folder's namespace.
- **Fix:** Wrap the type; disable redundant folder segments in Rider.


### STYLE-010: Fixed member order
- **Priority:** Style
- **Rule:** const → static readonly → `[SerializeField]` → readonly injected → mutable state → events/properties → constructor/`InjectDependencies` → lifecycle (Initialize and Dispose adjacent) → public → protected → private → RPCs → event handlers.
- **Why:** Predictable layout; state visible at the top.
- **Bad → Good:** a field between methods, Dispose at the end of the file → field in the top block, Dispose next to Initialize.
- **Fix:** Reorder (Rider File Layout).


### STYLE-012: Access modifiers are always explicit, including interface members
- **Priority:** Style
- **Rule:** Never rely on implicit `private`; interface members declare `public` explicitly and consistently.
- **Why:** Project convention; mixed modifiers look accidental.
- **Bad → Good:** `void ResetTimer();` next to `public void ActivateTimer();` → both `public`.
- **Fix:** Add the modifier.


### STYLE-013: Name complex conditions, callbacks and method steps
- **Priority:** Style
- **Rule:** Multi-clause conditions become a named `bool` or `IsX()` method; long methods are split into named steps (initialization separate from logic); FSM predicates, local functions and non-trivial lambdas become named private methods; never subscribe with a lambda.
- **Why:** Reviewers repeatedly couldn't tell what a condition or method does.
- **Bad → Good:** `AddTransition(A, B, _ => timer <= 0f && ...)` → `_ => IsSlowExpired()`; a 60-line `Attack()` → `EvaluateAttackPosition(); ApplyDash();`.
- **Fix:** Extract a named bool/method.


### STYLE-014: Inline trivial single-use locals
- **Priority:** Style
- **Rule:** A local assigned once and immediately returned or used once is inlined, unless it names a complex expression (STYLE-013).
- **Why:** Throwaway temps add lines without meaning.
- **Bad → Good:** `List<T> data = reader.GetRecords<T>(); return data;` → `return reader.GetRecords<T>();`
- **Fix:** Inline.


### STYLE-015: Try-pattern for "may fail" lookups
- **Priority:** Style
- **Rule:** A method returning `bool` + an `out` result is named `TryX` and assigns out defaults as its first statements; don't return `(bool, T)` tuples or nullable for this.
- **Why:** The contract is visible from the name; the failure value is set up front.
- **Bad → Good:** `(bool, int) IsShipUpgraded(int)` → `bool TryGetRequiredShipLevel(int id, out int level) { level = 0; ... }`
- **Fix:** Rename and restructure.


### STYLE-017: No #region
- **Priority:** Style
- **Rule:** Don't use `#region`/`#endregion`.
- **Why:** Team agreement; regions hide oversized classes instead of splitting them.
- **Fix:** Remove the region; split the class if it's too big.
