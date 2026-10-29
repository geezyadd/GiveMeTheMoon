using Features.ParcelModule.Data;
using Features.ParcelModule.Data.Generated;
using Features.ParcelModule.Scripts.Configuration;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Services {
    // Only the stand at the station the ship stands at offers: the ship is in Build and near it. The pad the ship left
    // stays a while with its stand, which then offers nothing.
    public sealed class ParcelStandService : IParcelStandService {
        private readonly IReadOnlyParcelsModel _parcelsModel;
        private readonly IReadOnlyShipRunModel _shipRunModel;
        private readonly IShipClientBindingModel _shipClientBindingModel;
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly ParcelConfiguration _parcelConfiguration;

        public ParcelStandService(
            IReadOnlyParcelsModel parcelsModel,
            IReadOnlyShipRunModel shipRunModel,
            IShipClientBindingModel shipClientBindingModel,
            IShipRunBindingModel shipRunBindingModel,
            ParcelConfiguration parcelConfiguration) {
            _parcelsModel = parcelsModel;
            _shipRunModel = shipRunModel;
            _shipClientBindingModel = shipClientBindingModel;
            _shipRunBindingModel = shipRunBindingModel;
            _parcelConfiguration = parcelConfiguration;
        }

        public bool IsOffering(Vector3 standPosition) {
            ShipBase ship = _shipRunBindingModel.Ship != null ? _shipRunBindingModel.Ship : _shipClientBindingModel.Ship;
            if (ship == null || _shipRunModel.Phase != ShipRunPhase.Build)
                return false;

            return Vector3.Distance(ship.transform.position, standPosition) <= _parcelConfiguration.StandShipMaxMeters;
        }

        public bool TryGetOffer(int slot, out ParcelOffer offer) {
            foreach (ParcelOffer candidate in _parcelsModel.Offers) {
                if (candidate.Slot != slot)
                    continue;

                offer = candidate;
                return true;
            }

            offer = default;
            return false;
        }
    }
}
