<!-- globs: Assets/**/*.asset; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/**/*.asset`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### CFG-005: New content is added to every platform variant
- **Priority:** Critical
- **Rule:** A new enemy, particle, pool entry or texture is also added to the mobile (and other platform) configs and overrides, or the PR says why not.
- **Why:** Otherwise the mobile build has missing pools/effects or oversized assets.
- **Bad → Good:** entry added only to `…_Default.asset` → also added to the mobile holder.
- **Fix:** Update all platform variants.
