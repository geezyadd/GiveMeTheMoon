namespace Features.ShipModule.Scripts.Throttle {
    // Where the server puts the throttle for a requested position, and whether that move takes off.
    public readonly struct ShipThrottleMove {
        public float Position { get; }
        public bool IsLaunch { get; }

        public ShipThrottleMove(float position, bool isLaunch) {
            Position = position;
            IsLaunch = isLaunch;
        }
    }
}
