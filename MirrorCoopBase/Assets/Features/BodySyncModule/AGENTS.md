# BodySyncModule — player body sync by state and input

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## The idea
Every player's body is simulated on every peer. The owner simulates their own from the keyboard and about 30 times a
second sends an unreliable, always complete state packet: position and velocity in the axes of the platform they
stand on, walk input, look, flags and a jump counter. The server stamps when the owner took it and forwards it. Every
other peer runs a dynamic copy of that body with the same controller, driven by the input from the last packet, so
the copy keeps moving between packets and never waits for one. A copy lives a little in the past (the copy delay,
0.1 s): a packet's controls and position apply together when the show time reaches its stamp, so the copy stops where
its owner stopped instead of running past and sliding back. Packets only correct the copy softly:
- the target is the packet's position, moved ahead along its velocity by the packet's age (capped) only while the
  owner presses a walk key;
- horizontally a share of the error goes each physics step; while the owner presses no walk key, a small error is
  left alone and a larger one goes only gently;
- vertically there is a dead zone, so the network does not fight local jumps, steps and falls; a copy stuck in the
  wrong grounded state for a while is pulled without it;
- velocity is reconciled, never overwritten;
- a very large error snaps, and the visual hides the jump.

Rare events (death, seat, platform change, hits) go reliably and apply at once.

## Status
Being built in steps on `demo/platform-sync`, in the lobby first (the still world as the only platform); the ship deck
comes later. Done: the pure math, the packet stream and the simulated copies behind a per-peer debug toggle ("Player
sync: old / new", `BodySyncModeModel`, new by default). The old way stays one press away: copies follow
`NetworkRigidbodyUnreliable`; riders always follow `ShipRider`. Not done yet: hiding a snap visually, the look from the packet,
the ship deck as a moving platform, removing the proxy shove.

Known limits of the toggle: on the host, the new mode silences the old network body of the other players, so a third
player in the old mode sees them stand still (two players are fine). Switching to the old mode while a copy rides waits
until the ride ends.

## Assembly
`Features.BodySyncModule` (layer 3; references Mirror, Mirror.Components, Zenject, `CharacterMovableModule`,
`FloatingControllerModule`, `DebugModule.Contracts`).
- `BodyCopyDriver` (on `DummyPlayer`, runs after the float probe and before the movement, so a packet's controls
  and the reconciled velocity apply on the step they are due): on a non-owner in the new mode it silences
  `NetworkRigidbodyUnreliable`, makes the body dynamic, turns on `FloatingController.SimulatesCopy` and gives
  `CharacterMovableBase.SetRemoteInput` a `PacketMoveInputSource` (the newest packet's controls, a jump press from the
  jump counter). Packets wait in a `PacketDelayBuffer` until the show time (server time minus `CopyDelaySeconds`)
  reaches their stamp; the current one gives the controls and the target. After each physics step it pulls the body
  toward the target (`PositionCorrection`), reconciles the velocity once per packet (only slowing the copy while the owner presses no
  walk key) and snaps past the snap distance (counted in the
  stats). A body that turns kinematic (ride, seat) is left alone until it is dynamic again; a copy held by a ride
  when the mode is switched on is taken over only after the ride. The server's copy pushes items by contact, so the
  proxy shove does not run for a simulated copy.
- `BodyStateChannel` (on `DummyPlayer`): the owner samples its body each physics step and sends a packet when
  `BodySendSchedule` says so (at once when a control changes: walk pressed or released, sprint, jump held, support, a
  jump; `SendRate` while anything moves or the look turns; `IdleSendRate` while it stands); the
  command and the RPC go on the unreliable channel. The server stamps `ServerTime` = its time minus half the owner's
  round trip, keeps the packet for its own copy and forwards it to everyone else (the host's own player goes out from
  the server directly). Each non-owner keeps the newest packet (`Latest`) through `PacketSequenceFilter` and counts
  what arrives in `BodyStateReceiveStats`. Packets are in world axes for now (platform 0).
- `BodyStateChannelRegistry`: the channels spawned on this peer.
- `Sending/`: `BodyStateSampler` (packet from the body, the owner's `IMoveInputSource`, the look, support and
  `CharacterMovableBase.JumpCount`), `BodySendSchedule`.
- `Receiving/`: `PacketMoveInputSource`, `PacketDelayBuffer`, `BodyStateReceiveStats` + `BodyStateReceiveReport` (also counts snaps).
- `Diagnostics/` (editor and development builds), in the debug window's Network section: `BodySyncDebugSystem`
  ("Body sync: stats": per other player to the console, packets a second, dropped, snaps, average and last age) and
  `BodySyncModeDebugSystem` ("Player sync: old / new").
- `Models/BodySyncModeModel`: this peer's sync mode, bound in `DataInstaller`.
- `Configuration/BodySyncConfiguration` (Addressables `BodySyncConfiguration_Default`, bound in
  `ConfigurationInstaller`): send rates and the correction numbers.
- `Packets/`: `BodyStatePacket` (public fields so the network layer serializes it), `BodyStateFlags`,
  `YawCompression` (look angle in two bytes), `PacketSequenceFilter` (drops older and out-of-order packets, number
  wraps), `JumpCounterTracker` (a copy jumps once for any number of missed jumps).
- `Correction/`: `BodyCorrectionSettings` (filled from the configuration),
  `CorrectionTarget`, `PositionCorrection` + `PositionCorrectionStep`, `VelocityReconciliation` (per axis: scale down
  to a slower owner keeping direction, add only the missing speed toward a faster one), `GroundMismatchTimer`.

## Tests
- `Features.BodySyncModule.Editor.Tests`: yaw packing and wrap, flags, sequence order and wrap, jump counter (missed
  jumps, wrap), target extrapolation and its cap, horizontal share, vertical dead zone and cap, no overshoot, snap,
  velocity reconciliation per axis (and only slowing while the owner steers nothing), ground mismatch timer, send schedule (moving, idle, a change after idle), receive
  stats, packet input source (controls, jump press from the counter, reset), delay buffer (waits, newest due one,
  restart), the gentle pull while the owner stands, sending at once when a control changes.
