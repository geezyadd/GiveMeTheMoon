using System;
using Features.GameCoreModule.Contracts;
using Features.ParcelModule.Data;
using Features.ParcelModule.Scripts.Server;
using Features.ParcelModule.Scripts.Services;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Mirror;
using Zenject;

namespace Features.ParcelModule.Scripts.Systems {
    // Server: once per station the ship stands at, the crew's parcels are settled there and a delivery stand with new
    // offers stands beside its pad; a crash (Wreck), a destroyed hull or the end of the run loses every parcel, the
    // offers and the stand.
    public sealed class ParcelStationSystem : ITickable, IInitializable, IDisposable {
        private const int NO_STATION = -1;

        private readonly IReadOnlyShipRunModel _shipRunModel;
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly ParcelsBindingModel _parcelsBindingModel;
        private readonly IShipHullEvents _shipHullEvents;
        private readonly IParcelSettleService _parcelSettleService;
        private readonly IParcelOfferService _parcelOfferService;
        private readonly IParcelStandSpawnService _parcelStandSpawnService;
        private readonly IShipStationPads _shipStationPads;
        private readonly ParcelServerModel _parcelServerModel;
        private int _settledLoop = NO_STATION;
        private int _standLoop = NO_STATION;
        private bool _hasShip;
        private bool _isWrecked;

        public ParcelStationSystem(
            IReadOnlyShipRunModel shipRunModel,
            IShipRunBindingModel shipRunBindingModel,
            ParcelsBindingModel parcelsBindingModel,
            IShipHullEvents shipHullEvents,
            IParcelSettleService parcelSettleService,
            IParcelOfferService parcelOfferService,
            IParcelStandSpawnService parcelStandSpawnService,
            IShipStationPads shipStationPads,
            ParcelServerModel parcelServerModel) {
            _shipRunModel = shipRunModel;
            _shipRunBindingModel = shipRunBindingModel;
            _parcelsBindingModel = parcelsBindingModel;
            _shipHullEvents = shipHullEvents;
            _parcelSettleService = parcelSettleService;
            _parcelOfferService = parcelOfferService;
            _parcelStandSpawnService = parcelStandSpawnService;
            _shipStationPads = shipStationPads;
            _parcelServerModel = parcelServerModel;
        }

        public void Initialize() =>
            _shipHullEvents.OnDestroyed += OnHullDestroyed;

        public void Dispose() =>
            _shipHullEvents.OnDestroyed -= OnHullDestroyed;

        public void Tick() {
            if (NetworkServer.active == false || _parcelsBindingModel.IsBound == false)
                return;

            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null) {
                if (_hasShip)
                    LoseEverything();

                _hasShip = false;
                return;
            }

            _hasShip = true;
            bool wasWrecked = _isWrecked;
            _isWrecked = _shipRunModel.Phase == ShipRunPhase.Wreck;
            if (_isWrecked) {
                if (wasWrecked == false)
                    LoseEverything();

                return;
            }

            if (_shipRunModel.Phase != ShipRunPhase.Build)
                return;

            if (_shipRunModel.LoopIndex != _settledLoop)
                ArriveAtStation(_shipRunModel.LoopIndex, ship);

            if (IsStandMissing() && _shipStationPads.CurrentPad != null)
                PlaceStand(_shipRunModel.LoopIndex);
        }

        private void ArriveAtStation(int stationLoop, ShipBase ship) {
            _settledLoop = stationLoop;
            _parcelSettleService.ServerSettleAtStation(stationLoop, ship);
            _parcelOfferService.ServerCreateOffers(stationLoop);
        }

        // The run's first pad is known a moment after the ship is bound, and a stand placed while the game map was still
        // loading went with the loading scene.
        private bool IsStandMissing() =>
            _shipRunModel.LoopIndex != _standLoop || _parcelServerModel.Stand == null;

        private void PlaceStand(int stationLoop) {
            _standLoop = stationLoop;
            _parcelStandSpawnService.ServerPlaceAt(_shipStationPads.CurrentPad);
        }

        private void OnHullDestroyed() {
            if (NetworkServer.active && _parcelsBindingModel.IsBound)
                LoseEverything();
        }

        private void LoseEverything() {
            _settledLoop = NO_STATION;
            _standLoop = NO_STATION;
            _parcelSettleService.ServerLoseAll();
            _parcelOfferService.ServerClearOffers();
            _parcelStandSpawnService.ServerRemove();
        }
    }
}
