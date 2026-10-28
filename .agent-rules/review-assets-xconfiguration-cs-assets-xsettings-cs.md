<!-- globs: Assets/**/*Configuration.cs,Assets/**/*Settings.cs; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/**/*Configuration.cs,Assets/**/*Settings.cs`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### CFG-002: Configurations are data-only
- **Priority:** Architecture
- **Rule:** SO configs hold serialized data and trivial getters only — no lookup caches, processing, or methods that take services (Editor `OnValidate` excepted).
- **Why:** Logic inside assets is hidden behaviour.
- **Bad → Good:** `LoadingScreenSettings.GetCurrentSettings()` with a `_presetCache` / `GetText(ILocalizationService)` → a resolver Service behind an interface / an extension method.
- **Fix:** Move the logic to a Service.
