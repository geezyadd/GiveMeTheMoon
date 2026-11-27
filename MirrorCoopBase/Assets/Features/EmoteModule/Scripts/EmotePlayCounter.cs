namespace Features.EmoteModule {
    // Turns an emote counter into plays, pure. A play is the counter changing between two reads while the plays are
    // available (once for any number missed in between: the latest emote shows); the first count read after they were
    // not available is taken as it is, so a copy that starts or a body that wakes does not emote on its own.
    public sealed class EmotePlayCounter {
        private bool _hasCount;
        private byte _count;

        public bool Step(bool isAvailable, byte count) {
            if (isAvailable == false) {
                _hasCount = false;
                return false;
            }

            if (_hasCount == false) {
                _hasCount = true;
                _count = count;
                return false;
            }

            bool isPlayed = count != _count;
            _count = count;
            return isPlayed;
        }
    }
}
