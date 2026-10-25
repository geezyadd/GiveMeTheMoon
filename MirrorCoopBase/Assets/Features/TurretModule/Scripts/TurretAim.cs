using UnityEngine;

namespace Features.TurretModule.Scripts {
    // A turret's aim as yaw and pitch in its mount's frame, within the turret's limits. Pitch is positive up.
    public sealed class TurretAim {
        private const float HALF_TURN_DEGREES = 180f;
        private const float FULL_TURN_DEGREES = 360f;

        private readonly ShipTurretDefinition _definition;

        public float Yaw { get; private set; }
        public float Pitch { get; private set; }

        public Vector3 Direction => Quaternion.Euler(-Pitch, Yaw, 0f) * Vector3.forward;

        public TurretAim(ShipTurretDefinition definition) =>
            _definition = definition;

        public void Turn(Vector2 lookDelta) {
            float sensitivity = _definition.LookSensitivity;
            Set(Yaw + lookDelta.x * sensitivity, Pitch + lookDelta.y * sensitivity);
        }

        public void Set(float yaw, float pitch) {
            Yaw = LimitYaw(yaw);
            Pitch = Mathf.Clamp(pitch, _definition.MinPitchDegrees, _definition.MaxPitchDegrees);
        }

        public void SetDirection(Vector3 direction) {
            if (direction.sqrMagnitude < Mathf.Epsilon)
                return;

            Vector3 flat = new Vector3(direction.x, 0f, direction.z);
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Atan2(direction.y, flat.magnitude) * Mathf.Rad2Deg;
            Set(yaw, pitch);
        }

        // A free turret wraps round; a limited one stops at its limit, so a long turn never wraps it to the other side.
        private float LimitYaw(float yaw) {
            float limit = _definition.YawLimitDegrees;
            return limit >= HALF_TURN_DEGREES
                ? Mathf.Repeat(yaw + HALF_TURN_DEGREES, FULL_TURN_DEGREES) - HALF_TURN_DEGREES
                : Mathf.Clamp(yaw, -limit, limit);
        }
    }
}
