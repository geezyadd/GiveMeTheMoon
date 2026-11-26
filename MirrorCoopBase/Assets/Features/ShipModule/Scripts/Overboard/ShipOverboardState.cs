namespace Features.ShipModule.Scripts.Overboard {
    // One player as the server sees it this tick. In flight the safe space is the flying ship's volume; on the ground
    // (the lobby, a ship standing at a station) it is any overboard area or the standing ship's volume.
    public readonly struct ShipOverboardState {
        public bool IsFlying { get; }
        public bool IsOverboard { get; }
        // Has been in the safe space since the ship last took off or landed (in flight: rode the ship).
        public bool HasBeenInside { get; }
        // The flying ship carries the body or a seat holds it.
        public bool IsRiding { get; }
        public bool IsInSafeSpace { get; }
        // On the ground: right over a floor or the standing ship's deck, where a drift can end without a fall.
        public bool IsOverFloor { get; }
        public float DriftSeconds { get; }
        // In flight from the ship, on the ground from the nearest overboard area.
        public float DistanceMeters { get; }

        public ShipOverboardState(bool isFlying, bool isOverboard, bool hasBeenInside, bool isRiding, bool isInSafeSpace,
            float driftSeconds, float distanceMeters, bool isOverFloor = false) {
            IsFlying = isFlying;
            IsOverboard = isOverboard;
            HasBeenInside = hasBeenInside;
            IsRiding = isRiding;
            IsInSafeSpace = isInSafeSpace;
            IsOverFloor = isOverFloor;
            DriftSeconds = driftSeconds;
            DistanceMeters = distanceMeters;
        }
    }
}
