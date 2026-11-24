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
stay capsules; the feel numbers are not tuned; the animator is the throwaway idle / walk one.

## Assembly
`Features.PlayerBodyModule` (layer 4; references Mirror, Zenject, `CharacterMovableModule`, `ActiveRagdollModule`,
`FloatingControllerModule`, `CameraModule`, `PlayerReplicaModule`, `GameCoreModule.Contracts`). No installer: nothing
is bound. Editor code in `Features.PlayerBodyModule.Editor`.
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
    colliders shoves it away) and follows the root limb each physics step; a capsule found over 1 m from where it
    was put was teleported (spawn, map change), and the ragdoll is moved by the same offset and stopped.
  - Held (`CharacterMovableBase.IsHeld`: a seat or a ride places the capsule): the capsule leads. When the hold
    starts, the ragdoll is put on the capsule's spot once, horizontally (the seat places the capsule once a frame on
    the carrier as drawn, `ICarrier.DrawnPosition` / `DrawnRotation`, so the spot is read in the drawn pose and put in
    the step's `PoseBefore`); then it just stands there, carried by the ship like anyone standing on the deck, at its
    own height (no sitting). It does not walk while held, the walk animation stays idle, and no carrier turn is added
    to the camera (the held capsule already turns the view).
  - Each frame: the sampled animation is turned to the shown look, the animator's `Forward` comes from the walk input
    of the active controls (keyboard or packet; not the measured speed, which drifts on a moving carrier; under 0.125
    is cut), and the local player's camera turns with the carrier the ragdoll rides (`CameraCarryTurnModel`).
  - `SetSuspended` (from `PlayerLifeBody` for a dead player): falls asleep at once and hides the model's renderers.
  - A turned-off player object turns its detached ragdoll off too; destroyed while awake, it destroys it. A ragdoll
    lost under a live player hands the walking back to the capsule.
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
  pass through the player's own body), leaves it asleep, gives the animator the test controller and wires `PlayerRagdollBody` (with the player's `BodyCopyDriver`). It also points every component's `_bodyRenderer` field (the nameplate) at the model's skinned mesh, sets the
  animator to always animate (the pose is needed while the owner's model is not drawn) and wires
  `OwnBodyHider` with every renderer of the model. Running it again rebuilds it.
