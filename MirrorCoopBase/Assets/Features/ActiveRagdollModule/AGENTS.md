# ActiveRagdollModule — physical bodies that follow an animation

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## The idea
A body made of physical bones that keeps chasing a normal animation. The module knows nothing about the player:
any creature gets a ragdoll from its own body profile, and whoever owns the body (player input, AI, a network copy)
drives it through the module's API.

## Status
Being built in steps on `demo/active-ragdoll`, on a separate test scene first. Done: step 1, the body itself — a
profile describes the bones as data, an editor button puts rigidbodies, capsules and joints on them. The body falls
as a passive ragdoll; a temporary drive scale slider stiffens the joints toward the baked pose. Step 2, the pose from
the animator: the animator runs by hand once per physics step, every limb keeps its target, the bones go back to
physics; a gizmo shows the targets. Not done yet: following the pose, hover, walking, the control factor, the player.

## Assembly
`Features.ActiveRagdollModule` (layer 1, no references). Editor code in `Features.ActiveRagdollModule.Editor`.
- `Profile/RagdollBodyProfile` (ScriptableObject, Create > Ragdoll > Body Profile): the limbs and the numbers shared by
  all of them (mass multiplier, capsule radius multiplier, damper to spring ratio, joint angular limit, angular damping, max angular velocity,
  ignoring collisions between nearby limbs). The first limb is the root and gets no joint.
- `Profile/RagdollLimbProfile`: one limb — bone path from the body root, end bone path for the capsule (or a fallback
  length along the parent limb), mass, joint drive spring, capsule radius.
- `Body/RagdollBody` (on the creature's root): the limb list, applies max angular velocity and ignores nearby limb
  collisions on `Awake`; `ApplyDriveScale`, `StopAllMotion`, `SetKinematic`, `SetCollisions`.
- `Body/RagdollLimb` (on each physical bone): its rigidbody, joint (none on the root), colliders, parent limb and drive
  spring / damper; the bind rotation relative to the parent limb (taken on `Awake`) and the current
  `LimbPoseTarget` (rotation relative to the parent limb, world forward / up, world offset from the root limb).
  Limbs are listed parents first.
- `Body/AnimatedPoseSampler` (on the body root, execution order −95, before any force): takes the animator's graph
  off Unity's update (`Stop` + `DirectorUpdateMode.Manual`), and on each physics step but the first evaluates it by
  `fixedDeltaTime`, captures every limb's target, then puts every limb back on its rigidbody. Root motion is off.
  Animator parameters are set by the body's owner in `Update`; they apply at the next evaluation.
- `Body/RagdollPoseGizmo`: white = the animated skeleton hung on the physical root limb, red = physical limb to its
  target.
- Editor: `RagdollBodyInspector` (buttons "Fill profile from Humanoid avatar", "Bake limbs", "Remove limbs"),
  `RagdollBodyBaker`, `LimbCapsuleShaper`, `HumanoidProfileFiller` (15 limbs for a Humanoid avatar: hips, spine,
  chest, head, thigh / shin / foot, upper arm / forearm / hand; radii and lengths scale with the hips-to-head
  distance), `LobbyTestDummyPlacer` (Tools > Ragdoll > Place Test Dummy In Lobby: a baked humanoid
  `RagdollTestDummy` 2 m in front of the lobby spawn, as large as the humanoid in `DummyPlayer`, profile
  `GameResources/HumanoidRagdollProfile`, with the pose sampler, the gizmo and a throwaway animator from
  `TestDummyAnimatorBuilder`: `GameResources/RagdollTestDummy.controller`, float `Forward` blends idle (0) to walking
  forward (1), clips from `Global/Packages/KevinIglesias/HumanAnimations`; running it again replaces the dummy).
  A test helper until the ragdoll moves into the player.

## Setting up a body
1. Put the model under a root object, add `RagdollBody`, create a body profile and assign it.
2. For a Humanoid avatar press "Fill profile from Humanoid avatar"; for another creature fill the limbs by hand.
3. Press "Bake limbs". Baking again replaces what the last bake added.
