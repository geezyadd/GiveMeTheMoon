<!-- globs: Assets/Features/**; rules: 16; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/Features/**`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### NET-002: Use delta (CustomSynchronize) for concurrently-written state
- **Priority:** Critical
- **Rule:** Any collection, or any value more than one client can write concurrently, must sync via `CustomSynchronize()` with an operation command — not full `Synchronize()`. Conversely, a value with a single writer needs no custom data: plain `Synchronize()` is enough.
- **Why:** Full-state sync races — simultaneous senders overwrite each other and lose data; custom deltas for single-writer state are needless complexity.
- **Bad → Good:** two clients calling `Synchronize()` on a shared list → `Data1 = new AddItemData{...}; CustomSynchronize();`. Host-only value with custom delta data → plain JSON `Synchronize()`.
- **Fix:** Model the change as a delta command in TData; apply it in OnSetNewCustomValues (or drop the delta when there is one writer).


### NET-003: Never call Synchronize() from inside SetNewValues
- **Priority:** Critical
- **Rule:** `SetNewValues` / `OnSetNewCustomValues` must only copy fields and fire events — never call `Synchronize()` / `CustomSynchronize()`.
- **Why:** Re-synchronizing while applying an incoming snapshot causes an infinite sync loop.
- **Fix:** Copy from src with sync:false; move any send logic to the caller.


### NET-005: Sync models must be [Serializable] with [field: SerializeField] auto-props
- **Priority:** Critical
- **Rule:** A synchronized model class is `[Serializable]` and every synced auto-property uses `[field: SerializeField]`.
- **Why:** Without it Unity/JSON won't serialize the backing field and the value never crosses the wire.
- **Bad → Good:** `public int Score { get; set; }` on a sync model → `[field: SerializeField] public int Score { get; private set; }`.
- **Fix:** Add [Serializable] on the class and [field: SerializeField] on each synced property.


### NET-008: Persistent state from StateAuthority is [Networked], not an RPC
- **Priority:** Critical
- **Rule:** Anything late joiners/reconnecting players must see is a `[Networked]` property; RPCs are only for one-shot effects everyone must see; no RPC when the caller already has authority.
- **Why:** RPC data is lost on late join, reconnect and host migration.
- **Bad → Good:** `NotifySpectatorCrystalDestination_RPC(Vector3)` → `[Networked] Vector3 CrystalDestination { get; set; }`.
- **Fix:** Replace the state-carrying RPC with a networked property.


### UNITY-007: Pooled / respawned objects reset what they applied
- **Priority:** Critical
- **Rule:** Modifiers, tweens and state accumulated on spawn are undone in a single `CleanUp()` on despawn/re-init; DOTween tweens are killed on destroy/despawn.
- **Why:** Pooled enemies otherwise stack modifiers on each respawn; tweens act on dead objects.
- **Bad → Good:** `AddModifier(MaxHealth…)` on every spawn → the matching `RemoveModifier` in `CleanUp()`.
- **Fix:** Pair every Add with a Remove.


### UNITY-010: Gameplay uses scaled time; only the pause service touches timeScale
- **Priority:** Critical
- **Rule:** Gameplay motion uses `deltaTime` (not `unscaledDeltaTime`); nobody writes `Time.timeScale` directly — pause/resume goes through the pause service.
- **Why:** Otherwise things move during pause or the pause state goes out of sync.
- **Bad → Good:** `Time.timeScale = 1f;` in `GameOverUISystem` → `_gameTimeService.ResumeGame()`.
- **Fix:** Use the service.

### ARCH-001: Features don't reference each other directly
- **Priority:** Architecture
- **Rule:** A feature must not `using` or call another feature's internal types directly; go through a shared contract/interface.
- **Why:** Direct cross-feature coupling turns modules into a monolith and blocks isolated changes/tests.
- **Bad → Good:** `EnemyFeature` calling `PlayerFeature.PlayerController.Hp` → depend on an `IHealth` contract in a shared module.
- **Fix:** Extract the needed surface into a shared interface; inject it (see `DI-*`).


### ARCH-005: Keep the feature layer graph acyclic (Installers→Systems→Core→Data)
- **Priority:** Architecture
- **Rule:** Each `.cs` lives in its role folder (Data/Core/Systems/Installers); dependencies flow one way — Data depends on nothing feature-internal. Models live in `Data/` with their **own asmdef**, so other modules reference only that Data asmdef; `Data/` never contains services or service interfaces.
- **Why:** Layer inversions and cross-feature cycles fail compilation and rot the module boundary; a separate Data asmdef keeps consumers from pulling the whole module.
- **Bad → Good:** a module referencing `BootstrapModule.asmdef` just to read a model / a Service inside `Data/` → reference `<Module>.Data.asmdef`; move the service to Core/Systems.
- **Fix:** Move the type to its role folder; put shared contracts in Core, state in Data (own asmdef).


### NET-006: Pick the correct RPCType direction
- **Priority:** Architecture
- **Rule:** JSON sync models declare an `RPCType` matching who may write (FromStateAuthorityToAll / FromInputAuthorityToStateAuthority / InAllWays).
- **Why:** Wrong direction lets clients write state they shouldn't, or blocks legitimate updates.
- **Fix:** Set RPCType to the narrowest direction the feature actually needs.


### ADDR-003: Don't load Addressables synchronously on the main thread in gameplay
- **Priority:** Performance
- **Rule:** Avoid `.WaitForCompletion()` during active gameplay; load async and await.
- **Why:** Synchronous completion blocks the main thread and causes a visible hitch.
- **Fix:** Use the async flow with a loading state; preload where possible.


### NET-009: Don't hand-sync transforms every tick
- **Priority:** Performance
- **Rule:** Use `NetworkTransform` / `QuantizedNetworkTransform` instead of writing a `[Networked]` position/yaw each tick and applying it in `Render`.
- **Why:** Rewriting networked state every tick defeats Fusion's network optimisation.
- **Bad → Good:** `transform.position = NetworkedPosition` in Render → a `QuantizedNetworkTransform` component.
- **Fix:** Use the existing sync component.

### BUILD-003: No debug-only / test scaffolding in shipped runtime paths
- **Priority:** Best Practices
- **Rule:** Editor/debug-only helpers (gizmo drawers, test hooks) live in an Editor/debug asmdef or are guarded by `#if UNITY_EDITOR` / `#if DEVELOPMENT_BUILD`. Temporary `Debug.Log` probes are **deleted** before merge, not fenced with `#if` (see LOG-001).
- **Why:** Debug scaffolding bloats builds and can leak internal state or cost performance in production.
- **Fix:** Move gizmo/debug code to an editor/debug-only assembly; delete probe logs.


### CORO-003: Prefer async/await + CancellationToken for new async flows
- **Priority:** Best Practices
- **Rule:** New non-frame async work should use async/await with a `CancellationToken`, not chained coroutines.
- **Why:** Tokens give clear cancellation and composition; long coroutine chains leak and are hard to cancel.
- **Fix:** Use the team's async util with a token tied to the object lifetime.


### NET-004: Sync models fire their own change events
- **Priority:** Best Practices
- **Rule:** Fire change events inside `SetNewValues` / `OnSetNewCustomValues`; the sync base class does not fire them.
- **Why:** Otherwise remote updates apply silently and UI/systems never react to networked changes.
- **Fix:** Invoke the model's OnXChanged event after copying values.


### NET-007: Register sync models needing reset as ISessionCleanup
- **Priority:** Best Practices
- **Rule:** A model carrying session state that must reset between sessions implements `ISessionCleanup` and is bound via `BindInterfacesAndSelfTo` in `DataInstaller`.
- **Why:** Stale networked state leaks across sessions and host migration.
- **Fix:** Implement Cleanup() and register the model with interface binding.


### UNITY-008: Platform/device checks go through the device service
- **Priority:** Best Practices
- **Rule:** Feature code doesn't read `Application.isEditor`, `Application.isMobilePlatform`, `Debug.isDebugBuild` or the raw device type — it asks `IDeviceService` / `DeviceTypeModel` (add a named method if needed).
- **Why:** Mock/editor device switching only works if every check goes through one place.
- **Bad → Good:** `if (Application.isEditor || Debug.isDebugBuild)` → `if (_deviceService.IsDebugEnvironment)`.
- **Fix:** Route the check through the service.
