namespace Features.ShipModule.Scripts.Overboard {
    // One player as the server sees it against the ship this tick.
    public readonly struct ShipOverboardState {
        public bool IsFlying { get; }
        public bool IsOverboard { get; }
        public bool HasRiddenThisFlight { get; }
        public bool IsRiding { get; }
        public bool IsInShipVolume { get; }
        public float DriftSeconds { get; }
        public float DistanceMeters { get; }

        public ShipOverboardState(bool isFlying, bool isOverboard, bool hasRiddenThisFlight, bool isRiding, bool isInShipVolume, float driftSeconds, float distanceMeters) {
            IsFlying = isFlying;
            IsOverboard = isOverboard;
            HasRiddenThisFlight = hasRiddenThisFlight;
            IsRiding = isRiding;
            IsInShipVolume = isInShipVolume;
            DriftSeconds = driftSeconds;
            DistanceMeters = distanceMeters;
        }
    }
}
