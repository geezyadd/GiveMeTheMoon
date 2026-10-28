<!-- globs: Assets/**/*.meta; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/**/*.meta`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### ASSET-002: .meta GUIDs stay stable
- **Priority:** Critical
- **Rule:** A diff that changes the `guid:` of an existing asset, or deletes the `.meta` of a file that still exists, is reverted (or every reference is fixed and the change is justified).
- **Why:** References are GUID-based — a regenerated GUID silently breaks everything that pointed to the asset (missing refs, purple materials).
- **Bad → Good:** `-guid: d52fe14…` / `+guid: 0c8a978…` → restore the original `.meta`.
- **Fix:** Restore from git; move/rename assets inside Unity, not in the file system.
