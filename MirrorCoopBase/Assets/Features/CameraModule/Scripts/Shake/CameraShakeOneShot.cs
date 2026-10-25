using Features.CameraModule.Contracts;
using UnityEngine;

namespace Features.CameraModule.Scripts.Shake {
    // A shake that fades out by itself. One per profile is kept and restarted, so repeated hits allocate nothing.
    public sealed class CameraShakeOneShot {
        private float _length;
        private float _age;

        public CameraShakeProfile Profile { get; }
        public float Strength { get; private set; }
        public float NoiseTime { get; private set; }
        public bool IsOver => _age >= _length;

        public float CurrentStrength =>
            IsOver ? 0f : Strength * Mathf.Pow(1f - _age / _length, Profile.FadePower);

        public CameraShakeOneShot(CameraShakeProfile profile, float strength) {
            Profile = profile;
            Restart(strength);
        }

        public void Restart(float strength) {
            Strength = strength;
            _length = Profile.IsLengthScaledByStrength ? Profile.Seconds * strength : Profile.Seconds;
            _age = 0f;
            NoiseTime = 0f;
        }

        public void Advance(float deltaTime) {
            _age += deltaTime;
            NoiseTime += deltaTime * Profile.Frequency;
        }
    }
}
