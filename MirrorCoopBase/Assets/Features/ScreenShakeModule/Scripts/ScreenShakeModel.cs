using System.Collections.Generic;

namespace Features.ScreenShakeModule {
    // The shakes running on this peer and the cameras that show them. Local: a shake never goes over the network.
    public sealed class ScreenShakeModel {
        private readonly List<ICameraShaker> _shakers = new();
        private readonly List<ActiveScreenShake> _shakes = new();

        public IReadOnlyList<ICameraShaker> Shakers => _shakers;
        public IReadOnlyList<ActiveScreenShake> Shakes => _shakes;

        public void AddShaker(ICameraShaker shaker) {
            if (_shakers.Contains(shaker) == false)
                _shakers.Add(shaker);
        }

        public void RemoveShaker(ICameraShaker shaker) =>
            _shakers.Remove(shaker);

        public void AddShake(ScreenShakeProfile profile, float strength) =>
            _shakes.Add(new ActiveScreenShake(profile, strength));

        public void RemoveShakeAt(int index) =>
            _shakes.RemoveAt(index);
    }
}
