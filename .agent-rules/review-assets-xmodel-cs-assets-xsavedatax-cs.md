<!-- globs: Assets/**/*Model.cs,Assets/**/*SaveData*.cs; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/**/*Model.cs,Assets/**/*SaveData*.cs`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### CFG-004: Save data holds only player progress
- **Priority:** Critical
- **Rule:** Config values never become fields of the persisted save data.
- **Why:** The saved copy overrides new defaults — existing players never receive config changes.
- **Bad → Good:** `_data.UnPuttableItems` (saved) → a config field outside the saved class.
- **Fix:** Move config out of the save model.
