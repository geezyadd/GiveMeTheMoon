using UnityEngine;

namespace Features.ShipModule.Scripts {
    // seconds(loop) = first + loop * per loop, capped by the max when it is set; times CruiseSpeed it is the distance
    // from the hover point to the next station.
    public sealed class ShipRunConfigFlightLengthSource : IShipFlightLengthSource {
        private readonly ShipRunConfig _config;

        public ShipRunConfigFlightLengthSource(ShipRunConfig config) =>
            _config = config;

        public float GetFlightSeconds(int loopIndex) {
            float seconds = _config.FirstFlightSeconds + Mathf.Max(0, loopIndex) * _config.PerLoopFlightSeconds;
            return _config.MaxFlightSeconds > 0f ? Mathf.Min(seconds, _config.MaxFlightSeconds) : seconds;
        }
    }
}
