# WeaponModule — weapons

The weapon core, its one network path and the client feel. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file.

## Assembly
- `Features.WeaponModule.Contracts` (layer 1; references: NetworkModel): `IWeaponTarget`, `WeaponHit`, `IWeaponTargetCollider`, `IWeaponTargetRegistry`, `IWeaponTargetSource`, `IWeaponImpactFeedback`, and for bodies that react to shots `WeaponImpact`, `IWeaponImpactReceiver`, `IWeaponImpactReceiverRegistry` (PlayerBodyModule's `PlayerWeaponImpactReaction` is one); a weapon target such as a field rock implements them without referencing the weapon module; separate from `GameCoreModule.Contracts` so that one stays free of NetworkModel / Mirror.
- `Features.WeaponModule` (layer 5 since 2026-11-26, was 2; references: WeaponModule.Contracts, CameraModule.Contracts, CameraModule, CharacterMovableModule, InputModule, InputModuleRalization, ExplosionModule, NetworkModel, Zenject, Mirror): the weapon core, its one network path `WeaponHolderSync` and the client feel, see "Weapons" in Key places below; it does not reference the ship, the flight field or the player; its inspector part picker is `Features.WeaponModule.Editor`. The minigun turret on the ship is [TurretModule](../TurretModule/AGENTS.md).

## Key places
| What | Where |
|---|---|
| Weapons | `Assets/Features/WeaponModule/`. A weapon is one `WeaponDefinition` asset (`GameResources/Weapons/`: stable id, name, shots per second, damage per hit, range, the parts, a `WeaponFeelProfile` with the look numbers: muzzle flash, casings, recoil, camera shake, tracer every N, impact) in `WeaponCatalog_Default` (`Configurations` group, `WeaponCatalogValidator` at startup). Parts are `[SerializeReference]` objects inside the definition, picked in the inspector from a type menu (`WeaponPartPickerAttribute`): fire mode (`IWeaponFireModeDefinition` -> runtime `IWeaponFireMode`; `AutoFireMode` with barrel spin-up / spin-down), shot delivery (`IShotDeliveryDefinition` -> `IShotDelivery`, outcome through `IShotOutcomeReceiver`, may come later than the shot; `HitscanDelivery` with a spread cone over an injected `IWeaponRaycast`; `PhysicsWeaponRaycast` takes the nearest solid collider or trigger that carries an `IWeaponTargetCollider`, so a trigger with no target, such as a field structure's volume, lets the shot through, and carries a struck `IWeaponImpactFeedback` in the hit and the outcome, which `WeaponShooter` shows on every peer that fires or replays the shot before passing the outcome on), hit effects (`IHitEffect` list; `DamageHitEffect`); holder and aim (`IWeaponHolder`, `IWeaponAim`) and the client feel (`IWeaponFeel`) are interfaces only. The server checks a reported hit with `WeaponHitRule` (pure: weapon held, target alive, range + tolerance, fire rate budget `WeaponShotBudget` with tolerance and burst from `WeaponHitRuleConfiguration_Default`); targets implement `IWeaponTarget` from `Features.WeaponModule.Contracts`. A new weapon is a new asset and, if needed, one new part type, never a `switch` on a weapon type (`WeaponCatalogGuardTests`). A delivery definition builds its runtime part from an `IShotDeliveryContext` (`PhysicsShotDeliveryContext`: `PhysicsWeaponRaycast` on the delivery's `_hitLayers`, triggers included; the minigun hits only layer `FieldRock`, never the own ship or players; a collider of a target carries an `IWeaponTargetCollider` with its id); `IWeaponShooterFactory` gives every holder its own `WeaponShooter` (fire mode + delivery), whose outcomes go to `ReportedShotOutcomes` on the holding peer (feel + hit report) or `ShownShotOutcomes` on every other peer (feel only). **Network path, one for every weapon:** see "The weapon system" below. **Feel** (`Feel/`, client only, everything pooled at `Bind`, 0 B GC per frame of continuous fire): `MinigunFeel` (the slot view drives it with `Advance`) with `BarrelSpin`, `MuzzleFlash`, `RecoilJitter`, the camera shake through `ICameraShakeService` (the seated owner's view only: a continuous fire shake while it looks through the gun and a one-shot shake from impacts within `NearImpactDistanceMeters`; profiles `CameraShake_MinigunFire` / `CameraShake_MinigunNearImpact` in `GameResources/Weapons`, referenced by the `WeaponFeelProfile`), `CasingStream` (casings simulated in the floor's frame, `IWeaponFeelFloor`: they ride with the ship, bounce and pile on the deck and are never physics bodies), `TracerStream`, `ImpactBursts` (flash, sparks, chips, dust in the frame's custom simulation space). The muzzle flash (`MuzzleFlash`) is one small glow and a short light **per shot** (not per frame of fire), kept small, dim and short so the gunner sees the stream of tracers past it (2026-10-27, was a glow every frame of fire; size / light / seconds was -> now: light gun 0.8 / 5 / 0.06 -> 0.28 / 1.5 / 0.03, minigun 1.2 / 8 / 0.05 -> 0.35 / 2 / 0.03, heavy gun 2 / 14 / 0.08 -> 0.45 / 2.5 / 0.035, pistol 0.35 / 6 / 0.06 -> 0.14 / 1.5 / 0.035; colour alpha 1 -> 0.7; tracers unchanged). The weapons have no sound (removed on 2026-10-27 with `WeaponFeelAudio` and the clips). Every number is in `MinigunFeelProfile`. **Debug:** the debug window's Weapons → "Spawn / Remove the weapon test dummy" on the host (editor / development builds, `WeaponDebugTargetSystem` / `IWeaponDebugTargets`, the spawned dummy in `WeaponDebugTargetModel`) puts a `WeaponDebugTarget` dummy in front of the ship (layer `FieldRock`; it sways from side to side, counts and flashes the hits the server accepted). Everything about it is compiled only in editor / development builds: its prefab and ship offset come from `WeaponDebugTargetConfiguration_Default` (`Configurations` group, bound under the same define), and `WeaponDebugTargetPrefabSystem` registers the prefab with the Mirror client on every client start, so it is not in the network manager's spawn prefabs. Who shoots is only the server's `IWeaponShooterIdentity` (`ServerWeaponHolding`); a peer's `IWeaponHolder` gives just the trigger and the aim. |

## The weapon system (ported from FLEXUS-test-assignment, 2026-11-26, `demo/weapons`)
The shape of the FLEXUS cannon's weapon module, on Mirror and on Unity physics (FLEXUS's own projectile physics was not
ported, on purpose).
- **`IWeapon`** (`Core/`): a weapon a player holds: its `WeaponDefinition`, its feel (`IWeaponFeelPresenter`),
  `IsUsable` (a broken turret is not) and **capabilities** asked for with `TryGetCapability<T>()` (`WeaponCapabilities`:
  the weapon itself or a component on its object). A hand weapon item (`HandWeaponItem`) and a turret
  (`ShipTurretStation`) are weapons. Capabilities now: `IWeaponMagazine` (`WeaponMagazine`, a `NetworkBehaviour` on
  the item: rounds and the reload's end as SyncVars, server writes, the holding peer spends its rounds at once and
  takes the server's count when it arrives; capacity and reload time from the definition; capacity 0 = unlimited:
  never runs out, never reloads).
- **`WeaponModel`** (local, `DataInstaller`) + **`IWeaponService`** (`WeaponService`): the local player's current
  weapon and the holder that fires it; `TryGetCapability` on the current weapon.
- **`WeaponSystem`** (`IInitializable`, `ITickable`): the local player's controls of whatever they hold: `Fire` (LMB)
  is the trigger, `Reload` (R, new input action) asks the magazine, both off while a window frees the cursor or the
  controls are blocked; the HUD ring (`HeldWeaponReadinessModel`) shows the next shot's readiness, the reload's
  progress while one runs, 0 with an empty magazine.
- **`WeaponHolderSync`** (player prefab): the one network path. What holds a weapon (the hand's `HandWeaponWielder`,
  the turret seat) calls `Equip(weapon, aim)` / `Unequip()` on **every peer**. On the holder's peer
  `OwnerAdvance(trigger, dt)` runs the fire mode (gated by the magazine); each shot is delivered (`WeaponShooter`:
  fire mode + delivery), shown at once (`WeaponShotPresenter`) and sent as a `WeaponShotMessage` (`CmdShot`). The
  server checks it (holding, weapon index, `ServerTrySpendRound`), applies a target hit with `WeaponHitService` /
  `WeaponHitRule` as before, launches the real projectile and passes the shot to every other peer (`RpcShot`,
  `includeOwner = false`), which shows the same shot: feel, the struck surface's `IWeaponImpactFeedback` (found by a
  ray to the end point), the struck body's reaction. So every peer shows each shot exactly once. The trigger and the
  aim are still SyncVars (`CmdSetAim`, sent on a trigger change and on the turret's schedule) for what lasts between
  shots: barrel spin on other peers, the weapon's pose in other players' views. `CmdReload` starts a reload on the
  server.
- **Deliveries**: `HitscanDelivery` (a ray; `PhysicsWeaponRaycast` now also finds a struck `IWeaponImpactReceiver` by
  its rigidbody and passes through the shooter's own body) and `LaunchDelivery` (`LaunchDeliveryDefinition`: only a
  launch direction inside the spread cone; the weapon's `ProjectileDefinition` does the rest). `WeaponSpread` is the
  shared cone.
- **Bodies struck**: `WeaponDefinition.ImpactPushSpeed` (velocity change of the struck part) and
  `ImpactKnockdownSeconds` (limp time; 0 only pushes). `WeaponImpactReceiverRegistry` (`IWeaponImpactReceiverRegistry`
  + `IWeaponImpactLookup`) lists the receivers on each peer; every peer applies the impact to its own copy of the
  body, like the explosion wave.
- **Projectiles** (`Projectiles/`): `ProjectileDefinition` (prefab, speed, gravity on = ballistic
  and bouncing / off = straight at constant speed, lifetime, goes off on any impact or only on a moving body, the
  `ExplosionPreset`, prewarm count), `Projectile` (a Rigidbody flown by Unity physics; ignores the shooter's body and the weapon's
  colliders, `WeaponHolderColliders`), `ProjectilePool` (one `ProjectilePrefabPool` per prefab, a `PoolBase` from
  PoolModule; every projectile of the `WeaponCatalog` is prewarmed at start; no maximum, so a projectile in flight is
  never taken back and the pool grows instead; rebuilt when its root went away with a scene), `ProjectileLifetimeSystem` (`IFixedTickable`), `ProjectileSpawner`. The **server's**
  projectile is the real one: where it ends, `IExplosionService.ServerExplode(..., showsEffect: false)` sets off the
  wave on every peer and `RpcProjectileEnded` ends the other peers' copies (a copy flies from the same launch). Every
  projectile that ends on a peer, the server's or a copy (by itself or by that message), plays the explosion's look
  there by itself (`IExplosionEffects`: the air explosion and the screen shake); the look never goes over the network. Projectile
  id = holder netId << 16 | shot sequence, the same on the owner and the server.
- Data rules: a weapon with a projectile needs no hit effect (`WeaponCatalogValidator`, `WeaponCatalogGuardTests`).

## Hand weapons now (lobby, `WeaponRack` row at x -3.5..-1.1, z 1.5)
| Weapon (id) | Fire | Magazine | Hits | Body struck |
|---|---|---|---|---|
| Pistol (`pistol`) | single shot, 3 / s (was 0.5 / s, 2026-11-26) | unlimited | ray, 12 damage, FieldRock + PlayerRagdoll + Default + Ground (was FieldRock only) | pushed 8 m/s |
| Knockdown gun (`knockdown-gun`) | single shot, 1 / s | unlimited | ray, 4 damage, 60 m | pushed 14 m/s, limp 2.5 s |
| Grenade launcher (`grenade-launcher`) | single shot, 1.25 / s | unlimited | `GrenadeProjectile`: 18 m/s, ballistic, bounces (`GrenadeBounce`), goes off after 2 s or on a moving body | `ExplosionMedium` |
| Rocket launcher (`rocket-launcher`) | single shot, 1 / s | unlimited | `RocketProjectile`: 28 m/s straight, goes off on any impact or after 3.5 s | `ExplosionLarge` |

Every hand weapon's magazine is unlimited (capacity 0) since 2026-11-26 on the user's request; the magazine, its
reload (R) and the reload ring stay in the code for a weapon that sets a capacity. The new three are not in the shop (lobby only for now); their tooltip is still the pistol's. Models (2026-11-26): ThePolynestLab Cosmic Retro Blasters in
`Global/Packages/ThePolynestLab/Cosmic_Retro_Blasters` (only the 4 used FBX, their 8x8 palette textures imported
with point filter, no mips, uncompressed, and side-view icons as the hand weapons' `Icon`): pistol = Blaster_1,
knockdown gun = Blaster_11, grenade launcher = Blaster_2_5, rocket launcher = Blaster_2_12; URP materials
`HandWeaponModule/GameResources/Materials/CosmicRetroBlaster1/2` (base + emission). In each item the model sits under
`RecoilRoot/<Name>View`, barrel along +Z; the muzzle is at the barrel's front face and the item's box collider is the
model's bounds.

## Tiers, the pistol and their parts
- Weapons in `WeaponCatalog_Default`: `Minigun`, `LightMachineGun`, `HeavyGun` (the turret tiers, see [TurretModule](../TurretModule/AGENTS.md)) and `Pistol` (see [HandWeaponModule](../HandWeaponModule/AGENTS.md)).
- `SingleShotFireMode` (`SingleShotFireModeDefinition`): one shot per press, cooldown = one shot interval of the weapon's shots per second; readiness 0 -> 1 over the cooldown.
- The turret tiers have no magazine (capacity 0): they never run out.
- `WeaponFeelProfile` got `RecoilKickMeters` / `RecoilKickDegrees` (a single shot's kick, `RecoilKick`), `ShotShake` (the holder's view kick per shot of a single-shot weapon). `HandGunFeel` is the feel presenter of a gun in the hand (a muzzle flash per shot, kick, tracer, impact, view kick).
- `IWeaponCatalogIndexService` (`WeaponCatalogIndexService`) gives a weapon's catalog index **by id**: in a player build a scene spawn prefab holds its own copy of a definition, apart from the Addressables catalog's (a reference match threw `KeyNotFoundException` on the coop client). The turret station uses it too.
- `HeldWeaponReadinessModel` (bound in `DataInstaller`, local): the local player's weapon in hand for the HUD ring around the crosshair (`HeldWeaponReadinessView` in `GameHudWindow`, LobbyModule).

## Tests
- `SingleShotFireModeTests`, `WeaponCatalogIndexServiceTests` (a copy of a definition finds its index, an unknown one throws).
- `Features.WeaponModule.Editor.Tests` (auto fire cadence and spin-up / spin-down at any frame rate, hitscan spread bounds and outcomes, every `WeaponHitRule` branch and the fire rate budget, weapon catalog guard, `WeaponTargetRegistryTests`: target sources)
- EditMode guard: `WeaponCatalogGuardTests` (weapon ids set and unique, every weapon has all its parts, the startup validator passes and fails on a shared id, no weapon code branches on a concrete weapon).
- In the weapon tests `WeaponHitServiceTests` (the server hit path), `WeaponShooterTests` (the fire mode births the shots, a ray shot's outcome is the struck target, a launch strikes nothing and leaves along the aim) and `WeaponImpactReceiverRegistryTests` (a reacting body is found by its part and its network id while listed).
