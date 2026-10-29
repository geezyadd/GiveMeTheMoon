using System.Collections.Generic;
using Features.ParcelModule.Scripts.Configuration;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Rules {
    public sealed class ParcelOfferRule : IParcelOfferRule {
        // The delivery stand has two slots.
        public const int MAX_OFFERS = 2;

        private readonly ParcelConfiguration _parcelConfiguration;

        public ParcelOfferRule(ParcelConfiguration parcelConfiguration) =>
            _parcelConfiguration = parcelConfiguration;

        public int CalculateOfferCount(float roll) {
            IReadOnlyList<float> weights = _parcelConfiguration.OfferCountWeights;
            float total = 0f;
            foreach (float weight in weights)
                total += weight;

            float left = Mathf.Clamp01(roll) * total;
            for (int count = 0; count < weights.Count; count++) {
                left -= weights[count];
                if (left < 0f)
                    return count;
            }

            return weights.Count - 1;
        }

        public long CalculateReward(float roll) {
            int step = _parcelConfiguration.RewardStep;
            int steps = (_parcelConfiguration.MaxReward - _parcelConfiguration.MinReward) / step;
            int picked = Mathf.Min(steps, Mathf.FloorToInt(Mathf.Clamp01(roll) * (steps + 1)));
            return _parcelConfiguration.MinReward + (long)picked * step;
        }
    }
}
