<!-- globs: Assets/**/Installers/**; rules: 3; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/**/Installers/**`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### DI-009: Single-owner helpers are created via IInstantiator, not bound
- **Priority:** Architecture
- **Rule:** A helper used by one owner is created with `IInstantiator` / `Container.Instantiate*`; bind only contracts consumed from outside, plus Systems; don't add an installer (or MonoInstaller) just to inject one spawned object.
- **Why:** A binding exposes the helper to the whole context.
- **Bad → Good:** `Container.Bind<UIAnimationPlaybackController>()` → `_instantiator.Instantiate<UIAnimationPlaybackController>()`.
- **Fix:** Remove the binding/installer.


### DI-002: Register services with the correct lifetime
- **Priority:** Best Practices
- **Rule:** Choose the intended lifetime deliberately (single vs transient/per-scope); don't make stateful services accidental singletons.
- **Why:** Wrong lifetime causes shared mutable state or unexpected re-creation and subtle bugs.
- **Fix:** Set the explicit lifetime in the installer; document why.


### DI-011: Module bindings live in the module's installer, one binding per line
- **Priority:** Best Practices
- **Rule:** Bindings go into the module's own `*Installer`, not the context installer; each binding is one statement on one line with the fluent chain on that line (`Container.BindInterfacesTo<X>().AsSingle();`); `.NonLazy()` is redundant on `BindInterfacesTo<IInitializable impl>()`.
- **Why:** Keeps module boundaries; consistent, diff-friendly installers.
- **Bad → Good:** `...AsSingle().NonLazy()` in `MainGameContextInstaller` → the module's installer, no NonLazy.
- **Fix:** Move bindings; drop NonLazy.
