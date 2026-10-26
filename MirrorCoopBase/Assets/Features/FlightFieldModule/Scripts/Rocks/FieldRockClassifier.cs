using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Small or large from the data: the recipe's always-large flag, else the diameter against the configured limit.
    public sealed class FieldRockClassifier : IFieldRockClassifier {
        private readonly FieldRockDurabilityConfiguration _fieldRockDurabilityConfiguration;

        public FieldRockClassifier(FieldRockDurabilityConfiguration fieldRockDurabilityConfiguration) =>
            _fieldRockDurabilityConfiguration = fieldRockDurabilityConfiguration;

        public FieldRockSize GetSize(float diameter, bool isAlwaysLarge) =>
            isAlwaysLarge || diameter > _fieldRockDurabilityConfiguration.SmallMaxDiameterMeters ? FieldRockSize.Large : FieldRockSize.Small;
    }
}
