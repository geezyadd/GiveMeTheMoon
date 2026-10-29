using System;
using System.Collections.Generic;
using Features.ParcelModule.Data;
using Features.ParcelModule.Scripts.Rules;
using Features.ParcelModule.Scripts.Server;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Core;

namespace Features.ParcelModule.Scripts.Services {
    // A delivered parcel pays its reward into the crew wallet and its crate goes; a lost one only goes. Every parcel is
    // settled once: it leaves the model as it is settled.
    public sealed class ParcelSettleService : IParcelSettleService {
        private const string NO_WALLET_ERROR = "Parcel {0} was delivered with no crew wallet bound on the server.";

        private readonly ParcelServerModel _parcelServerModel;
        private readonly IParcelEntryService _parcelEntryService;
        private readonly IParcelCrateService _parcelCrateService;
        private readonly IParcelAboardService _parcelAboardService;
        private readonly IParcelSettleRule _parcelSettleRule;
        private readonly ICrewWalletBindingModel _crewWalletBindingModel;

        public ParcelSettleService(
            ParcelServerModel parcelServerModel,
            IParcelEntryService parcelEntryService,
            IParcelCrateService parcelCrateService,
            IParcelAboardService parcelAboardService,
            IParcelSettleRule parcelSettleRule,
            ICrewWalletBindingModel crewWalletBindingModel) {
            _parcelServerModel = parcelServerModel;
            _parcelEntryService = parcelEntryService;
            _parcelCrateService = parcelCrateService;
            _parcelAboardService = parcelAboardService;
            _parcelSettleRule = parcelSettleRule;
            _crewWalletBindingModel = crewWalletBindingModel;
        }

        public void ServerSettleAtStation(int stationLoop, ShipBase ship) {
            foreach (uint netId in CollectNetIds()) {
                if (_parcelServerModel.TryGet(netId, out ParcelServerRecord record) && _parcelEntryService.TryFind(netId, out ParcelEntry entry))
                    Settle(record, entry, stationLoop, ship);
            }
        }

        public void ServerLoseAll() {
            foreach (uint netId in CollectNetIds())
                _parcelCrateService.ServerDestroy(netId);
        }

        private void Settle(ParcelServerRecord record, in ParcelEntry entry, int stationLoop, ShipBase ship) {
            bool isAboard = record.Crate != null && _parcelAboardService.IsAboard(record.Crate.Position, ship);
            ParcelSettlement settlement = _parcelSettleRule.Decide(entry, stationLoop, isAboard);
            if (settlement == ParcelSettlement.Keep)
                return;

            if (settlement == ParcelSettlement.Deliver)
                Pay(entry);

            _parcelCrateService.ServerDestroy(record.NetId);
        }

        private void Pay(in ParcelEntry entry) {
            if (_crewWalletBindingModel.IsBound == false)
                throw new InvalidOperationException(string.Format(NO_WALLET_ERROR, entry.NetId));

            _crewWalletBindingModel.Wallet.ServerEarn(entry.Reward);
        }

        // Settling removes records, so it walks a copy; rare (a landing, a lost ship), so the list is made per call.
        private List<uint> CollectNetIds() {
            List<uint> netIds = new(_parcelServerModel.Records.Count);
            foreach (ParcelServerRecord record in _parcelServerModel.Records)
                netIds.Add(record.NetId);

            return netIds;
        }
    }
}
