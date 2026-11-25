using UnityEngine;

namespace Features.CharacterMovableModule.Scripts.Drift {
    // The turn a drifting body shows so that it flies head first: its up axis goes smoothly toward its flight
    // direction. Slower than the settings' minimum it keeps the turn it has; when it no longer flies (the drift ended,
    // the ship caught it) it smoothly stands upright again. A world rotation, laid over the body's facing.
    public sealed class CharacterDriftHeadFirstTurn {
        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        public Quaternion Step(bool isFlying, Vector3 velocity, CharacterDriftSettings settings, float deltaTime) {
            if (settings == null) {
                Rotation = Quaternion.identity;
                return Rotation;
            }

            Quaternion target = Quaternion.identity;
            if (isFlying) {
                float minSpeed = settings.HeadFirstMinSpeedMetersPerSecond;
                target = velocity.sqrMagnitude >= minSpeed * minSpeed
                    ? Quaternion.FromToRotation(Vector3.up, velocity.normalized)
                    : Rotation;
            }

            float share = 1f - Mathf.Exp(-settings.HeadFirstTurnPerSecond * deltaTime);
            Rotation = Quaternion.Slerp(Rotation, target, share);
            return Rotation;
        }

        public void Reset() =>
            Rotation = Quaternion.identity;
    }
}
