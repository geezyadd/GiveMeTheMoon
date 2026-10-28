<!-- globs: Packages/**; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Packages/**`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### PKG-002: No Odin dependency in portable modules
- **Priority:** Best Practices
- **Rule:** Modules meant to move between projects don't depend on Odin attributes or serialization; use plain `OnValidate` checks instead.
- **Why:** Odin is a heavy dependency that ties the module to projects that have it.
- **Fix:** Replace Odin attributes with plain Unity/C#.
