using UnityEngine;

namespace Features.ScreenShakeModule {
    public sealed class ScreenShakeService : IScreenShakeService {
        private readonly ScreenShakeModel _model;

        public ScreenShakeService(ScreenShakeModel model) =>
            _model = model;

        public void Shake(ScreenShakeProfile profile, float strength = 1f) {
            if (profile != null && strength > 0f)
                _model.AddShake(profile, Mathf.Max(0f, strength));
        }
    }
}
