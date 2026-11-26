# VfxModule — pooled visual effects

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## The idea
Ported from FLEXUS-test-assignment's VfxModule (2026-11-27). An effect is a prefab with a `VfxEffectBase` component;
what draws it is an implementation detail: `ParticleSystemVfx` (particle systems) or `VisualEffectVfx` (VFX Graph). The
pool only plays, stops and asks whether the effect is still alive. Effects are local: nothing here goes over the
network; each peer plays an effect where it sees the thing happen.

## Assembly
`Features.VfxModule` (layer 1; references Zenject and, optionally, `Unity.VisualEffectGraph.Runtime`). Bindings:
`VfxModuleInstaller`, called from GameCore's `GlobalSceneInstaller`. No configuration asset: every effect is its own
`VfxDefinition`.

## Parts
- `Effects/VfxEffectBase`: `IsAlive`, `Play`, `Stop`.
- `Effects/ParticleSystemVfx`: plays the root system with its children (`Clear` then `Play`), stops emitting on `Stop`,
  alive while any system is. `Play Once` (on by default) turns looping off in every system of the instance, so a pack's
  looping demo prefab plays one cycle and ends (the pack prefab itself is not changed).
- `Effects/VisualEffectVfx`: compiled only while the Visual Effect Graph package is installed (`VFX_GRAPH_PRESENT`,
  the asmdef's version define); it is not installed now.
- `VfxDefinition` (Create > Vfx > Definition): the prefab, prewarm count, max count, min and max lifetime. A new effect
  is a new asset, never a new enum value.
- `IVfxService` / `VfxService`: `Play(definition, position, rotation, scale)` for a free-standing effect,
  `Attach(definition, target)` for one that follows a transform (`VfxHandle.Stop`, safe against a reused instance).
- `VfxPoolRegistry`: one `VfxPool` per definition, made on its first play (prewarmed then), under one root kept across
  scene changes (DontDestroyOnLoad); a root gone with play mode starts the pools over.
- `VfxPool` / `VfxInstance`: a free instance or a new one up to the max, past it the oldest free-standing one is reused;
  an instance goes back once it is older than the min lifetime and nothing of it is alive, or past the max lifetime.
- `VfxSystem` (`ILateTickable`): ages every playing effect once a frame.

## Users
- ExplosionModule: the air explosions (`ExplosionAirSmall/Medium/Large`).
