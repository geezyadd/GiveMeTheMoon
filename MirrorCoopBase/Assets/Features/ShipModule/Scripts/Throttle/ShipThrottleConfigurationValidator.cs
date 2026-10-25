using System;
using System.Collections.Generic;
using Zenject;

namespace Features.ShipModule.Scripts.Throttle {
    // The one startup check of the throttle configuration: named detents from Stop (0) up to Full (1) in order, and a
    // launch mark the handle can reach.
    public sealed class ShipThrottleConfigurationValidator : IInitializable {
        private readonly ShipThrottleConfiguration _shipThrottleConfiguration;

        public ShipThrottleConfigurationValidator(ShipThrottleConfiguration shipThrottleConfiguration) =>
            _shipThrottleConfiguration = shipThrottleConfiguration;

        public void Initialize() {
            string name = _shipThrottleConfiguration.name;
            IReadOnlyList<ShipThrottleConfiguration.Detent> detents = _shipThrottleConfiguration.Detents;
            if (detents.Count < 2)
                throw new InvalidOperationException($"{name}: needs at least the Stop and Full detents, has {detents.Count}.");

            if (detents[0].Position != 0f || detents[detents.Count - 1].Position != 1f)
                throw new InvalidOperationException($"{name}: the first detent must sit at 0 and the last at 1.");

            for (int i = 0; i < detents.Count; i++)
                ValidateDetent(name, detents, i);

            float launchMark = _shipThrottleConfiguration.LaunchMark;
            if (launchMark <= 0f || launchMark > 1f)
                throw new InvalidOperationException($"{name}: {nameof(ShipThrottleConfiguration.LaunchMark)} must be in (0, 1], is {launchMark}.");
        }

        private static void ValidateDetent(string name, IReadOnlyList<ShipThrottleConfiguration.Detent> detents, int index) {
            ShipThrottleConfiguration.Detent detent = detents[index];
            if (string.IsNullOrEmpty(detent.Name))
                throw new InvalidOperationException($"{name}: detent {index} has no name.");

            if (index > 0 && detent.Position <= detents[index - 1].Position)
                throw new InvalidOperationException($"{name}: detent {detent.Name} must sit after {detents[index - 1].Name}.");
        }
    }
}
