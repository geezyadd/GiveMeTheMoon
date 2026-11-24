# PlayerBodyModule — the body the player walks with

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## The idea
The player's movement (`CharacterMovableBase`) pushes an `IMovableBody`. For the player's own body this module makes
it the active ragdoll ([ActiveRagdollModule](../ActiveRagdollModule/AGENTS.md)) instead of the capsule, so the same
walking, sprint, jump, stats and input drive a physical body.

## Status
Being built on `demo/active-ragdoll`. The owner's player always walks with the ragdoll. Known limits: other peers
keep seeing a capsule and get the owner's capsule state (the capsule is kinematic and follows the ragdoll, so its sent
velocity is wrong, and its own `CarrierRider` is off, so the packet names no carrier); rides, seats and the ship deck
are not checked; the feel numbers are not tuned; the animator is the throwaway idle / walk one.

## Assembly
`Features.PlayerBodyModule` (layer 4; references Mirror, Zenject, `CharacterMovableModule`, `ActiveRagdollModule`,
`FloatingControllerModule`, `CameraModule`). No installer: nothing is bound. Editor code in
`Features.PlayerBodyModule.Editor`.
- `PlayerRagdollBody` (on `DummyPlayer`): wakes the ragdoll while the player is owned here and keeps it asleep
  otherwise (other players' copies). Asleep: the ragdoll's components are off, its limbs kinematic without
  collisions, its animator off, the model sits in the capsule as before. Awake: the model leaves the capsule (it
  becomes a scene root, since physical limbs cannot live inside a moving rigidbody) with the capsule's velocity, the
  ragdoll hands walking, falling and jumping to the movement (`UsesPlainGravityInAir`, no horizontal or air drag),
  the capsule turns kinematic without its collider and hover and follows the root limb each physics step (camera,
  network and everything that looks at the capsule keep working), and the movement drives `RagdollMovableBody`.
  Each frame it turns the sampled animation to the look yaw and sets the animator's `Forward` from the walk input (not the
  measured speed, which drifts on a moving carrier; under 0.125 is cut to keep the idle still), and turns the local player's camera with the carrier
  the ragdoll rides (`CameraCarryTurnModel`). Destroyed while awake, it destroys the detached model.
- `RagdollMovableBody`: `IMovableBody` over `RagdollBody` + `RagdollGroundSensor` (mass-weighted velocity, total
  mass, acceleration and velocity change on every limb, `PauseSupport` = the sensor's stand pause after a jump).
- `RagdollBodyParts`: the ragdoll components switched together, the hand-over of locomotion, the pose reset.
- Editor: `PlayerRagdollBaker` (Tools > Ragdoll > Bake Ragdoll Into Player): on the humanoid in `DummyPlayer` adds
  the ragdoll components, fills `GameResources/PlayerRagdollProfile` from the avatar, bakes the limbs, leaves it
  asleep, gives the animator the test controller and wires `PlayerRagdollBody`. Running it again rebuilds it.
