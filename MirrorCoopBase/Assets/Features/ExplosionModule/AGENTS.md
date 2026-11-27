# ExplosionModule — explosions and their wave

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## The idea
An explosion is a wave: a sphere that grows from the centre at its preset's speed, so near things are reached before
far ones. Each thing is reached once, when the front passes it. Plain rigidbodies are thrown away from the centre;
things that react in their own way (a ragdoll creature, a seated player) are **receivers** and get the hit to handle
themselves. Kinds of explosions (small, medium, large...) are data: presets in one configuration.

## Status
Built on `demo/explosion` (2026-11-26). Since 2026-11-27 an explosion looks like the GabrielAguiarProductions air
explosion (VfxModule) and shakes the screen (ScreenShakeModule); the see-through stand-in sphere is gone. Since 2026-11-27 it plays the preset's
FMOD sound through AudioModule (all three presets on `event:/Weapon/explosion_mid` for now). No
damage, no cover behind walls. Known limits: the centre is a world point, so an
explosion during the flight's world shift is not moved with the world; a wave reaches a receiver by its centre only.

## Assemblies
- `Features.ExplosionModule.Contracts` (layer 1, no references): `ExplosionHit` (centre, speed at the receiver,
  upward share, knockdown seconds, `VelocityAt(point)`), `IExplosionReceiver` (centre, parts, `ReceiveExplosion`),
  `IExplosionReceiverRegistry`. Receivers in other modules reference only this.
- `Features.ExplosionModule` (layer 4; references Mirror, Zenject, Contracts, `DebugModule.Contracts`, `CameraModule`,
  `TargetModule`, `VfxModule`, `ScreenShakeModule`, `AudioModule`, `FMODUnity`). Bindings: `ExplosionModuleInstaller`, called from GameCore's
  `GlobalSceneInstaller`; the configuration is bound in `ConfigurationInstaller`
  (`Address.Configurations.ExplosionConfiguration_Default`).

## Parts
- `Configuration/ExplosionPreset` (Create > Explosion > Preset): display name, radius, wave speed, centre speed (velocity
  change right at the centre, m/s), falloff curve (share of the centre speed by distance / radius), upward share,
  knockdown seconds, knockdown min share (farther than this share a ragdoll is only pushed), and its look: the effect
  (`VfxDefinition`), the effect's scale, the screen shake profile and the shake distance (full at the centre, nothing
  this far from the camera), and its sound (an FMOD `EventReference`, played at the centre; empty: none). Presets in `GameResources/Presets` (radius, wave speed, centre speed,
  knockdown): Small (3 m, 25 m/s, 15 m/s, 1.5 s), Medium (5 m, 30 m/s, 22 m/s, 2.5 s), Large (9 m, 35 m/s, 32 m/s,
  3.5 s); centre speeds were 10 / 15 / 22 until 2026-11-26.
- `Configuration/ExplosionConfiguration` (`GameResources/Configurations/ExplosionConfiguration_Default`, Addressables
  group `Configurations`): the preset list (a preset goes over the network as its index here; `IndexOf` matches by
  the asset's name too, since in a player build a preset referenced by a scene object or a weapon asset is a copy of its
  own: by reference alone no projectile or mine explosion was sent, fixed 2026-11-27, `ExplosionConfigurationTests`), the layers whose loose
  rigidbodies are thrown, the debug distance.
