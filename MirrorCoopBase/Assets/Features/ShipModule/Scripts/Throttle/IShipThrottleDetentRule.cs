namespace Features.ShipModule.Scripts.Throttle {
    // The soft detents along the throttle's arc.
    public interface IShipThrottleDetentRule {
        public float PullTowardsDetent(float target);

        public float SettleOnRelease(float position);

        public int FindClickedDetent(float position);

        public string FindNearestName(float position);
    }
}
