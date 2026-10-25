using System.Collections.Generic;
using UnityEngine;

namespace Features.ShipModule.Scripts.Throttle {
    // The soft detents along the arc: near one the handle is pulled towards it, right at it the handle clicks.
    public sealed class ShipThrottleDetentRule : IShipThrottleDetentRule {
        public const int NO_DETENT = -1;

        private readonly ShipThrottleConfiguration _shipThrottleConfiguration;

        public ShipThrottleDetentRule(ShipThrottleConfiguration shipThrottleConfiguration) =>
            _shipThrottleConfiguration = shipThrottleConfiguration;

        // Where the handle settles for a hand at the target: a detent within the pull radius draws it in, more the
        // closer it is, so it sticks a little but the hand can still move past it.
        public float PullTowardsDetent(float target) {
            int nearest = FindNearestIndex(target);
            float detent = _shipThrottleConfiguration.Detents[nearest].Position;
            float radius = _shipThrottleConfiguration.DetentPullRadius;
            float distance = Mathf.Abs(target - detent);
            if (radius <= 0f || distance >= radius)
                return target;

            float pull = _shipThrottleConfiguration.DetentPullStrength * (1f - distance / radius);
            return Mathf.Lerp(target, detent, pull);
        }

        // A handle let go within a detent's pull radius drops into it; elsewhere it stays where it was left.
        public float SettleOnRelease(float position) {
            int nearest = FindNearestIndex(position);
            float detent = _shipThrottleConfiguration.Detents[nearest].Position;
            return Mathf.Abs(position - detent) < _shipThrottleConfiguration.DetentPullRadius ? detent : position;
        }

        // The detent the handle sits in, or NO_DETENT between them.
        public int FindClickedDetent(float position) {
            int nearest = FindNearestIndex(position);
            float distance = Mathf.Abs(position - _shipThrottleConfiguration.Detents[nearest].Position);
            return distance <= _shipThrottleConfiguration.DetentClickRadius ? nearest : NO_DETENT;
        }

        public string FindNearestName(float position) =>
            _shipThrottleConfiguration.Detents[FindNearestIndex(position)].Name;

        private int FindNearestIndex(float position) {
            IReadOnlyList<ShipThrottleConfiguration.Detent> detents = _shipThrottleConfiguration.Detents;
            int nearest = 0;
            float best = float.MaxValue;
            for (int i = 0; i < detents.Count; i++) {
                float distance = Mathf.Abs(position - detents[i].Position);
                if (distance >= best)
                    continue;

                best = distance;
                nearest = i;
            }

            return nearest;
        }
    }
}
