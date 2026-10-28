<!-- globs: Assets/Features/AIModuleStateMachine/**/States/**; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/Features/AIModuleStateMachine/**/States/**`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### AI-002: Per-state parameters are applied by a Setup System, not in OnEnter
- **Priority:** Best Practices
- **Rule:** Per-state speed, stoppingDistance and radii are applied by the enemy's Setup System based on the active state.
- **Why:** One convention across enemies; states stay declarative.
- **Bad → Good:** `SetMoveSpeed(...)`; `stoppingDistance = 0f` in `PatrolState.OnEnter` → the Setup System applies the Patrol parameters.
- **Fix:** Move the parameters into the Setup System.
