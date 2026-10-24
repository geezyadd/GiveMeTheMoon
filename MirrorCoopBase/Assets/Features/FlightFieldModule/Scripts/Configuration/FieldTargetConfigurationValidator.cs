using System;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // The one startup check of the target balloon configuration.
    public sealed class FieldTargetConfigurationValidator : IInitializable {
        private readonly FieldTargetConfiguration _fieldTargetConfiguration;

        public FieldTargetConfigurationValidator(FieldTargetConfiguration fieldTargetConfiguration) =>
            _fieldTargetConfiguration = fieldTargetConfiguration;

        public void Initialize() {
            FieldTargetConfiguration configuration = _fieldTargetConfiguration;
            if (configuration.FirstLoopCount.x < 0 || configuration.FirstLoopCount.y < configuration.FirstLoopCount.x)
                throw new InvalidOperationException($"{configuration.name}: the loop 0 balloon count {configuration.FirstLoopCount} must be a range from 0 up.");

            if (configuration.HeightMeters.y < configuration.HeightMeters.x)
                throw new InvalidOperationException($"{configuration.name}: the balloon height range {configuration.HeightMeters} runs downwards.");

            if (configuration.ReleaseRadiusMeters <= configuration.BuildRadiusMeters)
                throw new InvalidOperationException($"{configuration.name}: the release radius must be above the build radius.");

            if (configuration.PopEffect == null)
                throw new InvalidOperationException($"{configuration.name}: the pop effect is empty.");
        }
    }
}
