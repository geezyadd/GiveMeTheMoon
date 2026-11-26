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
as a passive ragdoll. Step 2, the pose from the animator: the animator runs by hand once per physics step, every limb
keeps its target, the bones go back to physics; a gizmo shows the targets. Step 3, following the pose: joint drive,
torque toward the animated axes and a spring force from the physical root; checked with the root pinned in the air.
Step 4, standing: ground from the shared support probe and limb contacts, hover on the trunk, own air gravity, drag.
Step 5: the player's movement can drive the ragdoll (see PlayerBodyModule). Not done yet: the control factor (fixed
at 1), falling and getting up, network copies.

## Assembly
`Features.ActiveRagdollModule` (layer 2, references `FloatingControllerModule` for `SupportProbe` and `ICarrier`, and
`Unity.Animation.Rigging` for the sampler's optional rig). Editor code in `Features.ActiveRagdollModule.Editor`.
- `Profile/RagdollBodyProfile` (ScriptableObject, Create > Ragdoll > Body Profile): the limbs and the numbers shared by
  all of them (mass multiplier, capsule radius multiplier, damper to spring ratio, joint angular limit, angular damping, max angular velocity,
  ignoring collisions between nearby limbs), pose force, damping and torque, and `Stand` settings. The first limb is the root
  and gets no joint.
- `Profile/RagdollStandSettings`: ground (mask, max stand slope 50°, min support mass 500, probe radius and extra
  length), hover (stand force 100, smoothing 0.25 m), air gravity (ramp curve and speed, max −500, Unity gravity below
  control 0.9) and drag (velocity kept per 1/60 s, 0.85, turned into the physics rate).
- `Profile/RagdollLimbProfile`: one limb — bone path from the body root, end bone path for the capsule (or a fallback
  length along the parent limb), mass, joint drive spring, capsule radius, `Hovered` (held up by the hover),
  `HeightReference` (kept at its animated height above the ground; one per body).
- `Body/RagdollBody` (on the creature's root, execution order 100): the limb list and `ControlFactor` (0 limp ..
  1 follows the animation; fixed at 1 for now); on `Awake` applies max angular velocity, takes each limb's bind
  pose, ignores nearby limb collisions and sets the joint drives; on each physics step, after every other ragdoll
  component, hands each limb's summed force to its rigidbody in one call. `ApplyDriveScale`, `StopAllMotion`,
  `SetKinematic`, `SetCollisions` (on again re-ignores nearby limbs), `IgnoreCollisionsWith`, `TotalMass`,
  `AverageVelocity` (mass-weighted), `AddAccelerationToAll`, `AddVelocityChangeToAll`, `MoveAllBy` (moves each
  limb's transform and rigidbody together: the sampler writes the transforms each step, and a moved transform is
  copied onto its rigidbody before the simulation, so moving only the rigidbody is undone), `ShiftVelocities`.
- `Body/RagdollLimb` (on each physical bone): its rigidbody, joint (none on the root), colliders, parent limb and drive
  spring / damper; the bind rotation relative to the parent limb (taken on `Awake`) and the current
  `LimbPoseTarget` (rotation relative to the parent limb, world forward / up, world offset from the root limb,
  height above the animator's origin). Reports its contacts through `Touching`. Limbs are listed parents first.
- `Body/AnimatedPoseSampler` (on the body root, execution order −95, before any force): takes the animator's graph
  off Unity's update (`Stop` + `DirectorUpdateMode.Manual`), and on each physics step but the first evaluates it by
  `fixedDeltaTime` (turned to `FacingYaw` if the owner sets one, at most at profile `Max Facing Turn Degrees Per
  Second` (0: at once; 540 in `PlayerRagdollProfile` since 2026-11-26, so a fast spin of the look does not whirl the
  body and fling its legs out); the graph is taken again each time the animator is turned on), with the optional rig in it (`Rig`: an Animation Rigging `RigBuilder` on the animator's object, kept off, built into the animator's own graph each time the graph is taken, so one evaluation runs the animation and then the constraints; a rig graph of its own evaluated apart starts from the avatar's default pose with the hips at the origin and the body sinks to sit; before each evaluation `RigEvaluating` passes the previous sample's offset from the root limb's rigidbody to the animated root limb, for world targets, then the rig's weights are synced); the rig's nodes and outputs are taken out of the animator's graph again (everything it added after the animator's own outputs) before the rig is rebuilt, when the sampler is turned off (the body falls asleep) and when it is destroyed, and the graph is taken anew when the animator dropped it: `RigBuilder.Clear` frees its jobs but leaves its nodes in a graph it did not create, and evaluating or rebinding that graph crashed the player (2026-11-26, build `2026-11-26-slap`), captures every limb's target, then puts every limb back on its rigidbody. Root motion is off and the animator always animates (no culling: the pose is the body's target, needed off screen
  too).
  Animator parameters are set by the body's owner in `Update`; they apply at the next evaluation.
- `Body/AnimatedPoseFollower` (execution order −90, after the sampler): per limb, the joint drive target (target
  rotation relative to the parent limb, turned into the joint's space against the bind rotation), a torque toward
  the animated forward / up (the root too: this keeps the body upright) and, except the root, a spring force toward
  physical root + animated offset, damped by the limb's speed relative to the root, with half of it back on the
  parent and half on the root (so the damping calms the sway without slowing the whole body). Profile `Pose Force`,
  `Pose Damping` (12 since 2026-11-26, was none: the pelvis sway dies out sooner) and `Pose Torque`, all times the
  control factor. `Root Swing Damping` slows only the root limb's (the hips') spin against the spin of its animated
  target (from the target's turn since the last step; a turn over 45° in a step counts as a jump, not a spin), so the
  hips stop swinging like a pendulum (left / right and tilting) sooner while turning toward the look keeps its speed
  (0 by default, 100 in `PlayerRagdollProfile` since 2026-11-26). A limb sums forces with `AddForce` /
  `AddAcceleration`.
- `Ground/RagdollGroundSensor` (−85): each step takes the highest standable sample (slope, support body still,
  kinematic or heavy, not its own) from the `SupportProbe` cast down from the root limb (animated root height + extra)
  and from the limbs' contacts of the last simulation; with none, contacts whose normals point up on average count
  (wedged). `IsGrounded`, `GroundPoint`, `GroundNormal`, `Carrier`, `SecondsSinceGrounded`; `PauseStanding`
  (profile `Stand Pause After Jump`, 0.3 s) turns standing off for a while so a jump is not held down.
- `Ground/RagdollHover` (−80): while grounded, pushes the hovered limbs up / down toward the height limb's animated
  height above the ground point. On the step it lands, `Ground/LandingSag` lowers that height for a moment by the
  landing speed times `Landing Sag Per Speed` (0.03 m per m/s, at most `Max Landing Sag` 0.3 m): the trunk sinks over
  the first `Landing Sag Sink Share` (0.3) of `Landing Sag Seconds` (0.35 s) and comes back up over the rest (profile
  `Stand`, read every step; added 2026-11-26 so a landing sags as the capsule's hover spring did). When the legs (the
  limbs that are not hovered) no longer reach the floor (their lowest collider point more than `Leg Reach Tolerance`
  0.05 m over the ground point, e.g. flung out by a spin), the held height drops by that gap over `Leg Reach
  Smoothing` (0.1 s), at most `Max Leg Reach Drop` (0.6 m), and rises back at `Leg Reach Rise Speed` (0.5 m/s) once a
  leg reaches the floor, so the body sits onto its legs instead of floating.
- `Ground/RagdollGravity` (−80): Unity gravity only below the limp threshold; in the air its own gravity ramps up by
  time since the ground, or plain world gravity with `UsesPlainGravityInAir` (an owner with its own jump rules);
  none while grounded.
- `Ground/RagdollDrag` (−75): scales every limb's velocity each step by the kept share, lerped by the control factor;
  an owner that sets the walking speed itself turns off `DampsHorizontal` and `DampsInAir` (the spin and, on the
  ground, the vertical stay damped).
- `Ground/RagdollCarrierRide` (−150) + `Ground/RagdollCarrierRideStepEnd` (1000): riding a moving `ICarrier`
  (platform, deck), the capsule's `CarrierRider` scheme per limb: boards what the ground sensor stands on and keeps
  it in the air while the carrier `Holds` the root limb; at the step end each limb's velocity gets the carrier's move
  of its point over the step and is turned with the carrier, at the next step start that is taken off, so between
  them velocities are relative to the carrier; boarding takes the carrier's point speed off, leaving gives it back;
  a carry over 5 m in one step moves the limbs at once. `Carrier`. It is an `ICarrierRider`: a network copy
  (`RideAsCopy`) rides the carrier its owner's state names (`SetCopyCarrier`) instead of what it stands on.
- `Testing/RagdollRootPin`: test helper, makes the root limb kinematic so the body hangs in the air (off by default).
- `Body/RagdollPoseGizmo`: white = the animated skeleton hung on the physical root limb, red = physical limb to its
  target.
- Editor: `RagdollBodyInspector` (buttons "Fill profile from Humanoid avatar", "Bake limbs", "Remove limbs"),
  `RagdollBodyBaker` (a limb's colliders are its capsule plus solid colliders the model already has on the bone, short
  of the next limb; removing takes off only the capsule), `LimbCapsuleShaper`, `HumanoidProfileFiller` (15 limbs for a Humanoid avatar: hips, spine,
  chest, head, thigh / shin / foot, upper arm / forearm / hand; the trunk is hovered, the head is the height limb; radii and lengths scale with the hips-to-head
  distance), `LobbyTestDummyPlacer` (Tools > Ragdoll > Place Test Dummy In Lobby: a baked humanoid
  `RagdollTestDummy` 2 m in front of the lobby spawn, as large as the humanoid in `DummyPlayer`, profile
  `GameResources/HumanoidRagdollProfile`, with the pose sampler, the pose follower, the ground sensor, hover,
  gravity, drag, the gizmo, the root pin and a throwaway animator from
  `TestDummyAnimatorBuilder`: `GameResources/RagdollTestDummy.controller`, float `Forward` blends idle (0) to walking
  forward (1), clips from `Global/Packages/KevinIglesias/HumanAnimations`; running it again replaces the dummy).
  A test helper; the dummy itself was taken out of the lobby again once the ragdoll moved into the player.

## Setting up a body
1. Put the model under a root object, add `RagdollBody`, create a body profile and assign it.
2. For a Humanoid avatar press "Fill profile from Humanoid avatar"; for another creature fill the limbs by hand.
3. Press "Bake limbs". Baking again replaces what the last bake added.
