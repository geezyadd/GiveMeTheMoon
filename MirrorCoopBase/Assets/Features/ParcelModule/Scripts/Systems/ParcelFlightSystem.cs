using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Cargo;
using Features.ParcelModule.Data;
using Features.ParcelModule.Scripts.Rules;
using Features.ParcelModule.Scripts.Server;
using Features.ParcelModule.Scripts.Services;
using Features.ShipModule.Scripts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ParcelModule.Scripts.Systems {
    // Server, every frame, for every parcel: a free parcel on board is a cargo the bugs may go for; a pulled one comes
    // in on its rope, a drifting one is caught when the ship flies into it; one that is too far, too long gone or fell
    // overboard is lost; a crate that vanished takes its parcel with it.
    public sealed class ParcelFlightSystem : ITickable {
        private readonly ParcelServerModel _parcelServerModel;
        private readonly ParcelsBindingModel _parcelsBindingModel;
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IParcelEntryService _parcelEntryService;
        private readonly IParcelTheftService _parcelTheftService;
        private readonly IParcelRecoveryService _parcelRecoveryService;
        private readonly IParcelAboardService _parcelAboardService;
        private readonly IParcelCrateService _parcelCrateService;
        private readonly IParcelLossRule _parcelLossRule;
        private readonly IFieldAgentCargoTargetRegistry _fieldAgentCargoTargetRegistry;
        private readonly List<ParcelServerRecord> _records = new();

        public ParcelFlightSystem(
            ParcelServerModel parcelServerModel,
            ParcelsBindingModel parcelsBindingModel,
            IShipRunBindingModel shipRunBindingModel,
            IParcelEntryService parcelEntryService,
            IParcelTheftService parcelTheftService,
            IParcelRecoveryService parcelRecoveryService,
            IParcelAboardService parcelAboardService,
            IParcelCrateService parcelCrateService,
            IParcelLossRule parcelLossRule,
            IFieldAgentCargoTargetRegistry fieldAgentCargoTargetRegistry) {
            _parcelServerModel = parcelServerModel;
            _parcelsBindingModel = parcelsBindingModel;
            _shipRunBindingModel = shipRunBindingModel;
            _parcelEntryService = parcelEntryService;
            _parcelTheftService = parcelTheftService;
            _parcelRecoveryService = parcelRecoveryService;
            _parcelAboardService = parcelAboardService;
            _parcelCrateService = parcelCrateService;
            _parcelLossRule = parcelLossRule;
            _fieldAgentCargoTargetRegistry = fieldAgentCargoTargetRegistry;
        }

        public void Tick() {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (NetworkServer.active == false || _parcelsBindingModel.IsBound == false || ship == null || _parcelServerModel.Records.Count == 0)
                return;

            _records.Clear();
            _records.AddRange(_parcelServerModel.Records);
            foreach (ParcelServerRecord record in _records)
                StepParcel(record, ship);
        }

        private void StepParcel(ParcelServerRecord record, ShipBase ship) {
            if (record.Crate == null || _parcelEntryService.TryFind(record.NetId, out ParcelEntry entry) == false) {
                _parcelCrateService.ServerDestroy(record.NetId);
                return;
            }

            if (IsLost(record, entry, ship)) {
                _parcelCrateService.ServerDestroy(record.NetId);
                return;
            }

            UpdateCargoTarget(record, ship);
            if (entry.State == ParcelState.Pulled)
                _parcelRecoveryService.ServerPullIn(record, ship, Time.deltaTime);
            else if (entry.State == ParcelState.Drifting)
                _parcelRecoveryService.ServerCatchFlownInto(record, ship);
        }

        // A thief is measured where it flies, everything else where the crate is.
        private bool IsLost(ParcelServerRecord record, in ParcelEntry entry, ShipBase ship) {
            Vector3 position = entry.State == ParcelState.Carried ? record.CarrierPosition : record.Crate.Position;
            float distance = Vector3.Distance(position, ship.transform.position);
            float secondsInState = Time.time - record.StateSince;
            return _parcelLossRule.IsLost(entry.State, distance, secondsInState, _parcelAboardService.GetHeightAboveDeck(position, ship));
        }

        private void UpdateCargoTarget(ParcelServerRecord record, ShipBase ship) {
            if (ship.IsFlying && _parcelTheftService.IsFreeOnBoard(record.NetId))
                _fieldAgentCargoTargetRegistry.Add(record.CargoTarget);
            else
                _fieldAgentCargoTargetRegistry.Remove(record.CargoTarget);
        }
    }
}
