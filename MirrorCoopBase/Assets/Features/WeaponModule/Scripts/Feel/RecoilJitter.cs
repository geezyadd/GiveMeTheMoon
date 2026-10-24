using Features.WeaponModule.Scripts.Definitions;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Feel {
    // The gun and the hands on it never stand still while firing: smooth position and turn noise that fades in with
    // the fire and settles after it.
    public sealed class RecoilJitter {
        // Separate noise rows so the axes do not move together.
        private const float ROW_X = 11.3f;
        private const float ROW_Y = 37.9f;
        private const float ROW_Z = 71.1f;
        private const float ROW_PITCH = 5.7f;
        private const float ROW_YAW = 23.5f;
        private const float ROW_ROLL = 53.3f;

        private readonly Transform _root;
        private readonly Vector3 _restPosition;
        private readonly Quaternion _restRotation;
        private readonly WeaponFeelProfile _profile;

        private float _weight;
        private float _noiseTime;

        public RecoilJitter(Transform root, WeaponFeelProfile profile) {
            _root = root;
            _restPosition = root.localPosition;
            _restRotation = root.localRotation;
            _profile = profile;
        }

        public void Advance(bool isFiring, float deltaTime) {
            float settleRate = _profile.RecoilSettleSeconds > 0f ? deltaTime / _profile.RecoilSettleSeconds : 1f;
            _weight = Mathf.MoveTowards(_weight, isFiring ? 1f : 0f, settleRate);
            _noiseTime += deltaTime * _profile.RecoilNoiseFrequency;
            float position = _profile.RecoilPositionNoiseMeters * _weight;
            float rotation = _profile.RecoilRotationNoiseDegrees * _weight;
            _root.localPosition = _restPosition + new Vector3(
                SignedNoise(ROW_X) * position,
                SignedNoise(ROW_Y) * position,
                SignedNoise(ROW_Z) * position);
            _root.localRotation = _restRotation * Quaternion.Euler(
                SignedNoise(ROW_PITCH) * rotation,
                SignedNoise(ROW_YAW) * rotation,
                SignedNoise(ROW_ROLL) * rotation);
        }

        private float SignedNoise(float row) =>
            Mathf.PerlinNoise(_noiseTime, row) * 2f - 1f;
    }
}
