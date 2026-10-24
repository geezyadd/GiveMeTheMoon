using Features.ShipModule.Scripts.Hull;

namespace Features.ShipModule.Scripts.Editor.Tests {
    // A null hull stands for "no ship bound to the run".
    public sealed class FakeShipHullStatSource : IShipHullStatSource {
        private readonly IShipHullStat _hull;

        public FakeShipHullStatSource(IShipHullStat hull) =>
            _hull = hull;

        public bool TryGetHull(out IShipHullStat hull) {
            hull = _hull;
            return hull != null;
        }
    }
}
