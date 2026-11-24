# MovingPlatformModule — moving platforms

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## Status
A test platform for walking and jumping on something that moves, before the ship's deck works that way. Players do not
ride it yet: one standing on it slides off as it moves (riding comes with the platform carry in
[PlayerReplicaModule](../PlayerReplicaModule/AGENTS.md)'s next steps).

## Assembly
`Features.MovingPlatformModule` (layer 4; references Mirror, `GrabModule`).
- `CirclingPlatform`: flies around a circle and faces along its way. Synced: running or not, and the angle and server
  time of the last start or stop (SyncVars); every peer computes the pose from `NetworkTime.time` with `CirclePath`, so
  there is no buffer and no jitter. The pose is set each physics step on a kinematic, interpolated `Rigidbody`
  (`MovePosition` / `MoveRotation`); the server time between frames is the frame's `NetworkTime.time` offset from the
  local time. `ServerToggle` starts or stops it where it is.
- `PlatformToggleButton` (`InteractableBase`): aim + E on the post toggles the platform on the server.
- `CirclePath`: the angle at a time (wraps, precise after hours), the position on the circle and the facing.
- `GameResources/Prefabs/TestPlatform`: root (`NetworkIdentity`, `CirclingPlatform`: centre (-3, 0.2, -1.5), radius
  20 m, 5 m/s, a lap in about 25 s), child `Platform` (6 x 0.4 x 6 m cube on the Ground layer, so the float probe
  stands on it) and child `PlatformButton` (post on the Interactable layer next to the lobby spawn). Placed in
  `LobbyScene` as a scene object (its own `sceneId`); the circle goes around the lobby floor at floor height.

## Tests
- `Features.MovingPlatformModule.Editor.Tests`: `CirclePathTests` (angle while running and stopped, wrap, hours of
  running, position on the circle, facing along the way).
