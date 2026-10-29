using System;
using System.Collections.Generic;
using Features.ParcelModule.Scripts.Rules;
using Zenject;

namespace Features.ParcelModule.Scripts.Configuration {
    // The one startup check of the parcel data: the crate, the stand and the sonar kind are set, the offer counts fit the stand
    // and the rewards make sense.
    public sealed class ParcelConfigurationValidator : IInitializable {
        private const string ERROR_FORMAT = "{0}: {1}";

        private readonly ParcelConfiguration _parcelConfiguration;

        public ParcelConfigurationValidator(ParcelConfiguration parcelConfiguration) =>
            _parcelConfiguration = parcelConfiguration;

        public void Initialize() {
            if (_parcelConfiguration.CratePrefab == null)
                Fail($"{nameof(ParcelConfiguration.CratePrefab)} is not set.");

            if (_parcelConfiguration.StandPrefab == null)
                Fail($"{nameof(ParcelConfiguration.StandPrefab)} is not set.");

            if (_parcelConfiguration.StolenSonarKind == null)
                Fail($"{nameof(ParcelConfiguration.StolenSonarKind)} is not set.");

            CheckOfferCounts();
            if (_parcelConfiguration.MinReward > _parcelConfiguration.MaxReward)
                Fail($"{nameof(ParcelConfiguration.MinReward)} {_parcelConfiguration.MinReward} is above {nameof(ParcelConfiguration.MaxReward)} {_parcelConfiguration.MaxReward}.");
        }

        private void CheckOfferCounts() {
            IReadOnlyList<float> weights = _parcelConfiguration.OfferCountWeights;
            if (weights.Count == 0 || weights.Count > ParcelOfferRule.MAX_OFFERS + 1)
                Fail($"{nameof(ParcelConfiguration.OfferCountWeights)} must weigh 0..{ParcelOfferRule.MAX_OFFERS} offers.");

            float total = 0f;
            foreach (float weight in weights) {
                if (weight < 0f)
                    Fail($"{nameof(ParcelConfiguration.OfferCountWeights)} has a negative weight.");

                total += weight;
            }

            if (total <= 0f)
                Fail($"{nameof(ParcelConfiguration.OfferCountWeights)} weighs nothing.");
        }

        private void Fail(string message) =>
            throw new InvalidOperationException(string.Format(ERROR_FORMAT, _parcelConfiguration.name, message));
    }
}
