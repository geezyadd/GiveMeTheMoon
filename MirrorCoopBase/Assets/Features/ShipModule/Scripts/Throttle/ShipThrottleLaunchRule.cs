using UnityEngine;

namespace Features.ShipModule.Scripts.Throttle {
    // The throttle is also the launch lever: at a station that cannot launch the handle stays at Stop; at one that
    // can, pushing it to the launch mark takes off. In flight it sets the share of the max speed.
    public sealed class ShipThrottleLaunchRule : IShipThrottleLaunchRule {
        private readonly ShipThrottleConfiguration _shipThrottleConfiguration;

        public ShipThrottleLaunchRule(ShipThrottleConfiguration shipThrottleConfiguration) =>
            _shipThrottleConfiguration = shipThrottleConfiguration;

        public float ReadMaxPosition(ShipRunPhase phase, bool canLaunch) =>
            phase == ShipRunPhase.Build && canLaunch == false ? 0f : 1f;

        public ShipThrottleMove Evaluate(ShipRunPhase phase, bool canLaunch, float requestedPosition) {
            float position = Mathf.Clamp(requestedPosition, 0f, ReadMaxPosition(phase, canLaunch));
            bool isLaunch = phase == ShipRunPhase.Build && canLaunch && position >= _shipThrottleConfiguration.LaunchMark;
            return new ShipThrottleMove(position, isLaunch);
        }
    }
}
