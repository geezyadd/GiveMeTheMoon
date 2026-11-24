namespace Features.PlayerReplicaModule.Packets {
    // Tells a copy when its owner has jumped, from the jump counter of the packets it accepts. However many jumps were
    // missed between two packets, the copy jumps once: replaying several jumps in a row would look wrong. The first
    // packet only sets the start. The counter wraps from 255 to 0.
    public sealed class JumpCounterTracker {
        private const int HALF_RANGE = (byte.MaxValue + 1) / 2;

        private bool _hasLast;
        private byte _last;

        public bool ConsumeJump(byte jumpCounter) {
            if (_hasLast == false) {
                _hasLast = true;
                _last = jumpCounter;
                return false;
            }

            byte ahead = unchecked((byte)(jumpCounter - _last));
            _last = jumpCounter;
            return ahead != 0 && ahead < HALF_RANGE;
        }

        public void Reset() =>
            _hasLast = false;
    }
}
