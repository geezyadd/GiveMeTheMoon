# PlayerAnimationModule — the player's animator

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## The idea
The player's body is an active ragdoll ([ActiveRagdollModule](../ActiveRagdollModule/AGENTS.md),
[PlayerBodyModule](../PlayerBodyModule/AGENTS.md)) that follows an animated pose. This module owns that animation: the
animator controller and everything that sets its parameters. The ragdoll only samples the animator once a physics step
and follows the pose; how the pose is turned (the look, flying head first) stays with `PlayerRagdollBody`.

## Status
Being built on `demo/active-ragdoll`. Parameters: `Forward` and `Fly`. The clips are the KevinIglesias idle, walk and
fall; no jump, sprint or seated clips yet.

## Assembly
`Features.PlayerAnimationModule` (layer 3; references Zenject, `CharacterMovableModule`, `ActiveRagdollModule`).
Editor code in `Features.PlayerAnimationModule.Editor`.
- `PlayerAnimatorParameters`: every parameter's name (what the controller is built with) and hash (what the driver
  sets). A new parameter is a line here, a line in `PlayerAnimatorBuilder` and a line in `PlayerAnimationDriver`.
- `PlayerAnimationDriver` (on `DummyPlayer`, references the movement and the ragdoll): every frame, on every peer that
  simulates the body (the owner and the copies of the others, nothing extra over the network), while the ragdoll is
  awake (its animator is on):
  - `Forward` (float 0..1): the walk input of the active controls, not the measured speed (on a moving carrier the body
    always drifts a little); under `ForwardIdleCutoff` it is 0; damped by `ForwardDampingSeconds`; 0 while held (a seat).
  - `Fly` (bool): the ragdoll's ground sensor has seen no ground for `FlyDelaySeconds` (`AirborneDelay`), so a step or
    a bump does not turn it on; off at once on the ground; never while held.
- `AirborneDelay`: the delay itself, pure.
- `PlayerAnimationConfiguration` (Addressables `PlayerAnimationConfiguration_Default`, bound in
  `ConfigurationInstaller`): `ForwardDampingSeconds` 0.125, `ForwardIdleCutoff` 0.125, `FlyDelaySeconds` 0.2.
- `GameResources/PlayerAnimator.controller`: built from code, never edited by hand. Two layers: `Base Layer` with
  `Locomotion` (1D blend by `Forward`, idle 0 → walk 1), and `Fly` (override, weight 0 by default) with only `Fall`.
  `PlayerAnimationDriver` fades the `Fly` layer's weight to 1 while `Fly` is on (`FlyBlendInSeconds` 0.15) and back to 0
  when it is off (`FlyBlendOutSeconds` 0.2). No empty state: an empty state in an override layer freezes the layer's
  last pose (and with written defaults pulls the body to its rest pose).
- Editor: `PlayerAnimatorBuilder` (Tools > Animation > Build Player Animator): rebuilds the controller in place (the
  asset keeps its GUID) and puts it on the humanoid in `DummyPlayer`. The ragdoll baker (PlayerBodyModule) takes the
  controller from here too.

## Tests
- `Features.PlayerAnimationModule.Editor.Tests`: `AirborneDelayTests` (on the ground, a short lift, a long enough one,
  landing ends it at once, short lifts do not add up, zero delay).
