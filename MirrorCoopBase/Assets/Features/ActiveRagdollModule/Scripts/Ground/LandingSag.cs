using UnityEngine;

namespace Features.ActiveRagdollModule {
    // How far below its standing height the trunk is let down after a landing: the faster the landing, the deeper
    // (up to a limit); it sinks smoothly, then comes smoothly back up. The hover holds the trunk at the standing height
    // minus this depth.
    public sealed class LandingSag {
        private float _depth;
        private float _seconds;
        private float _sinkShare;
        private float _elapsed;

        public bool IsSagging => _elapsed < _seconds;

        public void Begin(float landingSpeed, RagdollStandSettings settings) {
            _depth = Mathf.Min(Mathf.Max(landingSpeed, 0f) * settings.LandingSagPerSpeed, settings.MaxLandingSag);
            _seconds = _depth > 0f ? Mathf.Max(settings.LandingSagSeconds, 0f) : 0f;
            _sinkShare = Mathf.Clamp(settings.LandingSagSinkShare, 0.05f, 0.95f);
            _elapsed = 0f;
        }

        // The depth on this step, metres; 0 once the sag is over.
        public float Step(float deltaTime) {
            if (IsSagging == false)
                return 0f;

            _elapsed += deltaTime;
            float time = Mathf.Clamp01(_elapsed / _seconds);
            float shape = time < _sinkShare
                ? Mathf.SmoothStep(0f, 1f, time / _sinkShare)
                : 1f - Mathf.SmoothStep(0f, 1f, (time - _sinkShare) / (1f - _sinkShare));
            return _depth * shape;
        }

        public void Stop() =>
            _elapsed = _seconds;
    }
}
