using Features.PlayerReplicaModule.Packets;
using Features.CharacterMovableModule.Scripts.MovementControls;
using UnityEngine;

namespace Features.PlayerReplicaModule.Receiving {
    // A remote copy's controls: the owner's walk direction, sprint and jump hold from the newest state packet, kept
    // until the next one arrives, so a copy keeps walking while packets are late. A jump press comes from the jump
    // counter, a slap press from the slap counter, each once for any number missed between two packets.
    public sealed class PacketMovementControls : IMovementControls {
        private readonly JumpCounterTracker _jumps = new();
        // The same counter logic as the jumps.
        private readonly JumpCounterTracker _slaps = new();

        private BodyStatePacket _packet;
        private bool _pendingJump;
        private bool _pendingSlap;
        private Quaternion _heading = Quaternion.identity;

        public bool SprintHeld => _packet.Has(BodyStateFlags.Sprinting);
        public bool JumpHeld => _packet.Has(BodyStateFlags.JumpHeld);
        public byte SlapCount => _packet.SlapCount;

        // The walk direction comes in the carrier's heading; it is turned into the world's here.
        public Vector3 ReadWish() =>
            _heading * new Vector3(_packet.WalkDirection.x, 0f, _packet.WalkDirection.y);

        // The heading of the carrier the packet is in (identity on the still world), updated each physics step.
        public void SetHeading(Quaternion heading) =>
            _heading = heading;

        public bool ConsumeJumpPress() {
            bool pressed = _pendingJump;
            _pendingJump = false;
            return pressed;
        }

        public bool ConsumeSlapPress() {
            bool pressed = _pendingSlap;
            _pendingSlap = false;
            return pressed;
        }

        public void Apply(in BodyStatePacket packet) {
            _packet = packet;
            if (_jumps.ConsumeJump(packet.JumpCount))
                _pendingJump = true;
            if (_slaps.ConsumeJump(packet.SlapCount))
                _pendingSlap = true;
        }

        // For a copy that starts being simulated: no old input, and the next packet only sets the jump and slap counts.
        public void Reset() {
            _packet = default;
            _pendingJump = false;
            _pendingSlap = false;
            _heading = Quaternion.identity;
            _jumps.Reset();
            _slaps.Reset();
        }
    }
}
