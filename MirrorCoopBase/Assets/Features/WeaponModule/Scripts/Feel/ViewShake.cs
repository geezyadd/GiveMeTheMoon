using Features.WeaponModule.Scripts.Definitions;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Feel {
    // Turns the holder's view a little: a light constant shake while firing and short stronger kicks from impacts
    // close by. Only the holding player's own view shakes.
    public sealed class ViewShake {
        private const float ROW_PITCH = 3.1f;
        private const float ROW_YAW = 17.9f;
        private const float ROW_ROLL = 41.7f;

        private readonly Transform _root;
        private readonly Quaternion _restRotation;
        private readonly WeaponFeelProfile _profile;

        private float _fireWeight;
        private float _impulse;
        private float _noiseTime;

        public ViewShake(Transform root, WeaponFeelProfile profile) {
            _root = root;
            _restRotation = root.localRotation;
            _profile = profile;
        }

        public void AddImpulse(float amplitude) =>
            _impulse = Mathf.Max(_impulse, amplitude);

        public void Advance(bool isFiring, bool isViewOwned, float deltaTime) {
            float fadeRate = _profile.ShakeFadeSeconds > 0f ? deltaTime / _profile.ShakeFadeSeconds : 1f;
            _fireWeight = Mathf.MoveTowards(_fireWeight, isFiring ? 1f : 0f, fadeRate);
            _impulse = Mathf.MoveTowards(_impulse, 0f, fadeRate * _profile.NearImpactShakeAmplitude);
            if (isViewOwned == false) {
                _root.localRotation = _restRotation;
                return;
            }

            _noiseTime += deltaTime * _profile.ShakeFrequency;
            float amplitude = _fireWeight * _profile.FireShakeAmplitude + _impulse;
            _root.localRotation = _restRotation * Quaternion.Euler(
                SignedNoise(ROW_PITCH) * amplitude,
                SignedNoise(ROW_YAW) * amplitude,
                SignedNoise(ROW_ROLL) * amplitude * _profile.ShakeRollShare);
        }

        private float SignedNoise(float row) =>
            Mathf.PerlinNoise(_noiseTime, row) * 2f - 1f;
    }
}
