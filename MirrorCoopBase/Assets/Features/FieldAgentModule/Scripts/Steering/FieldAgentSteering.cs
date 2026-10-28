using Features.FieldAgentModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Steering {
    // Seek with arrival (slows down within the profile's slowing distance) and the velocity limits: the direction
    // turns at most the turn rate, the speed changes at most the acceleration.
    public sealed class FieldAgentSteering : IFieldAgentSteering {
        private const float MIN_SPEED = 0.05f;

        public Vector3 CalculateSeek(Vector3 position, Vector3 target, float speedShare, FieldAgentProfile profile) {
            Vector3 toTarget = target - position;
            float distance = toTarget.magnitude;
            if (distance < Mathf.Epsilon)
                return Vector3.zero;

            float arrival = Mathf.Clamp01(distance / profile.SlowingMeters);
            return toTarget / distance * (profile.MaxSpeed * speedShare * arrival);
        }

        public Vector3 LimitVelocity(Vector3 current, Vector3 desired, FieldAgentProfile profile, float deltaTime) {
            float speedStep = profile.Acceleration * deltaTime;
            if (current.magnitude < MIN_SPEED || desired.magnitude < MIN_SPEED)
                return Vector3.MoveTowards(current, desired, speedStep);

            float turnStep = profile.TurnRateDegrees * Mathf.Deg2Rad * deltaTime;
            return Vector3.RotateTowards(current, desired, turnStep, speedStep);
        }
    }
}
