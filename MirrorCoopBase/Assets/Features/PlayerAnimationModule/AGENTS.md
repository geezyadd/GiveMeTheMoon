# PlayerAnimationModule — the player's animator

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## The idea
The player's body is an active ragdoll ([ActiveRagdollModule](../ActiveRagdollModule/AGENTS.md),
[PlayerBodyModule](../PlayerBodyModule/AGENTS.md)) that follows an animated pose. This module owns that animation: the
animator controller and everything that sets its parameters. The ragdoll only samples the animator once a physics step
and follows the pose; how the pose is turned (the look, flying head first) stays with `PlayerRagdollBody`.

## Status
Being built on `demo/active-ragdoll`. Parameters: `Forward`, `Fly`, `Slap`, `EmoteStop` and one trigger per emote of
the emote catalog ([EmoteModule](../EmoteModule/AGENTS.md)). The clips are the KevinIglesias idle, walk and fall and our
own `PlayerSlap`; no jump, sprint or seated clips yet; the emotes' clips are placeholders. Every player sees every slap
and every emote: their counts ride the replica packets (AttackModule, EmoteModule).

## Assembly
`Features.PlayerAnimationModule` (layer 3; references Zenject, `CharacterMovableModule`, `ActiveRagdollModule`, `Unity.Animation.Rigging`).
Editor code in `Features.PlayerAnimationModule.Editor` (also references `EmoteModule.Data` and the Addressables
constants, to build the Emote layer from the emote catalog).
- `PlayerAnimatorParameters`: every parameter's name (what the controller is built with) and hash (what the driver
  sets). A new parameter is a line here, a line in `PlayerAnimatorBuilder` and a line in `PlayerAnimationDriver`.
