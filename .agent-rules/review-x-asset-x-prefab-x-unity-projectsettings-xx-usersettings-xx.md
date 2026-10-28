<!-- globs: **/*.asset,**/*.prefab,**/*.unity,ProjectSettings/**,UserSettings/**; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `**/*.asset,**/*.prefab,**/*.unity,ProjectSettings/**,UserSettings/**`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### ASSET-001: Commit only intended changes to prefabs, scenes, .asset and ProjectSettings
- **Priority:** Critical
- **Rule:** Every hunk in a serialized Unity file is explained by the task; revert editor noise, accidental prefab overrides, test toggles (`IsTestDatabase`, timers tuned for local testing, splash/build settings, `*DebugConfiguration` assets), caches (e.g. FR2_Cache) and `EditorUserSettings`.
- **Why:** Merged test flags switch the whole team and the builds; noise hides real regressions and causes scene conflicts.
- **Bad → Good:** `IsTestDatabase: 0→1`, `Duration: 60→15` → `git checkout main -- <file>` (or `git restore -p`) for that hunk.
- **Fix:** Read the YAML diff; discard unrelated hunks; revert, don't delete files.
