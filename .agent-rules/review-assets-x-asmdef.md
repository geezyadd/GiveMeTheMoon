<!-- globs: Assets/**/*.asmdef; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/**/*.asmdef`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### ARCH-006: asmdef references are not transitive; reference by GUID
- **Priority:** Architecture
- **Rule:** When adding a `using` for another assembly, add the matching reference to the owning asmdef (sub-asmdefs each need their own). References use GUIDs (`"GUID:…"`, "Use GUIDs" checked), not assembly names.
- **Why:** Missing refs cause compile errors the C# error message doesn't localize to the asmdef; GUID refs survive assembly renames.
- **Bad → Good:** `"references": ["MovementModule"]` → `"references": ["GUID:5c29d4825674a054a8cbba7a4afd808a"]`
- **Fix:** Add the GUID reference (from the target asmdef's `.meta`) to the exact asmdef that owns the file.
