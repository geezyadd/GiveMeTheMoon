namespace Features.ShipModule.Scripts.Hull {
    // The hull of the ship bound to the run; none in the lobby and between runs.
    public sealed class ShipHullStatSource : IShipHullStatSource {
        private readonly IShipRunBindingModel _shipRunBindingModel;

        public ShipHullStatSource(IShipRunBindingModel shipRunBindingModel) =>
            _shipRunBindingModel = shipRunBindingModel;

        public bool TryGetHull(out IShipHullStat hull) {
            hull = null;
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null)
                return false;

            hull = ship.Hull;
            return true;
        }
    }
}
