# CoroutineRunnerModule — one place to run coroutines

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

`ICoroutineRunner` (`StartCoroutine`, `StopCoroutine`) lets plain classes run coroutines, all from one object. The
implementation `CoroutineRunner` is a MonoBehaviour on the `CoroutineRunner` object in `GlobalScene` (lives the whole
game); bound `FromComponentInHierarchy` by `CoroutineRunnerModuleInstaller`, called from GameCore's
`GlobalSceneInstaller`. A coroutine run here outlives the scene of its caller: stop it when the caller goes away.

Assembly `Features.CoroutineRunnerModule` (layer 1; references Zenject). Used by
[LoadingScreenModule](../LoadingScreenModule/AGENTS.md).
