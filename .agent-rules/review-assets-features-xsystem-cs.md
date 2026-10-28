<!-- globs: Assets/Features/**/*System.cs; rules: 3; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/Features/**/*System.cs`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### NET-001: Guard authoritative mutations with the master-client check
- **Priority:** Critical
- **Rule:** State-changing logic on shared/networked state must early-return unless `NetworkRunner.IsSharedModeMasterClient`.
- **Why:** Non-authoritative writes diverge peers and get overwritten — silent desync.
- **Bad → Good:** `_currencyModel.SetCurrency(x)` unguarded → `if (!_mp.NetworkRunner.IsSharedModeMasterClient) return;` first.
- **Fix:** Add the authority guard at the top of the handler, then call Synchronize().


### ARCH-004: System must never depend on another System
- **Priority:** Architecture
- **Rule:** A *System must not take, hold, or call another *System; allowed deps are Models, Services, thin Core interfaces, and event buses.
- **Why:** System-to-system references create asmdef cycles and hidden coupling across features.
- **Bad → Good:** `GameDisconnectHandlerSystem(SessionEndAnalyticsSystem x)` → inject `ISessionEndAnalyticsService` from Core.
- **Fix:** Extract a Core interface / shared event and depend on that.


### ROLE-003: Systems coordinate — no domain logic, no shared state
- **Priority:** Architecture
- **Rule:** A *System subscribes/unsubscribes and delegates to models and services; it contains no business calculations inline. It may keep private runtime bookkeeping that no other class reads (a timer, a "subscribed" flag); anything read by others, observed, or synchronized lives in a Model.
- **Why:** Logic inside systems can't be reused or tested and duplicates across handlers; shared state belongs in models.
- **Bad → Good:** damage formula computed inside `OnHit` handler → call `_damageService.Compute(...)`, store the result in a model.
- **Fix:** Extract calculations into a Service; move shared state to a Model; keep only wiring and private bookkeeping in the System.