- **The look is local, never over the network.** `IExplosionEffects` / `ExplosionEffects` plays the preset's effect at
  the centre, its sound (`IAudioService.PlayOneShot` at the centre) and the shake (strength 1 - camera distance / shake distance). A projectile's explosion (grenade,
  rocket: WeaponModule's `ProjectileSpawner`) shows it on every peer where that peer's own projectile ends (the
  server's or a copy) and asks the server's wave with `showsEffect: false`; any other explosion (the lobby mine, the
  debug buttons) shows it when its wave starts (`ExplosionMessage.ShowsEffect`).
- Looks (`GameResources/Effects`): `ExplosionAirSmall/Medium/Large` (prefab variants of the pack's
  `vfx_StylizedExplosion_360_v1_S/M/L` with a `ParticleSystemVfx`; the pack's root system loops, `Play Once` turns it
  off) and their `...Vfx` definitions (prewarm 1, max 6, life 0.5..6 s); `ScreenShake_ExplosionSmall/Medium/Large`
  (amplitude 2.5 / 4 / 6 since 2026-11-27: at 0.45 / 0.7 / 1.0 the `6D Shake` noise moved the view by ~0.1 degree and nobody saw it; frequency 1, 0.3 / 0.35 / 0.45 s); shake distances 15 / 25 / 40 m. The pack itself sits
  in `Assets/GabrielAguiarProductions` (imported by the user; not yet moved under `Global/Packages`).
- **Ground or air look** (2026-11-27, `demo/ground-explosion`): `ExplosionEffects.Play` asks `ExplosionGroundProbe`
  whether the explosion went off on the ground: a surface on the configuration's `Ground Layers` (Default, Ground,
  FieldRock) at most `Ground Probe Distance` (1 m) under the centre, leaning at most `Ground Max Slope Degrees` (40)
  there, and 8 rays on a ring of the preset's `Ground Flat Radius` (Small / Medium / Large: 1.5 / 2.5 / 4 m) all finding
  the surface within `Ground Flat Tolerance` (0.6 m: the rock islands are uneven by ~0.4 m) of that height. Then one of
  the preset's `Ground Effects`, picked at random on each peer, plays on the surface point; in the air, near an edge, on a slope or a small thing the air
  `Effect` plays at the centre. A grenade or rocket that struck the ground goes off on it; one that timed out in the air
  or struck a player (the centre is high above the ground) stays in the air; the lobby mine (0.3 m over the ground) gets
  the ground look too. Each peer decides by itself, like the rest of the look. Ground looks (the user's pick, random of
  two): `ExplosionGroundV2Small / Medium / Large` and `ExplosionGroundV4...` (+ `...Vfx`), the pack's
  `vfx_StylizedExplosion_v2/v4_S/M/L` with a `ParticleSystemVfx` (Play Once). Tests: `ExplosionGroundProbeTests`.
- `IExplosionService` / `ExplosionService`: `ServerExplode(centre, preset)`, server only. Sends `ExplosionMessage`
  (centre, preset index) to every client with `NetworkServer.SendToAll`; a server without a local client runs the
  wave itself.
- `Network/ExplosionNetworkChannel`: the client handler of `ExplosionMessage`, registered again on every client start
  (GameCore's `ExplosionNetworkChannelSystem`, on `ConnectionNetworkEvents.ClientStarted`), starts the wave.
- `Wave/ExplosionWaves` (`IFixedTickable`): the waves running on this peer, each stepped every physics step; starts a
  wave and, when the message says so, its look (`IExplosionEffects`). `Wave/ExplosionWave`: per step the radius grows by speed x step up to the preset's radius;
  every receiver whose centre is inside and was not reached gets `ReceiveExplosion` (on every peer); on the server only,
  every non-kinematic rigidbody inside (`OverlapSphereNonAlloc` on the affected layers, triggers ignored) that no
  receiver owns and was not thrown gets an impulse of `VelocityAt` x mass at its bounds' point nearest the centre, so it
  also spins. Loose items are simulated on the server and synced to the clients by their network bodies.
- `Wave/ExplosionReceiverRegistry`: the receivers on this peer and the rigidbodies they own.
- `DebugActions/ExplosionDebugSystem` (editor / development builds): section Explosions, one button per preset,
  "Explode: <name>", host only, `Debug Distance` (4 m) in front of the host's camera.
- `Testing/ExplosionTestMine`: a test object in `LobbyScene` (`ExplosionTestMine`, a flat red cylinder at (5, 0, 5)
  with the Large preset (was Medium until 2026-11-26)): on the server, when an available `Hittable` target (a live player's head) is within
  `Trigger Radius` (2 m) on the ground plane and at most `Trigger Height` (6 m) above it (the scaled-up player's head
  is about 4 m up: a 3D distance never let it go off), it goes off 0.3 m above itself and waits `Cooldown Seconds` (2.5 s).

## Receivers now
- PlayerBodyModule's `PlayerExplosionReaction` (on `DummyPlayer`): throws every limb and knocks the ragdoll down
  (ActiveRagdollModule's `RagdollKnockdown`).
- ShipModule's `ShipRiderExplosionEject` (on `DummyPlayer`): on the server, a hit that knocks down stands a seated
  player up.

Adding a receiver: a component implementing `IExplosionReceiver` that injects `IExplosionReceiverRegistry`, adds itself
while on and removes itself when off (see `PlayerExplosionReaction`).
