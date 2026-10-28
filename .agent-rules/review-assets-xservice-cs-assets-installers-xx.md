<!-- globs: Assets/**/*Service.cs,Assets/**/Installers/**; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/**/*Service.cs,Assets/**/Installers/**`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### DI-008: Services are bound and consumed only through their interface
- **Priority:** Architecture
- **Rule:** Every Service has an interface and is bound `Bind<IX>().To<X>()` / `BindInterfacesTo<X>()`; never `BindInterfacesAndSelfTo` or a self-bind for services.
- **Why:** A self-bind lets consumers couple to the concrete class.
- **Bad → Good:** `BindInterfacesAndSelfTo<LoadingScreenService>()` → `Bind<ILoadingScreenService>().To<LoadingScreenService>().AsSingle()`.
- **Fix:** Extract the interface; rebind.
