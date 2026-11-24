namespace Features.BodySyncModule.Packets {
    // Lets through only packets newer than the last one let through. Unreliable packets can arrive late or out of
    // order, and applying an older one would pull the copy back. The number wraps from 65 535 to 0 (after about
    // 36 minutes at 30 packets a second): a number counts as newer when it is less than half the range ahead.
    public sealed class PacketSequenceFilter {
        private const int HALF_RANGE = (ushort.MaxValue + 1) / 2;

        private bool _hasLast;
        private ushort _last;

        public bool TryAccept(ushort sequence) {
            if (_hasLast && IsNewer(sequence, _last) == false)
                return false;

            _hasLast = true;
            _last = sequence;
            return true;
        }

        // For a new owner or after a respawn: the next packet is accepted whatever its number.
        public void Reset() =>
            _hasLast = false;

        private static bool IsNewer(ushort sequence, ushort last) {
            ushort ahead = unchecked((ushort)(sequence - last));
            return ahead != 0 && ahead < HALF_RANGE;
        }
    }
}
