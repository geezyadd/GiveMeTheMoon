<!-- globs: Assets/**/*Installer.cs; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/**/*Installer.cs`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### DI-010: Bind instances with Bind<T>().FromInstance(x)
- **Priority:** Style
- **Rule:** Use `Container.Bind<T>().FromInstance(x).AsSingle()`, never `BindInstance(x)`.
- **Why:** Team convention; the bound contract type stays explicit.
- **Bad → Good:** `Container.BindInstance(_enemySettings).AsSingle();` → `Container.Bind<HeadcrabEnemySettings>().FromInstance(_enemySettings).AsSingle();`
- **Fix:** Rewrite `BindInstance` calls in touched installers.
