using System;
using Zenject;

namespace Features.ShipModule.Scripts {
    // The one startup check of the flight length in the run config: first and per-loop seconds above zero, the cap
    // either off (0) or not below the first flight.
    public sealed class ShipRunConfigValidator : IInitializable {
        private readonly ShipRunConfig _shipRunConfig;

        public ShipRunConfigValidator(ShipRunConfig shipRunConfig) =>
            _shipRunConfig = shipRunConfig;

        public void Initialize() {
            if (_shipRunConfig.FirstFlightSeconds <= 0f)
                throw new InvalidOperationException($"{_shipRunConfig.name}: {nameof(ShipRunConfig.FirstFlightSeconds)} must be above 0, is {_shipRunConfig.FirstFlightSeconds}.");

            if (_shipRunConfig.PerLoopFlightSeconds <= 0f)
                throw new InvalidOperationException($"{_shipRunConfig.name}: {nameof(ShipRunConfig.PerLoopFlightSeconds)} must be above 0, is {_shipRunConfig.PerLoopFlightSeconds}.");

            float max = _shipRunConfig.MaxFlightSeconds;
            if (max != 0f && max < _shipRunConfig.FirstFlightSeconds)
                throw new InvalidOperationException(
                    $"{_shipRunConfig.name}: {nameof(ShipRunConfig.MaxFlightSeconds)} must be 0 (no cap) or at least {nameof(ShipRunConfig.FirstFlightSeconds)} ({_shipRunConfig.FirstFlightSeconds}), is {max}.");
        }
    }
}
