using UnityEngine;

namespace Features.ShipModule.Scripts.Impacts {
    // The knockback of a rock hit: away from the rock plus back against the heading as a share of the forward speed,
    // so a faster ship loses more speed. A large rock is felt ahead of the hull, along its motion, by a share of the
    // braking distance at the knockback's acceleration limit (capped), so the ship stops at the rock, not inside it.
    public sealed class ShipRockKnockbackRule : IShipRockKnockbackRule {
        private const float MIN_HORIZONTAL_SQR = 0.0001f;
        private const float MIN_SPEED = 0.01f;

        private readonly ShipRockImpactConfiguration _shipRockImpactConfiguration;

        public ShipRockKnockbackRule(ShipRockImpactConfiguration shipRockImpactConfiguration) =>
            _shipRockImpactConfiguration = shipRockImpactConfiguration;

        public Vector3 CalculateAwayDirection(Vector3 shipPosition, Vector3 rockCenter, Vector3 heading) {
            Vector3 away = shipPosition - rockCenter;
            away.y = 0f;
            return away.sqrMagnitude > MIN_HORIZONTAL_SQR ? away.normalized : -heading;
        }

        public ShipKnockbackImpulse CalculateSmallPush(Vector3 awayDirection, Vector3 heading, Vector3 shipVelocity) =>
            new(CalculateVelocity(awayDirection, _shipRockImpactConfiguration.SmallAwaySpeed, heading, shipVelocity, _shipRockImpactConfiguration.SmallBackSpeedShare),
                _shipRockImpactConfiguration.SmallPushSeconds, _shipRockImpactConfiguration.SmallMaxAcceleration);

        public ShipKnockbackImpulse CalculateLargeKnockback(Vector3 awayDirection, Vector3 heading, Vector3 shipVelocity) =>
            new(CalculateVelocity(awayDirection, _shipRockImpactConfiguration.LargeAwaySpeed, heading, shipVelocity, _shipRockImpactConfiguration.LargeBackSpeedShare),
                _shipRockImpactConfiguration.LargeKnockbackSeconds, _shipRockImpactConfiguration.LargeMaxAcceleration);

        public Vector3 CalculateLargeLookAhead(Vector3 shipVelocity) {
            float speed = shipVelocity.magnitude;
            float brakingDistance = speed * speed / (2f * _shipRockImpactConfiguration.LargeMaxAcceleration);
            float lookAhead = Mathf.Min(brakingDistance * _shipRockImpactConfiguration.BrakingLookAheadShare, _shipRockImpactConfiguration.MaxLookAheadMeters);
            return speed > MIN_SPEED ? shipVelocity * (lookAhead / speed) : Vector3.zero;
        }

        private static Vector3 CalculateVelocity(Vector3 awayDirection, float awaySpeed, Vector3 heading, Vector3 shipVelocity, float backSpeedShare) {
            float forwardSpeed = Mathf.Max(0f, Vector3.Dot(heading, shipVelocity));
            return awayDirection * awaySpeed - heading * (forwardSpeed * backSpeedShare);
        }
    }
}
