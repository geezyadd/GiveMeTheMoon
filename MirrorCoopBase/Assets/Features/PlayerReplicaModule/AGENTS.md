# PlayerReplicaModule — player body sync by state and input

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

Rare events (death, seat, platform change, hits) go reliably and apply at once. So does a move of the body by the
server (a respawn, a map change, a world shift): every peer moves the body at once, the owner its own; each move has a
number the owner's packets carry from then on, and a copy holds still at the new place until the first packet sent
after the move, so it is never pulled back to where the player was.

## Status
Done on `demo/platform-sync`: the pure math, the packet stream and the simulated copies (the only way other players
are shown; the old network body of the player and its "Player sync: old / new" toggle were removed on 2026-11-24),
the server's moves of a body through the packets (`IPlayerBodyPlacement`), and carriers: on a moving carrier
(`ICarrier`: the lobby test platform, the flying ship's deck) the packet is in the carrier's axes and copies ride their
own copy of it. A seated player is still bound to its seat by `ShipRider` (kinematic); its copy leaves it alone.
Not done yet: removing the proxy shove.

## Assembly
`Features.PlayerReplicaModule` (layer 3; references Mirror, Mirror.Components, Zenject, `CharacterMovableModule`,
`FloatingControllerModule`, `GameCoreModule.Contracts`, `DebugModule.Contracts`).
- `BodyCopyDriver` (on `DummyPlayer`, runs after the float probe and before the movement, so a packet's controls
  and the reconciled velocity apply on the step they are due): on a non-owner it holds the body still (kinematic)
  until the first packet, then makes it dynamic, turns on `FloatingController.SimulatesCopy` and gives
  `CharacterMovableBase.SetRemoteInput` a `PacketMovementControls` (the newest packet's controls, a jump press from the
  jump counter). Packets wait in a `PacketDelayBuffer` until the show time (server time minus `CopyDelaySeconds`)
  reaches their stamp; the current one gives the controls and the target. After each physics step it pulls the body
  toward the target (`PositionCorrection`), reconciles the velocity once per packet (only slowing the copy while the owner presses no
  walk key) and snaps past the snap distance (counted in the
  stats). A body a seat holds (kinematic, collisions off) is left alone until it is dynamic again. After a move by the
  server (`BodyStateChannel.OnPlaced`) the copy stops, holds still at the new place and starts over from the first
  packet with the move's number (`PlacementFence`; at most `PlacementWaitSeconds`, 1 s). The server's copy pushes
  items by contact, so the proxy shove does not run for a simulated copy.
  On a carrier: `CopyCarrierFrame` finds this peer's copy of the packet's carrier by network id (missing: the body is
  left alone), the copy rides it through `ICarrierRider.SetCopyCarrier` (`RideAsCopy` while simulating), the target and
  the owner's velocity are turned from the carrier's axes (its pose of the last step), and the walk direction
  (`PacketMovementControls.SetHeading`) and the look follow the carrier's heading.
- `Visual/ReplicaVisualLag` (on `DummyPlayer`): moves the `ReplicaVisual` child (everything seen: `RotatablePart` with
  the model and the hand, `Nameplate`, the spectator camera points) by a `VisualLagOffset`, so a jump of a copy's body
  shows as a quick slide: the driver reports every snap and every pull step of at least `VisualCatchUpThreshold`; the
  offset fades to 5 % over `VisualCatchUpSeconds`; a jump past `VisualCatchUpMaxDistance` (a respawn) is shown as it
  is. The colliders stay with the body. The copy's look comes from the current packet
  (`CharacterMovableBase.SetRemoteLook`), so it turns at the same moment it walks.
- `BodyStateChannel` (on `DummyPlayer`): the owner samples its body each physics step and sends a packet when
  `BodySendSchedule` says so (at once when a control changes: walk pressed or released, sprint, jump held, support, a
  jump; `PacketRate` while anything moves or the look turns; `RestingPacketRate` while it stands); the
  command and the RPC go on the unreliable channel. The server stamps `ServerTime` = its time minus half the owner's
  round trip, keeps the packet for its own copy and forwards it to everyone else (the host's own player goes out from
  the server directly). Each non-owner keeps the newest packet (`Latest`) through `PacketSequenceFilter` and counts
  what arrives in `BodyStateReceiveStats`. It is also the player's `IPlayerBodyPlacement` (contract in
  `GameCoreModule.Contracts`): `ServerPlace(pose)` (at rest, off any carrier: `ICarrierRider.DropCarrier`) and
  `ServerShift(delta)` (velocity kept) move the body on the server at once and on every client by a reliable RPC (the
  owner too, which moves the body it simulates), with the interpolation dropped for the jump; the move's number
  (`PlaceCount`) goes into every packet after it. Callers: the respawn (`PlayerLifeBody.ServerTeleport`), the map
  change (`ConnectionSpawnPlacement.TeleportEverybody`). A player that spawns or rejoins needs none: it spawns at its
  pose on every peer, and its copies hold still there until its first packet.
- `BodyStateChannelRegistry`: the channels spawned on this peer.
- `Sending/`: `BodyStateSampler` (packet from the body, the owner's `IMovementControls`, the look, support and
  `CharacterMovableBase.JumpCount`; on a carrier the position and velocity in its axes of the last step, the walk and
  look in its heading, and its id as `CarrierId`), `BodySendSchedule`.
- `Receiving/`: `PacketMovementControls`, `PacketDelayBuffer`, `CopyCarrierFrame`, `PlacementFence`, `BodyStateReceiveStats` + `BodyStateReceiveReport` (also counts snaps).
- `Diagnostics/` (editor and development builds), in the debug window's Network section: `ReplicaStatsDebugSystem`
  ("Replicas: stats": per other player to the console, packets a second, dropped, snaps, average and last age).
- `Configuration/ReplicaConfiguration` (Addressables `ReplicaConfiguration_Default`, bound in
  `ConfigurationInstaller`): send rates, the placement wait and the correction numbers.
- `Packets/`: `BodyStatePacket` (public fields so the network layer serializes it), `BodyStateFlags`,
  `YawCompression` (look angle in two bytes), `PacketSequenceFilter` (drops older and out-of-order packets, number
  wraps), `JumpCounterTracker` (a copy jumps once for any number of missed jumps).
- `Correction/`: `BodyCorrectionSettings` (filled from the configuration),
  `CorrectionTarget`, `PositionCorrection` + `PositionCorrectionStep`, `VelocityReconciliation` (per axis: scale down
  to a slower owner keeping direction, add only the missing speed toward a faster one), `GroundMismatchTimer`.

## Tests
- `Features.PlayerReplicaModule.Editor.Tests`: yaw packing and wrap, flags, sequence order and wrap, jump counter (missed
  jumps, wrap), target extrapolation and its cap, horizontal share, vertical dead zone and cap, no overshoot, snap,
  velocity reconciliation per axis (and only slowing while the owner steers nothing), ground mismatch timer, send schedule (moving, idle, a change after idle), receive
  stats, packet input source (controls, jump press from the counter, reset), delay buffer (waits, newest due one,
  restart), the gentle pull while the owner stands, sending at once when a control changes, the visual lag offset
  (hide, tiny and huge jumps, fade, sum), the placement fence (old packets blocked, the move's packet and the
  ones after it pass, the wait runs out, the number wraps, clear).
