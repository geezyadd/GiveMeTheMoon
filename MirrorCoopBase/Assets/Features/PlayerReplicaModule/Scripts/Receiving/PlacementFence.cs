namespace Features.PlayerReplicaModule.Receiving {
    // On a copy after the server moved the body: packets the owner sent before it got the move would pull the copy back
    // to where the player was. Every move has a number the owner puts into its packets from then on, so the copy waits
    // for a packet with that number. A packet that never comes (lost order, an owner that left) holds it only until the
    // wait runs out.
    public sealed class PlacementFence {
        private byte _awaitedCount;
        private double _startedAt;

        public bool IsWaiting { get; private set; }

        public void Begin(byte placeCount, double now) {
            _awaitedCount = placeCount;
            _startedAt = now;
            IsWaiting = true;
        }

        // Whether a packet with this place count still belongs to the time before the move.
        public bool Blocks(byte placeCount, double now, double waitSeconds) {
            if (IsWaiting == false)
                return false;

            if (placeCount == _awaitedCount || now - _startedAt >= waitSeconds) {
                IsWaiting = false;
                return false;
            }

            return true;
        }

        public void Clear() =>
            IsWaiting = false;
    }
}
