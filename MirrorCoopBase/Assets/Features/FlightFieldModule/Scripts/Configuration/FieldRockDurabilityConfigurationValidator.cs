using System;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // The one startup check of the rock durability configuration.
    public sealed class FieldRockDurabilityConfigurationValidator : IInitializable {
        private readonly FieldRockDurabilityConfiguration _fieldRockDurabilityConfiguration;

        public FieldRockDurabilityConfigurationValidator(FieldRockDurabilityConfiguration fieldRockDurabilityConfiguration) =>
            _fieldRockDurabilityConfiguration = fieldRockDurabilityConfiguration;

        public void Initialize() {
            if (_fieldRockDurabilityConfiguration.SmallMaxDiameterMeters <= 0f)
                throw new InvalidOperationException($"{_fieldRockDurabilityConfiguration.name}: the small rock diameter must be above 0.");

            if (_fieldRockDurabilityConfiguration.BreakEffect == null)
                throw new InvalidOperationException($"{_fieldRockDurabilityConfiguration.name}: the break effect is empty.");

            ValidateHitPoints(_fieldRockDurabilityConfiguration.HitPointsByDiameter);
        }

        private void ValidateHitPoints(AnimationCurve hitPointsByDiameter) {
            if (hitPointsByDiameter == null || hitPointsByDiameter.length == 0)
                throw new InvalidOperationException($"{_fieldRockDurabilityConfiguration.name}: the hit points by diameter have no keys.");

            foreach (Keyframe key in hitPointsByDiameter.keys) {
                if (key.value <= 0f)
                    throw new InvalidOperationException($"{_fieldRockDurabilityConfiguration.name}: the hit points at {key.time} m must be above 0, not {key.value}.");
            }
        }
    }
}
