using UnityEngine;

namespace Features.WeaponModule.Scripts.Feel {
    // Turns the barrel cluster around its own forward axis at the fire mode's readiness: the spin-up and spin-down
    // are visible before the first and after the last shot.
    public sealed class BarrelSpin {
        private const float FULL_TURN_DEGREES = 360f;

        private readonly Transform _barrels;
        private readonly Quaternion _restRotation;
        private readonly float _fullSpinDegreesPerSecond;

        private float _angle;

        public BarrelSpin(Transform barrels, float fullSpinDegreesPerSecond) {
            _barrels = barrels;
            _restRotation = barrels.localRotation;
            _fullSpinDegreesPerSecond = fullSpinDegreesPerSecond;
        }

        public void Advance(float readiness, float deltaTime) {
            _angle = Mathf.Repeat(_angle + readiness * _fullSpinDegreesPerSecond * deltaTime, FULL_TURN_DEGREES);
            _barrels.localRotation = _restRotation * Quaternion.Euler(0f, 0f, _angle);
        }
    }
}
