<!-- globs: **/*; rules: 2; dev-review-rules@local, generated - do not edit -->
# Team review rules for `**/*`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### PR-003: Changes to shared/base logic and level assets are justified
- **Priority:** Architecture
- **Rule:** Changing base classes, bootstraps, infrastructure (e.g. ViewsSystem) or level prefabs for one feature is explained in the PR with an impact check on all other users.
- **Why:** A tweak for one item regresses everything else that relies on it.
- **Bad → Good:** editing `LineArmControllerBase` for the healing fish → a fish-specific override, or a documented impact check.
- **Fix:** Scope the change, or document it.


### PR-001: Keep the diff in scope — no whole-file reformatting, stray renames or side refactors
- **Priority:** Best Practices
- **Rule:** Change only what the task needs: no AI/IDE cleanup rewriting whole files (line endings, blank lines, reordering), no renames of public APIs/events on the side, no sandbox leftovers (unused shaders, reference PNGs, throwaway scripts); unrelated fixes go in their own PR.
- **Why:** Side changes break other modules and hide the real change from the reviewer.
- **Bad → Good:** an installer diff `@@ -1,32 +1,32 @@` for a 2-line change → a 2-line diff.
- **Fix:** Revert formatting-only and unrelated hunks.
