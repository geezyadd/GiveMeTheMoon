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
stay capsules; rides, seats and the ship deck are not checked (a ride that moves the capsule
less than 1 m a step is undone by the follow); the feel numbers are not tuned; the animator is the
throwaway idle / walk one.

## Assembly
`Features.PlayerBodyModule` (layer 4; references Mirror, Zenject, `CharacterMovableModule`, `ActiveRagdollModule`,
`FloatingControllerModule`, `CameraModule`, `PlayerReplicaModule`, `GameCoreModule.Contracts`). No installer: nothing is bound. Editor code in
`Features.PlayerBodyModule.Editor`.
- `PlayerRagdollBody` (on `DummyPlayer`, execution order −200, before what reads the body): wakes the ragdoll while the player is owned here or its
  `BodyCopyDriver` simulates it as a copy, and keeps it asleep otherwise. Asleep: the ragdoll's components are off, its limbs kinematic without
  collisions, its animator off, the model sits in the capsule as before. Awake: the model leaves the capsule (it
  becomes a scene root, since physical limbs cannot live inside a moving rigidbody) with the capsule's velocity, the
  ragdoll hands walking, falling and jumping to the movement (`UsesPlainGravityInAir`, no horizontal or air drag),
  the capsule turns kinematic without its collider and hover and follows the root limb each physics step (camera,
  network and everything that looks at the capsule keep working), and the movement drives `RagdollMovableBody`.
  Each frame it turns the sampled animation to the look yaw and sets the animator's `Forward` from the walk input of the active controls (keyboard or packet; not the measured
  speed, which drifts on a moving carrier; under 0.125 is cut to keep the idle still), turned to the shown look, and turns the local player's camera with the carrier
  the ragdoll rides (`CameraCarryTurnModel`). Awake, the detached model moves to the persistent scene
  (`SceneNames.Global`, where the players live; `DontDestroyOnLoad` if it is not loaded), so a map unload does not
  take it; the player destroys it with itself. and a capsule found over 1 m from where it was put
  last step was teleported (spawn, map change): the ragdoll is moved by the same offset and stopped. A ragdoll lost
  under a live player hands the walking back to the capsule. Destroyed while awake, it destroys the detached model.
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
  pass through the player's own body), leaves it asleep, gives the animator the test controller and wires `PlayerRagdollBody` (with the player's `BodyCopyDriver`). It also sets the animator to always animate (the pose is needed while the owner's model is not drawn) and wires
  `OwnBodyHider` with every renderer of the model. Running it again rebuilds it.
