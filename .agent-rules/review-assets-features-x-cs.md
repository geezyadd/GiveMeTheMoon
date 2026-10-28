<!-- globs: Assets/Features/**/*.cs; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/Features/**/*.cs`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### CFG-001: Designer-tunable values live in SO configuration / DB
- **Priority:** Best Practices
- **Rule:** Radii, delays, speeds, attempts, balance numbers, content lists and data mappings come from a ScriptableObject settings asset (or DB table), not literals, switch expressions or per-component serialized floats.
- **Why:** Game designers tune without code changes; adding an enum value shouldn't need new code.
- **Bad → Good:** `RaycastUpward(30f)`, `15` attempts → `_settings.CeilingRaycastDistance`, `_settings.TeleportAttempts`.
- **Fix:** Add the fields to the module's settings SO.
