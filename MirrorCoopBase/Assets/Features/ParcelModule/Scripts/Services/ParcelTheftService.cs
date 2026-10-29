using System;
using Features.FieldAgentModule.Scripts.Cargo;
using Features.ParcelModule.Data;
using Features.ParcelModule.Scripts.Configuration;
using Features.ParcelModule.Scripts.Server;
using Features.ShipModule.Scripts;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Services {
    // A grabbed parcel leaves the deck (no hand takes it, the deck lets go of it) and hangs under its thief; let go,
    // it drifts on away from the ship and a weapon can strike it. A call for a parcel in another state changes nothing.
    public sealed class ParcelTheftService : IParcelTheftService {
        private const float MIN_AWAY_SQR = 0.01f;
        private const int NO_CARRIER = 0;
        private const string NO_SHIP_ERROR = "Parcel {0} is a bugs' cargo target with no ship bound on the server.";
        private const string NO_CRATE_ERROR = "Parcel {0} is a bugs' cargo target without its crate.";

        private readonly ParcelServerModel _parcelServerModel;
        private readonly IParcelEntryService _parcelEntryService;
        private readonly IParcelAboardService _parcelAboardService;
        private readonly IFieldAgentCargoTargetRegistry _fieldAgentCargoTargetRegistry;
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly ParcelConfiguration _parcelConfiguration;

        public ParcelTheftService(
            ParcelServerModel parcelServerModel,
            IParcelEntryService parcelEntryService,
            IParcelAboardService parcelAboardService,
            IFieldAgentCargoTargetRegistry fieldAgentCargoTargetRegistry,
            IShipRunBindingModel shipRunBindingModel,
            ParcelConfiguration parcelConfiguration) {
            _parcelServerModel = parcelServerModel;
            _parcelEntryService = parcelEntryService;
            _parcelAboardService = parcelAboardService;
            _fieldAgentCargoTargetRegistry = fieldAgentCargoTargetRegistry;
            _shipRunBindingModel = shipRunBindingModel;
            _parcelConfiguration = parcelConfiguration;
        }

        public bool IsFreeOnBoard(uint netId) {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null || TryGetInState(netId, ParcelState.Loose, out ParcelServerRecord record) == false)
                return false;

            return record.Crate.Grabbable.CanBeGrabbed && _parcelAboardService.IsAboard(record.Crate.Position, ship);
        }

        // Asked only of a registered cargo target: it is registered while its crate lies free on the flying ship's deck.
        public Vector3 GetShipLocalPosition(uint netId) {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null)
                throw new InvalidOperationException(string.Format(NO_SHIP_ERROR, netId));

            if (_parcelServerModel.TryGet(netId, out ParcelServerRecord record) == false || record.Crate == null)
                throw new InvalidOperationException(string.Format(NO_CRATE_ERROR, netId));

            return ship.transform.InverseTransformPoint(record.Crate.Position);
        }

        public void ServerTake(uint netId, int carrierAgentId) {
            if (TryGetInState(netId, ParcelState.Loose, out ParcelServerRecord record) == false)
                return;

            _fieldAgentCargoTargetRegistry.Remove(record.CargoTarget);
            _parcelEntryService.ServerSetState(netId, ParcelState.Carried, carrierAgentId, string.Empty, string.Empty);
            record.RestartStateClock(Time.time);
            record.RememberCarrier(record.Crate.Position);
            record.Crate.Grabbable.SetGrabBlocked(true);
            record.Crate.HoldAt(record.Crate.Position);
        }

        public void ServerFollowCarrier(uint netId, Vector3 carrierWorldPosition) {
            if (TryGetInState(netId, ParcelState.Carried, out ParcelServerRecord record) == false)
                return;

            record.RememberCarrier(carrierWorldPosition);
            record.Crate.HoldAt(HangUnder(carrierWorldPosition));
        }

        public void ServerDrop(uint netId, Vector3 carrierWorldPosition) {
            if (TryGetInState(netId, ParcelState.Carried, out ParcelServerRecord record) == false)
                return;

            _parcelEntryService.ServerSetState(netId, ParcelState.Drifting, NO_CARRIER, string.Empty, string.Empty);
            record.RestartStateClock(Time.time);
            Vector3 position = HangUnder(carrierWorldPosition);
            record.Crate.HoldAt(position);
            record.Crate.Drift(CalculateDriftVelocity(position));
            record.Crate.SetStrikable(true);
        }

        private bool TryGetInState(uint netId, ParcelState state, out ParcelServerRecord record) =>
            _parcelServerModel.TryGet(netId, out record)
            && record.Crate != null
            && _parcelEntryService.TryFind(netId, out ParcelEntry entry)
            && entry.State == state;

        private Vector3 HangUnder(Vector3 carrierWorldPosition) =>
            carrierWorldPosition + Vector3.down * _parcelConfiguration.CarrierHangMeters;

        // On along the thief's way out, away from the ship.
        private Vector3 CalculateDriftVelocity(Vector3 position) {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null)
                return Vector3.zero;

            Vector3 away = position - ship.transform.position;
            away.y = 0f;
            return away.sqrMagnitude > MIN_AWAY_SQR ? away.normalized * _parcelConfiguration.DriftSpeed : Vector3.zero;
        }
    }
}