- `PlayerAnimationDriver` (on `DummyPlayer`, references the movement and the ragdoll): every frame, on every peer that
  simulates the body (the owner and the copies of the others, nothing extra over the network), while the ragdoll is
  awake (its animator is on):
  - `Forward` (float 0..1): the walk input of the active controls, not the measured speed (on a moving carrier the body
    always drifts a little); under `ForwardIdleCutoff` it is 0; damped by `ForwardDampingSeconds`; 0 while held (a seat).
  - `Fly` (bool): the ragdoll's ground sensor has seen no ground for `FlyDelaySeconds` (`AirborneDelay`), so a step or
    a bump does not turn it on; off at once on the ground; never while held.
  - An action's trigger (`Slap`) fires through `Play(triggerHash)`, called by AttackModule's `Attacker` when an
    attack starts; `CanPlay` says whether the animator runs (the ragdoll is awake), and nothing fires while it does
    not. The presses, who may attack and when are AttackModule's. An emote's trigger (EmoteModule's `EmotePlayer`) goes
    the same way; `ResetTrigger(triggerHash)` takes back a trigger not taken yet (an emote stopped in the frame it
    started, a stale `EmoteStop`).
  - `IsWalking` (the walk input was above `ForwardIdleCutoff` at the last update) and `IsFlying` (`Fly` was on): what
    the last update saw, false while the animator does not run; an emote stops on them.
- `AirborneDelay`: the delay itself, pure.
- `Reach/LimbReach` (on the ragdoll's root, next to its `AnimatedPoseSampler`; refs: the sampler and its chains): lets
  any system point a limb at something over the animation (a slap's hand at a head; later a hand on an item, a hand
  dragged by something, the head looking at a thing), on any humanoid ragdoll that has the chains. A system takes a
  `LimbReachRequest` (`Request(LimbId, priority)`), sets its `Weight` (0..1) and goal (`SetGoal`: a transform with a
  local offset, or a world point) every physics step and calls `Release` when done. On every sample
  (`AnimatedPoseSampler.RigEvaluating`) each chain follows its winner (`LimbReachRule.PickWinner`: the highest priority
  among the requests for that limb with a weight above 0 and a goal, the latest on a tie; a waiting request at weight 0
  does not take the limb), its shown weight eased (`Blend Seconds` 0.08 for the whole 0 → 1) and its goal smoothed
  (`Goal Smoothing Seconds` 0.03; snapped when the weight was 0), the goal moved by the offset between the real and the
  animated root limb. A release eases the limb back to the animation, keeping its last goal. One step after a request
  changes, unless its owner runs before the sampler (−95).
  - `Reach/LimbId`: `RightHand`, `LeftHand`, `Head`. A body reaches only with the limbs it has a chain for (a request
    for another is logged as an error and never shown); `DummyPlayer` has only `RightHand` so far.
  - `Reach/LimbReachChain` (serialized in `LimbReach`): a limb, its rig constraint (any `IRigConstraint`: a
    `TwoBoneIKConstraint` for a hand, a `MultiAimConstraint` for the head) and the constraint's target (not under the
    animated model). Only `LimbReach` moves the target and sets the weight.
  - `Reach/LimbReachRule`: the pure winner pick, weight blend and goal smoothing.
  - Physically holding a limb (something dragging a player by the hand) is not here: it will be a joint on the ragdoll
    (ActiveRagdollModule), with a reach request so the pose matches.
- `PlayerAnimationConfiguration` (Addressables `PlayerAnimationConfiguration_Default`, bound in
  `ConfigurationInstaller`): `ForwardDampingSeconds` 0.125, `ForwardIdleCutoff` 0.125, `FlyDelaySeconds` 0.3.
- `GameResources/PlayerAnimator.controller`: built from code, never edited by hand. Four layers: `Base Layer` with
  `Locomotion` (1D blend by `Forward`, idle 0 → walk 1), and `Fly` (override, weight 0 by default) with only `Fall`.
  `PlayerAnimationDriver` fades the `Fly` layer's weight to 1 while `Fly` is on (`FlyBlendInSeconds` 0.15) and back to 0
  when it is off (`FlyBlendOutSeconds` 0.2). No empty state: an empty state in an override layer freezes the layer's
  last pose (and with written defaults pulls the body to its rest pose). `Emote` (override, no mask, weight 1):
  `Empty` (default, a blend tree with no motions, as in `Slap`) and one state per emote of the emote catalog, named by
  its trigger, playing the catalog entry's clip (the idle without one); Any State goes to an emote on its trigger (no
  exit time, can go to itself), the emote goes back to `Empty` at its exit time or on `EmoteStop` (no exit time); the
  contract and where the real clips go are in [EmoteModule](../EmoteModule/AGENTS.md). Whole body because an emote
  stops as soon as the player walks, jumps or is in the air, and the `Slap` layer above it still shows a slap on the
  upper body. `Slap` (override, mask
  `GameResources/PlayerUpperBody.mask`: trunk, head, arms, fingers, hand IK; not the root and the legs, so the legs
  keep walking or falling; weight 1): `Empty` (default, a blend tree with no motions) and `Slap` (`PlayerSlap`);
  `Empty` goes to `Slap` on the `Slap` trigger (no exit time), `Slap` goes back to `Empty` at its exit time; both
  transitions keep the editor's default exit time and duration.
- `GameResources/Animations/PlayerSlap.anim`: the slap, a right hand, 0.9 s, not looping, humanoid muscle curves only.
  Created once by `SlapClipDraft` (the idle's first 0.9 s; the right shoulder, arm, forearm, hand and the chest and
  spine twist keyed: wind-up at 0.22 s, hit at 0.36 s, follow-through at 0.5 s, back to the idle at 0.9 s); after that it
  is edited by hand in the Animation window (muscles, not bone rotations) and the builder never touches it.
- Editor: `PlayerAnimatorBuilder` (Tools > Animation > Build Player Animator): rebuilds the controller in place (the
  asset keeps its GUID) and puts it on the humanoid in `DummyPlayer`; before that it builds the upper body mask and creates the slap clip if it is missing (created once, then changed in place). The Emote layer comes from the emote catalog (`EmoteCatalog_Default`, read at its address with `AddressableConfigurationLoader`); an emote without a trigger name, or with one that is taken, is logged and left out. The ragdoll baker (PlayerBodyModule) takes the
  controller from here too.

## Tests
- `Features.PlayerAnimationModule.Editor.Tests`: `AirborneDelayTests` (on the ground, a short lift, a long enough one,
  landing ends it at once, short lifts do not add up, zero delay).
  `LimbReachRuleTests` (no weight, another limb, no goal, priority, a waiting higher request, the latest on a tie, a
  released request, the weight blend, the goal smoothing).
