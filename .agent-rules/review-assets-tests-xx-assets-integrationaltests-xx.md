<!-- globs: Assets/**/Tests/**,Assets/IntegrationalTests/**; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/**/Tests/**,Assets/IntegrationalTests/**`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### TEST-001: Play-mode tests are short, single-scenario, named When_And_Then
- **Priority:** Best Practices
- **Rule:** One scenario per test, named `When<Action>_And<Action>_Then<Result>`; don't wait for X and then assert the same X; don't make members public just for tests — fake the input instead. Where the repo uses `MuffinIntegrationalTests`, its `RULES.md` takes precedence on structure.
- **Why:** Long multi-scenario tests make a failure hard to locate.
- **Bad → Good:** a 100-line `WhenGameStarted_ThenOpenMenu_ThenOpenUpgrade...` → several short `When..._Then...` tests.
- **Fix:** Split the test; add a fake.
