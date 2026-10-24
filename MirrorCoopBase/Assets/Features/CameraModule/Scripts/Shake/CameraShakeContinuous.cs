using Features.CameraModule.Contracts;
using UnityEngine;

namespace Features.CameraModule.Scripts.Shake {
    // A shake that lasts while its source holds the handle; the intensity eases to the target over the profile's seconds.
    public sealed class CameraShakeContinuous : ICameraShakeHandle {
        private float _targetIntensity;

        public CameraShakeContinuous(CameraShakeProfile profile) =>
            Profile = profile;

        public CameraShakeProfile Profile { get; }
        public float Intensity { get; private set; }
        public float NoiseTime { get; private set; }
        public bool IsStopped { get; private set; }

        public void SetIntensity(float intensity) =>
            _targetIntensity = Mathf.Max(0f, intensity);

        public void Stop() =>
            IsStopped = true;

        public void Advance(float deltaTime) {
            float step = Profile.Seconds > 0f ? deltaTime / Profile.Seconds : float.PositiveInfinity;
            Intensity = Mathf.MoveTowards(Intensity, _targetIntensity, step);
            NoiseTime += deltaTime * Profile.Frequency;
        }
    }
}
