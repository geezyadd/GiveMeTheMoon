using System;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // The one startup check of the ore configuration: a drift range, a fade no longer than any kind lives.
    public sealed class FieldOreConfigurationValidator : IInitializable {
        private readonly FieldOreConfiguration _fieldOreConfiguration;
        private readonly FieldOreCatalog _fieldOreCatalog;

        public FieldOreConfigurationValidator(FieldOreConfiguration fieldOreConfiguration, FieldOreCatalog fieldOreCatalog) {
            _fieldOreConfiguration = fieldOreConfiguration;
            _fieldOreCatalog = fieldOreCatalog;
        }

        public void Initialize() {
            if (_fieldOreConfiguration.MaxDriftSpeedMetersPerSecond < _fieldOreConfiguration.MinDriftSpeedMetersPerSecond)
                throw new InvalidOperationException($"{_fieldOreConfiguration.name}: the fastest drift is below the slowest.");

            foreach (FieldOreDefinition ore in _fieldOreCatalog.Ores) {
                if (ore != null && ore.LifetimeSeconds < _fieldOreConfiguration.FadeSeconds)
                    throw new InvalidOperationException($"{ore.name}: lives {ore.LifetimeSeconds} s, shorter than the fade of {_fieldOreConfiguration.FadeSeconds} s.");
            }
        }
    }
}
