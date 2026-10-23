using Features.GameCoreModule.Contracts;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The run's ship position for modules below the ship module: the server's bound ship, or this client's ship on
    // a client without a server.
    public sealed class ShipPositionSource : IShipPositionSource {
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IShipClientBindingModel _shipClientBindingModel;

        public ShipPositionSource(IShipRunBindingModel shipRunBindingModel, IShipClientBindingModel shipClientBindingModel) {
            _shipRunBindingModel = shipRunBindingModel;
            _shipClientBindingModel = shipClientBindingModel;
        }

        public bool TryGetShipPosition(out Vector3 position) {
            position = Vector3.zero;
            ShipBase ship = _shipRunBindingModel.Ship != null ? _shipRunBindingModel.Ship : _shipClientBindingModel.Ship;
            if (ship == null)
                return false;

            position = ship.transform.position;
            return true;
        }
    }
}
