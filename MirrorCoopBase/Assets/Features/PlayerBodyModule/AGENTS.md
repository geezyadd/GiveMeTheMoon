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
`Features.PlayerBodyModule` (layer 4; references Mirror, Zenject, `CharacterMovableModule`, `ActiveRagdollModule`,
`FloatingControllerModule`, `CameraModule`, `PlayerReplicaModule`, `PlayerAnimationModule`, `GameCoreModule.Contracts`,
`Unity.Animation.Rigging`). No installer: nothing is bound. Editor code in `Features.PlayerBodyModule.Editor`; tests in
`Features.PlayerBodyModule.Editor.Tests` (`SlapAimRuleTests`: no head near or behind, the nearer of two, facing
sideways, a head right below, the reach weight over the slap; `SlapHitRuleTests`: a fast hand scaled, a slow one
raised to the minimum along its motion, a still one toward the head).
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
- `PlayerSlapAim` (on `DummyPlayer`, execution order −100, before the sampler; refs: `PlayerAnimationDriver`, the
  movement, the ragdoll's `LimbReach`, the player's own head `RagdollLimb` and `SlapHeadCenter`, an empty point on the head bone in the
  middle of the head, 0.14 local up from the bone: the head limb's own pivot is down at the neck, and slaps aimed there
  hit the neck; every head position below is this point, `HeadCenter`): on every peer that simulates the body, on
  `PlayerAnimationDriver.Slapped` picks the nearest head of another player (`BodyStateChannelRegistry`, awake ragdolls
  only) in front (`SlapAimRule.PickHead`: within `Max Distance` 1.3 m of its own head, at most `Max Angle` 60° off the
  shown look on the ground plane). It holds one `RightHand` request (priority 0) while on; every physics step it puts the
  request's goal past that head (`Reach Through` 0.3 m further along the line from its own head through it, so the
  hand swings through the head at speed instead of easing to a stop on it) and sets its weight (`SlapAimRule.Weight`:
  0 until `Start At Seconds` 0.2, so the slap starts as the plain animation and keeps its wind-up (the clip winds up
  at 0.22 s and hits at 0.36 s); then eases in to 1 by `Full At Seconds` 0.34, holds to `Release At Seconds` 0.5, eases
  out to 0 by `End At Seconds` 0.8; 2026-11-26 was: from 0 at the start to 1 by 0.3, held to 0.45, which pulled the
  arm straight to the head and lost the wind-up).
  No head in front: weight 0, the plain slap animation.
- `PlayerSlapHit` (on `DummyPlayer`; refs: `PlayerSlapAim`, the movement, the right hand's and the chest's
  `RagdollLimb`): the hit is played on every peer by itself, nothing goes over the network. Wherever the slapper's
  body is simulated (the owner's own; a copy elsewhere, which slaps when the owner's packets say so and aims on that
  peer), from `Hit Start Seconds` 0.15 to `Hit End Seconds` 0.6 of a slap, once per slap, when that hand touches the
  aimed head (`RagdollLimb.Touching`: a contact with the head limb's rigidbody, face included; the hand's speed is the
  contact's relative speed) or its centre comes within `Hit Radius` 0.6 m of the head's centre, it pushes the slapped
  player's body on this peer: the head limb gets a velocity change at `HeadCenter` (`AddForceAtPosition`, so the head
  also tips away) and the chest limb (`Spine2`) `Chest Share` 0.6 of it (not while held or asleep): the head jerks,
  the upper body bends at the hips and the joints bring it back. The head's velocity change is the hand's velocity ×
  `Hand Speed Scale` 6, at least `Min Head Speed` 14 m/s, at most `Max Head Speed` 40 m/s
  (`SlapHitRule.HeadVelocity`). So the push comes exactly when the hand reaches the head as each peer shows it; the
  slapped player's owner pushes its real body and the others follow its packets, and a peer where the hand misses
  shows no push.
  History (2026-11-26): first the slapper's owner alone detected the hit and sent it by a command and a `ClientRpc`
  to every peer, which pushed the head when the message came, out of step with the slapping copy's hand there; the hit
  radius was 0.25, then 0.4 m, which a colliding hand could never reach (the player is scaled 1.76: the head capsule
  is 0.39 m round and the hand 0.09 m, so a touching hand stays about 0.48 m from the centre), so no slap ever hit;
  the hand aimed at the head limb's pivot at the neck and eased to a stop there; strength went scale 1 / 2.5 / 4 → 6,
  minimum none / 6 / 9 → 14, max 10 / 20 / 30 → 40 m/s, head only → head and chest (the head alone, 25 of the body's
  305 kg, only swayed the body: its push spreads over the whole mass).
  For now every slap is logged with `Debug.LogError` (prefix `Slap:`): on every peer the aim (`PlayerSlapAim`: the
  head aimed at, or none with every awake head's distance and angle), then the hit here (touched or came close, hand
  and head speed, distance) or the miss (how close the hand came), and why a hit body was not pushed. Nothing else of
  the body is pushed.
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
