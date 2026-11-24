# BodySyncModule — player body sync by state and input

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## The idea
Every player's body is simulated on every peer. The owner simulates their own from the keyboard and about 30 times a
second sends an unreliable, always complete state packet: position and velocity in the axes of the platform they
stand on, walk input, look, flags and a jump counter. The server stamps when the owner took it and forwards it. Every
other peer runs a dynamic copy of that body with the same controller, driven by the input from the last packet, so
the copy keeps moving between packets and never waits for one. Packets only correct the copy softly:
- the target is the packet's position moved ahead along its velocity by the packet's age, capped;
- horizontally a share of the error goes each physics step;
- vertically there is a dead zone, so the network does not fight local jumps, steps and falls; a copy stuck in the
  wrong grounded state for a while is pulled without it;
- velocity is reconciled, never overwritten;
- a very large error snaps, and the visual hides the jump.

Rare events (death, seat, platform change, hits) go reliably and apply at once.

## Status
Being built in steps on `demo/platform-sync`, in the lobby first (the still world as the only platform); the ship deck
comes later. Done: the pure math and the packet stream. Every player's `BodyStateChannel` sends and receives packets,
but nothing drives the copies from them yet: the players still use `NetworkRigidbodyUnreliable` and `ShipRider`.

## Assembly
`Features.BodySyncModule` (layer 3; references Mirror, Zenject, `CharacterMovableModule`, `FloatingControllerModule`,
`DebugModule.Contracts`).
- `BodyStateChannel` (on `DummyPlayer`): the owner samples its body each physics step and sends a packet when
  `BodySendSchedule` says so (`SendRate` while anything moves or changes, `IdleSendRate` while it stands); the
  command and the RPC go on the unreliable channel. The server stamps `ServerTime` = its time minus half the owner's
  round trip, keeps the packet for its own copy and forwards it to everyone else (the host's own player goes out from
  the server directly). Each non-owner keeps the newest packet (`Latest`) through `PacketSequenceFilter` and counts
  what arrives in `BodyStateReceiveStats`. Packets are in world axes for now (platform 0).
- `BodyStateChannelRegistry`: the channels spawned on this peer.
- `Sending/`: `BodyStateSampler` (packet from the body, the owner's `IMoveInputSource`, the look, support and
  `CharacterMovableBase.JumpCount`), `BodySendSchedule`.
- `Receiving/`: `BodyStateReceiveStats` + `BodyStateReceiveReport`.
- `Diagnostics/BodySyncDebugSystem` (editor and development builds): the debug window's Network section button "Body
  sync: stats" writes per other player to the console: packets a second, dropped, average and last age.
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
  velocity reconciliation per axis, ground mismatch timer, send schedule (moving, idle, a change after idle), receive
  stats.
