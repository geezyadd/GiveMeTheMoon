# AttackModule — attacks of ragdoll bodies

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## The idea
An attack (the slap first; later a kick, a headbutt, a mob's swipe) is data (`AttackDefinition`) carried out by an
`Attacker` on any humanoid ragdoll that has a `LimbReach` and an animation driver. The attack is cosmetic: it looks the
same for every player and is not strict over the network. Every peer that simulates the attacker's body runs the
attack by itself from the attack presses it reads there (the owner's button, a copy's packets, later a mob's AI). Hits
and their effects are not decided here: other systems read the attack's state and events.

## Status
Step 2 of the slap rework (2026-11-26, `demo/slap`). The hit is still PlayerBodyModule's `PlayerSlapHit`; the
TargetModule (targets of any kind, reactions to a hit) is the next step. The player is the only attacker.

## Assemblies
- `Features.AttackModule.Contracts` (layer 1; no references): `IAttackPresses` (`IsAvailable`, `AttackCount`): where
  an attacker's presses come from on this peer. PlayerReplicaModule reads the owner's count through it for the body
  state packet.
- `Features.AttackModule` (layer 4; references `AttackModule.Contracts`, `PlayerAnimationModule`). No installer: the
  attacker sits on the body's prefab. Tests: `Features.AttackModule.Editor.Tests` (`AttackTimelineTests`: the reach
  weight over the attack, the hit window, past the window, over; `AttackPressCounterTests`: the first count is no
  press, one press for a grown count and for several missed ones, a wrapped count, no press after the presses were
  unavailable or the counter was reset).

## Parts
- `AttackDefinition` (ScriptableObject, referenced by the attacker on the prefab like the hand weapons'
  definitions): the animator trigger, the limb and its `LimbReach` priority, the timing (`AttackTiming`), the target
  search (`Max Distance`, `Max Angle`) and `Reach Through` (the limb aims this far past the target, so it swings
  through at speed). `GameResources/Attacks/SlapAttack.asset`: trigger `Slap`, `RightHand`, priority 0; reach from
  0.2 s (the clip's wind-up is kept), full by 0.34, held to 0.5, gone by 0.8; hit window 0.15–0.6 s; over at 0.8 s;
  1.3 m, 60°, 0.3 m (the values `PlayerSlapAim` / `PlayerSlapHit` had).
- `AttackTimeline` (pure): `ReachWeight` (0 until the reach starts, smooth in, held, smooth out), `IsInHitWindow`,
  `IsPastHitWindow`, `IsOver`.
- `AttackPressCounter` (pure): turns a count into presses; once for any number missed between two reads; the first
  count read after the presses were unavailable is no press, so a copy that starts or a body that wakes does not
  attack on its own.
- `IAttackAim`: picks the target when an attack starts (a transform the limb follows while the attack lasts; null:
  nothing in reach, the plain animation). The player's is PlayerBodyModule's `PlayerAttackAim`.
- `Attacker` (on the body's prefab, execution order −100: before the ragdoll samples its pose; refs: the definition,
  an `IAttackPresses` and an `IAttackAim` component, the `PlayerAnimationDriver`, the ragdoll's `LimbReach`, the aim
  origin, the middle of its own head): holds one `LimbReach` request for the attack's limb while on. Every physics
  step: a press starts an attack when none is running and the animator runs (`PlayerAnimationDriver.CanPlay`); a press
  during an attack is ignored. Starting fires the trigger (`PlayerAnimationDriver.Play`), picks the target once and
  raises `Started`; at `IsOver` it raises `Ended`. While the attack has a target the limb's goal stands
  `Reach Through` past it on the line from the aim origin, with `AttackTimeline.ReachWeight`. Others read
  `IsAttacking`, `AttackNumber`, `Target` (kept after the attack), `SecondsSinceStart`, `IsInHitWindow`,
  `IsPastHitWindow`.
