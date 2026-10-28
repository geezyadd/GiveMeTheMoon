<!-- globs: package.json; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `package.json`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### PKG-001: Submodules ship package.json; defaults/examples go to Samples; platform code in realization submodules
- **Priority:** Best Practices
- **Rule:** Every submodule has a `package.json` with correct metadata and bundles its required plugins; default configs, test accessors and example installers go to `Samples~/`; platform-specific data lives in realization submodules, not Core.
- **Why:** Without package.json the package can't be consumed; example code leaks into consumers.
- **Bad → Good:** only an asmdef + a runtime `*_Default.asset` → `package.json` added; defaults moved to `Samples~/`.
- **Fix:** Add package.json; move the content.
