using Features.BodySyncModule.Packets;
using Features.CharacterMovableModule.Scripts.MoveInput;
using UnityEngine;

namespace Features.BodySyncModule.Receiving {
    // A remote copy's controls: the owner's walk direction, sprint and jump hold from the newest state packet, kept
    // until the next one arrives, so a copy keeps walking while packets are late. A jump press comes from the jump
    // counter, once for any number of jumps missed between two packets.
    public sealed class PacketMoveInputSource : IMoveInputSource {
        private readonly JumpCounterTracker _jumps = new();

        private BodyStatePacket _packet;
        private bool _pendingJump;

        public bool SprintHeld => _packet.Has(BodyStateFlags.Sprinting);
        public bool JumpHeld => _packet.Has(BodyStateFlags.JumpHeld);

        public Vector3 ReadWish() =>
            new(_packet.Wish.x, 0f, _packet.Wish.y);

        public bool ConsumeJumpPress() {
            bool pressed = _pendingJump;
            _pendingJump = false;
            return pressed;
        }

        public void Apply(in BodyStatePacket packet) {
            _packet = packet;
            if (_jumps.ConsumeJump(packet.JumpCounter))
                _pendingJump = true;
        }

        // For a copy that starts being simulated: no old input, and the next packet only sets the jump count.
        public void Reset() {
            _packet = default;
            _pendingJump = false;
            _jumps.Reset();
        }
    }
}
