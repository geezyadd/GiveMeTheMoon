namespace Features.ShipModule.Scripts.Throttle {
    // The throttle as the launch lever: how far the handle may go now and what a requested position does.
    public interface IShipThrottleLaunchRule {
        public float ReadMaxPosition(ShipRunPhase phase, bool canLaunch);

        public ShipThrottleMove Evaluate(ShipRunPhase phase, bool canLaunch, float requestedPosition);
    }
}
