using UnityEngine;

namespace Features.CharacterMovableModule.Scripts.Drift {
    // The local look of a drifting body: a slow tumble around a tilted axis and a swing of the arms and legs. Each
    // peer runs its own; nothing of it is synced.
    public sealed class CharacterDriftTumble {
        private const float FULL_TURN_DEGREES = 360f;
        private const float TWO_PI = Mathf.PI * 2f;
        private static readonly Vector3 TumbleAxis = new Vector3(1f, 0.35f, 0.6f).normalized;

        private float _tumbleDegrees;
        private float _swingPhase;

        public Quaternion BodyRotation => Quaternion.AngleAxis(_tumbleDegrees, TumbleAxis);

        public void Reset() {
            _tumbleDegrees = 0f;
            _swingPhase = 0f;
        }

        public void Advance(CharacterDriftSettings settings, float deltaTime) {
            _tumbleDegrees = Mathf.Repeat(_tumbleDegrees + settings.TumbleDegreesPerSecond * deltaTime, FULL_TURN_DEGREES);
            _swingPhase = Mathf.Repeat(_swingPhase + settings.LimbSwingCyclesPerSecond * deltaTime, 1f);
        }

        // Each limb swings on its own phase offset (0..1), so the four never move together.
        public float LimbSwingDegrees(CharacterDriftSettings settings, float phaseOffset) =>
            Mathf.Sin((_swingPhase + phaseOffset) * TWO_PI) * settings.LimbSwingDegrees;
    }
}
