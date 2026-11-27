using UnityEngine;
using Zenject;

namespace Features.ScreenShakeModule {
    // Once a frame: every shake loses the frame's time and the ended ones go; the strongest that is left drives every
    // camera's noise, and with none left the noise stops.
    public sealed class ScreenShakeSystem : ITickable {
        private readonly ScreenShakeModel _model;

        public ScreenShakeSystem(ScreenShakeModel model) =>
            _model = model;

        public void Tick() =>
            Advance(Time.deltaTime);

        public void Advance(float deltaTime) {
            if (_model.Shakes.Count == 0)
                return;

            ActiveScreenShake strongest = UpdateShakes(deltaTime);
            if (strongest == null)
                ApplyToShakers(0f, 0f);
            else
                ApplyToShakers(strongest.Amplitude * _model.StrengthScale, strongest.Profile.Frequency);
        }

        private ActiveScreenShake UpdateShakes(float deltaTime) {
            ActiveScreenShake strongest = null;
            for (int i = _model.Shakes.Count - 1; i >= 0; i--) {
                ActiveScreenShake shake = _model.Shakes[i];
                shake.SetTimeLeft(Mathf.Max(0f, shake.TimeLeft - deltaTime));
                if (shake.TimeLeft <= 0f) {
                    _model.RemoveShakeAt(i);
                    continue;
                }

                if (strongest == null || shake.Amplitude > strongest.Amplitude)
                    strongest = shake;
            }

            return strongest;
        }

        private void ApplyToShakers(float amplitude, float frequency) {
            foreach (ICameraShaker shaker in _model.Shakers)
                shaker.Apply(amplitude, frequency);
        }
    }
}
