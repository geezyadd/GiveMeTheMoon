using System.Collections.Generic;

namespace Features.CameraModule.Scripts.Shake {
    // The camera shakes of this peer: one-shot shakes (one per profile, kept after they fade) and continuous ones.
    public sealed class CameraShakeModel {
        private readonly List<CameraShakeOneShot> _oneShots = new();
        private readonly List<CameraShakeContinuous> _continuousShakes = new();

        public IReadOnlyList<CameraShakeOneShot> OneShots => _oneShots;
        public IReadOnlyList<CameraShakeContinuous> ContinuousShakes => _continuousShakes;
        public bool IsEmpty => _oneShots.Count == 0 && _continuousShakes.Count == 0;

        public void AddOneShot(CameraShakeOneShot shake) =>
            _oneShots.Add(shake);

        public void AddContinuous(CameraShakeContinuous shake) =>
            _continuousShakes.Add(shake);

        public void RemoveContinuousAt(int index) =>
            _continuousShakes.RemoveAt(index);

        public void Clear() {
            _oneShots.Clear();
            _continuousShakes.Clear();
        }
    }
}
