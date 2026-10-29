using System.Collections.Generic;
using Features.ParcelModule.Data;
using Features.ParcelModule.Data.Generated;
using Features.ParcelModule.Scripts.Rules;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Services {
    // A station offers its parcels to the next station; taking one is free and marks the offer taken, so the stand
    // shows it gone for every player and nobody takes it twice.
    public sealed class ParcelOfferService : IParcelOfferService {
        private readonly IReadOnlyParcelsModel _parcelsModel;
        private readonly ParcelsBindingModel _parcelsBindingModel;
        private readonly IParcelOfferRule _parcelOfferRule;
        private readonly IParcelCrateService _parcelCrateService;

        public ParcelOfferService(
            IReadOnlyParcelsModel parcelsModel,
            ParcelsBindingModel parcelsBindingModel,
            IParcelOfferRule parcelOfferRule,
            IParcelCrateService parcelCrateService) {
            _parcelsModel = parcelsModel;
            _parcelsBindingModel = parcelsBindingModel;
            _parcelOfferRule = parcelOfferRule;
            _parcelCrateService = parcelCrateService;
        }

        public void ServerCreateOffers(int stationLoop) {
            ServerClearOffers();
            int count = _parcelOfferRule.CalculateOfferCount(Random.value);
            for (int slot = 0; slot < count; slot++)
                _parcelsBindingModel.Writer.ServerAddOffers(new ParcelOffer(slot, _parcelOfferRule.CalculateReward(Random.value), stationLoop + 1, false));
        }

        public void ServerClearOffers() {
            if (_parcelsModel.Offers.Count > 0)
                _parcelsBindingModel.Writer.ServerClearOffers();
        }

        public ParcelTakeResult ServerTryTakeOffer(int slot, Vector3 cratePosition, Quaternion crateRotation) {
            IReadOnlyList<ParcelOffer> offers = _parcelsModel.Offers;
            for (int i = 0; i < offers.Count; i++) {
                ParcelOffer offer = offers[i];
                if (offer.Slot != slot)
                    continue;

                if (offer.IsTaken)
                    return ParcelTakeResult.AlreadyTaken;

                _parcelsBindingModel.Writer.ServerSetOffers(i, new ParcelOffer(offer.Slot, offer.Reward, offer.DestinationLoop, true));
                _parcelCrateService.ServerSpawn(cratePosition, crateRotation, offer.Reward, offer.DestinationLoop);
                return ParcelTakeResult.Taken;
            }

            return ParcelTakeResult.NoOffer;
        }
    }
}
