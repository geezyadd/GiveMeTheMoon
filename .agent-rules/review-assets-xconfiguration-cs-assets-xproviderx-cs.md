<!-- globs: Assets/**/*Configuration.cs,Assets/**/*Provider*.cs; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/**/*Configuration.cs,Assets/**/*Provider*.cs`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### CFG-003: Config is validated centrally at load and fails loudly
- **Priority:** Architecture
- **Rule:** Parse, then validate typed config in one place at startup (or in the provider's own check); consumers don't re-validate; missing or duplicate config is an error/assert with a message, not a warning.
- **Why:** Scattered checks are forgotten and bad GD data reaches release builds.
- **Bad → Good:** a UI model calling `provider.CheckMissingEnemySprites()` → the provider's `DataCheck()` run by the validator at init.
- **Fix:** Move the check to the validator/provider; assert with a message.
