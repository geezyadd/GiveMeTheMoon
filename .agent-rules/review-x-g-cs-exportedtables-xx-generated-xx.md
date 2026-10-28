<!-- globs: **/*_g.cs,**/ExportedTables/**,**/Generated/**; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `**/*_g.cs,**/ExportedTables/**,**/Generated/**`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### ASSET-003: Generated code and exported tables are regenerated, never hand-edited
- **Priority:** Critical
- **Rule:** Regenerate on an up-to-date branch; never hand-edit generated output or commit stale / someone else's regenerated files.
- **Why:** Stale addresses and enums compile but point to the wrong assets or values.
- **Bad → Good:** a truncated `"rogressModule/..."` address → rerun the generator.
- **Fix:** Regenerate and check the diff.
