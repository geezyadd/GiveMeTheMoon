<!-- globs: Assets/**; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/**`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### ASSET-005: Source-asset hygiene
- **Priority:** Best Practices
- **Rule:** No unexplained oversized source assets, duplicate `(1)` copies, placeholder names (`asdf.cs`, `asdjaspdjoa.controller`) or unused art.
- **Why:** They bloat the repo and build and confuse the next person.
- **Bad → Good:** `Mobs_Blocking_Monk_V1.fbx` at 75MB → re-exported at a normal size; `Stinger_Melee (1).fbx` → deleted.
- **Fix:** Clean up before pushing.
