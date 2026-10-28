<!-- globs: Assets/**/Mock/**,Assets/**/Realizations/**; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/**/Mock/**,Assets/**/Realizations/**`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### BUILD-005: Implementation-only assemblies declare [assembly: AlwaysLinkAssembly]
- **Priority:** Critical
- **Rule:** Assemblies reached only through installers declare `[assembly: AlwaysLinkAssembly]` above the namespace; mock asmdefs prefer `excludePlatforms` over an include list.
- **Why:** Managed code stripping removes the assembly and the binding silently disappears in the build.
- **Bad → Good:** `SteamAchievementServiceInstaller.cs` without the attribute → `[assembly: AlwaysLinkAssembly]` at the top of the file.
- **Fix:** Add the attribute.
