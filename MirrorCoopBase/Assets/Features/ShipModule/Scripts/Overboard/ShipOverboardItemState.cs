namespace Features.ShipModule.Scripts.Overboard {
    // One loose item as the overboard rule sees it.
    public readonly struct ShipOverboardItemState {
        public ShipOverboardItemState(bool isDrifting, bool isInSafeSpace, bool isOverFloor, float driftSeconds, float distanceMeters) {
            IsDrifting = isDrifting;
            IsInSafeSpace = isInSafeSpace;
            IsOverFloor = isOverFloor;
            DriftSeconds = driftSeconds;
            DistanceMeters = distanceMeters;
        }

        public bool IsDrifting { get; }
        public bool IsInSafeSpace { get; }
        public bool IsOverFloor { get; }
        public float DriftSeconds { get; }
        // From the item to the nearest safe space (the ship, an overboard area's floors), meters.
        public float DistanceMeters { get; }
    }
}
