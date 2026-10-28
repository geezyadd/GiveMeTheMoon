<!-- globs: Assets/**/*Presenter*.cs,Assets/**/*View*.cs,Assets/**/*Window*.cs; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/**/*Presenter*.cs,Assets/**/*View*.cs,Assets/**/*Window*.cs`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### UI-001: All UI is MVP; Presenter depends on an abstract ViewBase
- **Priority:** Architecture
- **Rule:** UI = View + Presenter (+ Model). `PresenterBehaviour<T>` takes the abstract `*ViewBase`; PC and mobile differ only by view; elements the presenter needs (e.g. `RectTransform _content`) are serialized on the view, not cast or derived; views and presenters hold no gameplay timers, state or config logic.
- **Why:** Deleting a window must not break gameplay; one presenter can't drift between platforms.
- **Bad → Good:** `PresenterBehaviour<ConfirmExitPopupView>` + `(RectTransform)View.transform`; a countdown inside `SceneSwitchPresenter` → `PresenterBehaviour<ConfirmExitPopupViewBase>` + a serialized ref; the timer in a System, the time in a Model.
- **Fix:** Add the ViewBase/Presenter; move logic out.
