# PlayerBodyModule — the body the player walks with

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## The idea
The player's movement (`CharacterMovableBase`) pushes an `IMovableBody`. For the player's own body this module makes
it the active ragdoll ([ActiveRagdollModule](../ActiveRagdollModule/AGENTS.md)) instead of the capsule, so the same
walking, sprint, jump, stats and input drive a physical body.

## Status
Being built on `demo/active-ragdoll`. The owner's player always walks with the ragdoll, and every peer that simulates
a copy of it from its packets (PlayerReplicaModule, new sync mode) runs that copy as a ragdoll too. Known limits: a
copy's snaps and pulls are not hidden (the model no longer sits under `ReplicaVisual`); in the old sync mode copies
stay capsules; the feel numbers are not tuned. The animator and its parameters are
[PlayerAnimationModule](../PlayerAnimationModule/AGENTS.md)'s.

## Assembly
`Features.PlayerBodyModule` (layer 5; references Mirror, Zenject, `CharacterMovableModule`, `ActiveRagdollModule`,
`FloatingControllerModule`, `CameraModule`, `PlayerReplicaModule`, `PlayerAnimationModule`, `GameCoreModule.Contracts`,
`Unity.Animation.Rigging`, `AttackModule`, `AttackModule.Contracts`, `GrabModule`, `TargetModule`, `InputModule`,
`InputModuleRalization`). No installer: nothing is bound. Editor code in `Features.PlayerBodyModule.Editor`; tests in
`Features.PlayerBodyModule.Editor.Tests` (`PlayerAttackPressRuleTests`: a free press counts, an item in the hand, a
seat, a control block, a free cursor or a running attack refuse it; `HitPushRuleTests`: a fast limb scaled, a slow
one raised to the minimum along its motion, a still one toward the head).
- `PlayerRagdollBody` (on `DummyPlayer`, execution order −190: after the carriers take their step pose, before what
  reads the body): wakes the ragdoll while the player is owned here or its `BodyCopyDriver` simulates it as a copy,
  and it is not suspended; keeps it asleep otherwise.
  - Asleep: the ragdoll's components are off, its limbs kinematic without collisions, its animator off, the model
    sits in the capsule.
  - Awake: the model leaves the capsule (a scene root, since physical limbs cannot live inside a moving rigidbody)
    with the capsule's velocity and moves to the persistent scene (`SceneNames.Global`; `DontDestroyOnLoad` if it is
    not loaded). The ragdoll hands walking, falling and jumping to the movement (`UsesPlainGravityInAir`, no
    horizontal or air drag). The capsule turns kinematic, loses its hover and every collider of its own
    (`CapsuleColliders`, kept off each step: a revive turns them on again, and a capsule following the ragdoll with
    colliders shoves it away) and follows the root limb each physics step; a capsule found over 1 cm from where it
    was put was moved by someone else (a server placement or world shift through `BodyStateChannel`, a spawn), and
    the ragdoll is moved by the same offset and stopped.
  - Held (`CharacterMovableBase.IsHeld`: a seat or a ride places the capsule): the capsule leads. When the hold
    starts, the ragdoll is put on the capsule's spot once, horizontally (the seat places the capsule once a frame on
    the carrier as drawn, `ICarrier.DrawnPosition` / `DrawnRotation`, so the spot is read in the drawn pose and put in
    the step's `PoseBefore`); then it stands there, carried by the ship like anyone standing on the deck, at its own
    height (no sitting), and a soft damped spring in the carrier's plane pulls it back so it never drifts off
    (profile `Stand`: `Held Spot Spring` 4, `Held Spot Damping` 4, `Held Spot Tolerance` 0.05 m; speed relative to the
    carrier). It does not walk while held, the walk animation stays idle, and no carrier turn is added
    to the camera (the held capsule already turns the view).
  - Each frame: the sampled animation is turned to the shown look and, while the player drifts overboard, smoothly
    tilted to fly head first (`AnimatedPoseSampler.Tilt`, `CharacterDriftHeadFirstTurn`); the local player's camera
    turns with the carrier the ragdoll rides (`CameraCarryTurnModel`). The animator's parameters are set by
    `PlayerAnimationDriver` (PlayerAnimationModule).
  - `SetSuspended` (from `PlayerLifeBody` for a dead player): falls asleep at once and hides the model's renderers.
  - A turned-off player object turns its detached ragdoll off too; destroyed while awake, it destroys it. A ragdoll
    lost under a live player hands the walking back to the capsule.
- The slap aims at a head through PlayerAnimationModule's `LimbReach` (on the ragdoll's root), whose `RightHand` chain
  is the rig `SlapRig` on the humanoid in `DummyPlayer` (a `RigBuilder`, kept off; the ragdoll's `AnimatedPoseSampler`
  builds and evaluates it right after the animator) with `SlapArmReach` (`TwoBoneIKConstraint`: right arm → forearm →
  hand, position only) and its target `SlapReachTarget` on the `DummyPlayer` root (not under the model, which the
  sampler turns while sampling).
