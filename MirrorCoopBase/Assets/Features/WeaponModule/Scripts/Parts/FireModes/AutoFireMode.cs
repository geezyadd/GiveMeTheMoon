using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.FireModes {
    // Continuous fire with barrel spin: holding the trigger spins the barrels up, shots come at the full rate only at
    // full spin (the first one at the moment full spin is reached), releasing spins them down. Pressing again during
    // the spin-down starts from the current spin. Frame-rate independent: the time left in a frame after the barrels
    // reach full spin already counts towards the cadence.
    public sealed class AutoFireMode : IWeaponFireMode {
        private const float FULL_SPIN = 1f;

        private readonly float _spinUpSeconds;
        private readonly float _spinDownSeconds;
        private readonly float _shotsPerSecond;

        private float _shotProgress;

        public AutoFireMode(float spinUpSeconds, float spinDownSeconds, float shotsPerSecond) {
            _spinUpSeconds = spinUpSeconds;
            _spinDownSeconds = spinDownSeconds;
            _shotsPerSecond = shotsPerSecond;
        }

        public float Readiness { get; private set; }
        public bool IsFiring { get; private set; }

        public int Advance(bool isTriggerHeld, float deltaTime) {
            if (isTriggerHeld == false) {
                StopFiring();
                SpinDown(deltaTime);
                return 0;
            }

            float firingSeconds = SpinUp(deltaTime);
            if (Readiness < FULL_SPIN)
                return 0;

            if (IsFiring == false)
                StartFiring();

            _shotProgress += firingSeconds * _shotsPerSecond;
            int shots = Mathf.FloorToInt(_shotProgress);
            _shotProgress -= shots;
            return shots;
        }

        private void StartFiring() {
            IsFiring = true;
            _shotProgress = 1f;
        }

        private void StopFiring() {
            IsFiring = false;
            _shotProgress = 0f;
        }

        // Returns the part of deltaTime that is left after the barrels reached full spin.
        private float SpinUp(float deltaTime) {
            float secondsToFullSpin = (FULL_SPIN - Readiness) * _spinUpSeconds;
            if (deltaTime < secondsToFullSpin) {
                Readiness += deltaTime / _spinUpSeconds;
                return 0f;
            }

            Readiness = FULL_SPIN;
            return deltaTime - secondsToFullSpin;
        }

        private void SpinDown(float deltaTime) =>
            Readiness = _spinDownSeconds > 0f
                ? Mathf.Max(0f, Readiness - deltaTime / _spinDownSeconds)
                : 0f;
    }
}
