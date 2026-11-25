using UnityEngine;

namespace Features.CharacterMovableModule.Scripts.Drift {
    // One physics step of a drifting body, with no gravity: the speed above the slow drift fades exponentially, a
    // slower drift keeps its speed, and the push adds a little speed up to its own cap. A push never brakes a body
    // that is already faster than the cap (the exit velocity or a shot's recoil).
    public sealed class CharacterDriftMotion {
        private const float NO_PUSH_SQR = 0.0001f;

        public Vector3 Step(Vector3 velocity, Vector3 pushDirection, CharacterDriftSettings settings, float deltaTime) {
            Vector3 damped = Damp(velocity, settings, deltaTime);
            return Push(damped, pushDirection, settings, deltaTime);
        }

        private static Vector3 Damp(Vector3 velocity, CharacterDriftSettings settings, float deltaTime) {
            float speed = velocity.magnitude;
            float drift = settings.DriftSpeedMetersPerSecond;
            if (speed <= drift)
                return velocity;

            float faded = drift + (speed - drift) * Mathf.Exp(-settings.DampingPerSecond * deltaTime);
            return velocity * (faded / speed);
        }

        private static Vector3 Push(Vector3 velocity, Vector3 pushDirection, CharacterDriftSettings settings, float deltaTime) {
            if (pushDirection.sqrMagnitude < NO_PUSH_SQR)
                return velocity;

            Vector3 direction = Vector3.ClampMagnitude(pushDirection, 1f);
            float alongBefore = Vector3.Dot(velocity, direction.normalized);
            float cap = settings.MaxPushSpeedMetersPerSecond;
            if (alongBefore >= cap)
                return velocity;

            Vector3 pushed = velocity + direction * (settings.PushAcceleration * deltaTime);
            float alongAfter = Vector3.Dot(pushed, direction.normalized);
            return alongAfter <= cap ? pushed : pushed - direction.normalized * (alongAfter - cap);
        }
    }
}