- The slap is AttackModule's `Attacker` on `DummyPlayer` with `SlapAttack`, `TargetAttackAim` (origin
  `SlapHeadCenter`, facing the movement's `RotatablePart`, which turns to the shown look on every peer) and
  `AttackHitDetector` (the `RightHand` limb). The player is a target too: TargetModule's `Target` on `DummyPlayer`
  (`Hittable`, aim point `SlapHeadCenter`, an empty point on the head bone in the middle of the head, 0.14 local up
  from the bone: the head limb's own pivot is down at the neck, and slaps aimed there hit the neck; part: the `Head`
  limb's rigidbody; only while simulated). The player's own parts of it:
  - `PlayerAttackPresses` (`IAttackPresses`; refs: the movement, `BodyCopyDriver`, `GrabController`, the `Attacker`):
    on the owner's peer the fire button, counted only when `PlayerAttackPressRule` allows it (an empty hand: an item
    in the hand takes the fire button, a pistol fires and nothing slaps; not seated; the controls not blocked; the
    cursor not free; no attack running), so a refused press never reaches the copies; on another peer the owner's
    count in the packet the copy shows, while the copy is simulated.
  - `PlayerHitPush` (refs: its `Target`, the movement, the `Head` and `Spine2` limbs): the reaction to a hit on its
    target, on the peer where the hit landed: the head limb gets a velocity change at the aim point
    (`AddForceAtPosition`, so the head also tips away) and the chest `Chest Share` 0.6 of it (not while held or
    asleep): the head jerks, the upper body bends at the hips and the joints bring it back. The head's velocity
    change is the hitting limb's velocity x `Limb Speed Scale` 6, at least `Min Head Speed` 14 m/s, at most
    `Max Head Speed` 40 m/s (`HitPushRule.HeadVelocity`). The slapped player's owner pushes its real body and the
    others follow its packets; a peer where the hand misses shows no push. A body that is hit but not pushed is
    logged with `Debug.LogError` (prefix `Attack:`) for now.
  History (2026-11-26): `PlayerSlapAim` did the aim and the reach from `PlayerAnimationDriver.Slapped`, and the
  press count was `IMovementControls.SlapCount` from `CharacterInputBuffer`; the reach went from 0 at the start to 1
  by 0.3, held to 0.45, which pulled the arm straight to the head and lost the wind-up. Then `PlayerAttackAim` and
  `PlayerSlapHit` aimed at and hit the players' heads over `BodyStateChannelRegistry`, until the TargetModule. First
  the slapper's owner alone detected the hit and sent it by a command and a `ClientRpc` to every peer, which pushed
  the head when the message came, out of step with the slapping copy's hand there; the hit radius was 0.25, then
  0.4 m, which a colliding hand could never reach (the player is scaled 1.76: the head capsule is 0.39 m round and
  the hand 0.09 m, so a touching hand stays about 0.48 m from the centre), so no slap ever hit; the hand aimed at the
  head limb's pivot at the neck and eased to a stop there; strength went scale 1 / 2.5 / 4 -> 6, minimum none / 6 /
  9 -> 14, max 10 / 20 / 30 -> 40 m/s, head only -> head and chest (the head alone, 25 of the body's 305 kg, only
  swayed the body: its push spreads over the whole mass).
- `CapsuleColliders`: the capsule's colliders (not the ragdoll's), turned off, kept off and restored.
- `RagdollMovableBody`: `IMovableBody` over `RagdollBody` + `RagdollGroundSensor` + `RagdollCarrierRide` (root limb
  position, mass-weighted velocity, total mass, acceleration and velocity change on every limb, `PauseSupport` = the
  sensor's stand pause after a jump, `MoveBy` shifts every limb, `SetVelocity` shifts every limb's velocity by the same
  amount, the ride as `Rider`).
- `OwnBodyHider` (on `DummyPlayer`): on the local player, while the active camera is the first-person one
  (`IGameCameraService.ActiveId == FPCamera`), every renderer of the model (the body mesh, the face parts) only casts
  shadows; in the third-person view and for other players the whole body is drawn.
- `RagdollBodyParts`: the ragdoll components switched together, the hand-over of locomotion, the pose reset.
- Editor: `PlayerRagdollBaker` (Tools > Ragdoll > Bake Ragdoll Into Player): on the humanoid in `DummyPlayer` adds
  the ragdoll components, fills `GameResources/PlayerRagdollProfile` from the avatar, bakes the limbs, puts every limb
  collider's object on the `PlayerRagdoll` layer (not in the interaction aim's blocker mask, so aiming and grabbing
  pass through the player's own body), leaves it asleep, gives the animator the player's controller (`PlayerAnimatorBuilder.LoadOrBuild`) and wires `PlayerRagdollBody` (with the player's `BodyCopyDriver` and `CharacterDrift`) and `PlayerAnimationDriver`. It also points every component's `_bodyRenderer` field (the nameplate) at the model's skinned mesh, sets the
  animator to always animate (the pose is needed while the owner's model is not drawn) and wires
  `OwnBodyHider` with every renderer of the model. Running it again rebuilds it.
