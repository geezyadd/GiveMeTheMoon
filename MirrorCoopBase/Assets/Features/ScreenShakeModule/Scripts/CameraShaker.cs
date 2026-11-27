using Unity.Cinemachine;
using UnityEngine;

namespace Features.ScreenShakeModule {
    // A Cinemachine camera's noise as the screen shake's output. The camera's creator binds it to the shake model (the
    // game cameras are not made through the container); it is listed while it is on.
    public sealed class CameraShaker : MonoBehaviour, ICameraShaker {
        [SerializeField] private CinemachineBasicMultiChannelPerlin _noise;

        private ScreenShakeModel _model;

        public void Bind(ScreenShakeModel model) {
            _model = model;
            if (isActiveAndEnabled)
                _model.AddShaker(this);
        }

        private void OnEnable() =>
            _model?.AddShaker(this);

        private void OnDisable() {
            _model?.RemoveShaker(this);
            Apply(0f, 0f);
        }

        public void Apply(float amplitude, float frequency) {
            if (_noise == null)
                return;

            _noise.AmplitudeGain = amplitude;
            _noise.FrequencyGain = frequency;
        }
    }
}
