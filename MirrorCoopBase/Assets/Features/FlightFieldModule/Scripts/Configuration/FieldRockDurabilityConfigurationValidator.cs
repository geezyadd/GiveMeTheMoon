using System;
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
        }
    }
}
