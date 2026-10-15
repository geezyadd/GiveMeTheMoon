using Features.GameCoreModule.Contracts;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The run's ship position for modules below the ship module; the ship is bound on the server only.
    public sealed class ShipPositionSource : IShipPositionSource {
        private readonly IShipRunBindingModel _shipRunBindingModel;

        public ShipPositionSource(IShipRunBindingModel shipRunBindingModel) =>
            _shipRunBindingModel = shipRunBindingModel;

        public bool TryGetShipPosition(out Vector3 position) {
            position = Vector3.zero;
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null)
                return false;

            position = ship.transform.position;
            return true;
        }
    }
}
