# MovingPlatformModule — moving platforms

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## Status
A test platform for walking and jumping on something that moves, before the ship's deck works that way. The player's
own body rides it (`CarrierRider`); other players' copies do not yet: their state packets are still in world axes, so
a copy on the platform is pulled to where its owner was 0.1 s ago in the world (next: packets in the carrier's axes).

## Assembly
`Features.MovingPlatformModule` (layer 4; references Mirror, Zenject, `GrabModule`, `CameraModule`,
`FloatingControllerModule`).
- `CarrierRider` (on `DummyPlayer`, runs before the float probe) + `CarrierRiderStepEnd` (runs last in the step): for
  the owner's dynamic body. Boards the `FloatingController.SupportCarrier`, stays on it in the air while
  `ICarrier.Holds` the body, leaves when not. The carry goes through the velocity, so the physics moves the body and
  its interpolation stays smooth (setting the position directly made it jerk): at the step's end the carrier's exact
  move of the body's point (`CarrierMotion.CarryPoint` from `PoseBefore` to `PoseNow`) over the step time is added
  and the relative velocity is turned with the carrier; at the next step's start the addition is taken off, so the
  spring and the movement see the velocity relative to the carrier. Boarding takes the carrier's point velocity off,
  leaving gives it back. The local player's camera gets the turn of the carrier as drawn each frame
  (`ICarrier.DrawnRotation`) through `CameraCarryTurnModel`. A step longer than 5 m is not carried.
- `CirclingPlatform` (`ICarrier`, runs before the riders): flies around a circle and faces along its way; its carrier
  id is its `netId`, `Holds` is the box over it (6 m up, 0.3 m past the edges). Synced: running or not, and the angle and server
  time of the last start or stop (SyncVars); every peer computes the pose from `NetworkTime.time` with `CirclePath`, so
  there is no buffer and no jitter. The pose is set each physics step on a kinematic, interpolated `Rigidbody`
  (`MovePosition` / `MoveRotation`); the server time between frames is the frame's `NetworkTime.time` offset from the
  local time. `ServerToggle` starts or stops it where it is.
- `PlatformToggleButton` (`InteractableBase`): aim + E on the post on the deck starts or stops the platform on the
  server.
- `CirclePath`: the angle at a time (wraps, precise after hours), the position on the circle and the facing.
- `PlatformSway`: the rocking on top of the heading, a sine of the server time: roll from side to side (12 degrees),
  pitch a quarter period behind (6 degrees), 3 s a rock; it goes on while the platform stands. The heading (yaw) stays
  the circle's, so the camera turns only with the circle.
- `GameResources/Prefabs/TestPlatform`: root (`NetworkIdentity`, `CirclingPlatform`: centre (-3, 0.2, -1.5), radius
  20 m, 9 m/s, a lap in about 14 s; stands at the start) and child `Platform` (the kinematic body with a 6 x 0.4 x 6 m
  box collider on the Ground layer, so the float probe stands on it; `Holds` uses that box). Under `Platform`: `DeckMesh`
  (the scaled cube that is seen) and `PlatformButton` (the switch post on the deck, Interactable layer), so the switch
  rides along. Placed in `LobbyScene` as a scene object (its own `sceneId`); the circle goes around the lobby floor at
  floor height.

## Tests
- `Features.MovingPlatformModule.Editor.Tests`: `CirclePathTests` (angle while running and stopped, wrap, hours of
  running, position on the circle, facing along the way), `PlatformSwayTests` (full roll, pitch at the start, heading
  kept, no period), `CarrierMotionTests` (carry by a move and by a turn, yaw
  across zero, point velocity, local and back).
