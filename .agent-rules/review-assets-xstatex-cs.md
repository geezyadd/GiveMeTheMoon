<!-- globs: Assets/**/*State*.cs; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/**/*State*.cs`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### ROLE-008: State-owned resources are released on that state's Exit
- **Priority:** Critical
- **Rule:** Session state, coroutines and locks started in a (game-flow or FSM) state are cleaned up in that state's `Exit`, not in the next state's `Enter`.
- **Why:** New transitions (Restart, …) skip cleanup placed in a specific next state; coroutines keep running into the other state.
- **Bad → Good:** `EnterHubState.Enter() { LockPerk(); }` → `SessionPlayingState.Exit() { LockPerk(); }`.
- **Fix:** Move cleanup to the owning state's Exit.
