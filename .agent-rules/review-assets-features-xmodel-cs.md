<!-- globs: Assets/Features/**/*Model.cs; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/Features/**/*Model.cs`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### ROLE-005: Models hold state, not behavior flow
- **Priority:** Best Practices
- **Rule:** A *Model stores data and fires change events; it does not subscribe to external events or call services/systems, and knows nothing about views/presenters. State is exposed read-only (`{ get; private set; }`, `IReadOnly*` collections) and changes only through intention-revealing methods (`Heal()`, `AddCoins()`) that raise the change event themselves. A model may load its own data.
- **Why:** A model that reaches outward couples data to flow; callers that set a value and fire the event separately forget the event.
- **Bad → Good:** `Model.FishHealth += heal; Model.DoHeal(heal);` / public `List<T>` → `Model.Heal(heal);` / `IReadOnlyList<T>` + `Add`/`Remove` methods.
- **Fix:** Move orchestration to a System; route every write through a mutator that fires the event.
