<!-- globs: Assets/Features/AIModuleStateMachine/**; rules: 5; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/Features/AIModuleStateMachine/**`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### AI-005: Guard forced / FromAny transitions into Fear
- **Priority:** Critical
- **Rule:** Forced from-any transitions into Fear exclude states that must not be interrupted (attack, attached, scream); every enemy handles Fear explicitly.
- **Why:** A forced transition breaks mid-attack invariants.
- **Bad → Good:** `AddTriggerTransitionFromAny(OnFear, Fear, forceInstantly: true)` → per-source transitions from the allowed states.
- **Fix:** List the allowed source states.


### AI-001: Enemy logic lives in Systems, not in the enemy class or state bodies
- **Priority:** Architecture
- **Rule:** The enemy class and its states build the FSM and enable/disable systems; damage handling, timers, attraction and look-at live in per-enemy Systems; states rely on the systems' own `Clear`.
- **Why:** Keeps enemies thin and the logic reusable across enemies.
- **Bad → Good:** `UpdateScreamAttraction()` / `OnDamagedHandler` inside `ParrotMobEnemy` → a ScreamAttraction System / DamageProcess System enabled from states.
- **Fix:** Move the logic into a System.


### AI-004: AnimatorPresenter is the only animation owner, and only animates
- **Priority:** Architecture
- **Rule:** States and contexts never touch Animator hashes/triggers; AnimatorPresenters never rotate, move or look-at.
- **Why:** Single responsibility.
- **Bad → Good:** `Animator.StringToHash("JumpStart")` in a state; `LookAtPosition` in an AnimatorPresenter → a VisualState the presenter maps to animation; a look-at System.
- **Fix:** Move animation calls to the presenter and rotation to a System.


### AI-006: Enemy data lives in the Context; the Context doesn't pass dependencies through
- **Priority:** Architecture
- **Rule:** The enemy class holds only the FSM and the VisualState that mirrors it; other data lives on the Context; the Context doesn't re-expose dependencies consumers can inject themselves.
- **Why:** Keeps the enemy thin and stops the Context acting as a service locator.
- **Bad → Good:** `public SpawnedPlayersModel SpawnedPlayers => _spawnedPlayers;` on the Context → inject `SpawnedPlayersModel` directly.
- **Fix:** Move data to the Context; replace pass-through getters with injection.

### AI-003: All FSM transitions are declared in BuildFsm
- **Priority:** Best Practices
- **Rule:** States fire trigger events and never call `RequestStateChange`; branching choices use a ghost state.
- **Why:** One place to read the whole state graph.
- **Bad → Good:** `_enemy.RequestStateChange(Attached)` in ChaseState → `TriggerEvent(OnChaseTimeout)` + `AddTriggerTransition` in BuildFsm.
- **Fix:** Replace the in-state change with a trigger.
