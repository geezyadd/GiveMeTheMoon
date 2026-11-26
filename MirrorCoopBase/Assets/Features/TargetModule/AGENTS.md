# TargetModule — targets other systems act on

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## The idea
A target is something other systems can find and act on, by kind (`TargetKind`, flags): `Hittable` (an attack goes
for it and lands on it) and `LookAt` (eyes that notice things; nothing uses it yet). What a hit does is up to
reaction components on the target that listen to its `Hit` event (the player's push; later a mob catching fire or
running away, a particle). Nothing here goes over the network: every peer lists its own targets.

## Status
Step 3 of the slap rework (2026-11-26, `demo/slap`). The players' heads are the only targets.

## Assembly
`Features.TargetModule` (layer 2; references Zenject). Bindings: `TargetModuleInstaller` (`TargetRegistry` as a
single), called from GameCore's `GlobalSceneInstaller`. Tests: `Features.TargetModule.Editor.Tests`
(`TargetPickRuleTests`: nothing near or behind, the nearer of two, facing sideways, a point right below, a forward
tilted up counts on the ground plane).

## Parts
- `TargetKind` (`[Flags]`): `None`, `Hittable`, `LookAt`.
- `Target` (a component on the target's prefab; injected): its kinds, its aim point (where attacks and looks go; empty:
  its own object), its parts (the rigidbodies whose touch counts as touching it) and `Only While Simulated` (on by
  default: while any part is kinematic, a sleeping ragdoll, a dead player or a body nobody simulates here, it is not
  `IsAvailable`). Listed in the registry while on. `ReceiveHit(TargetHit)` raises `Hit` on the peer where the hit
  landed.
- `TargetHit`: the attacker's object, the hitting limb's velocity, the limb-to-aim-point offset, touch or came close.
- `TargetPickRule` (pure): `PickNearest`, the nearest point within a distance of the seeker's origin and within an
  angle of its forward on the ground plane (a point right above or below is not ruled out by the angle).
- `TargetRegistry`: the targets that are on; `PickNearest(kind, origin, forward, distance, angle, own)` among the
  available ones of that kind but the seeker's own, `FindByPart(kind, rigidbody, own)`: the target that owns a
  touched rigidbody.
