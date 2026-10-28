<!-- globs: Assets/**/*Presenter*.cs,Assets/**/*View*.cs; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/**/*Presenter*.cs,Assets/**/*View*.cs`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### UI-003: Presenter is Unity-agnostic with no non-UI side effects; views don't know their surroundings
- **Priority:** Architecture
- **Rule:** Unity display work (`SetActive`, spawning backgrounds, animations) belongs to the View; non-UI actions (open URL, lock input, save) belong to a service/system; the Presenter only wires Model/services to the View and doesn't expose its View publicly; a View doesn't know which window or system it sits in.
- **Why:** The UI technology can change (UI Toolkit) and actions stay reusable beyond one button.
- **Bad → Good:** Presenter `_background.SetActive(true); Application.OpenURL(url);` → `View.ShowBackground(); _linkService.Open(url);`.
- **Fix:** Move Unity calls into the View and side effects into a service.
