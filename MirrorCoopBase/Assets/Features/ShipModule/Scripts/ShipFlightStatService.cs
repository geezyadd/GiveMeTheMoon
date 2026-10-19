using UnityEngine;

namespace Features.ShipModule.Scripts {
    // Flight stats of a launch: the route work (cruise seconds at flight speed 1) of a loop and the module stats.
    public sealed class ShipFlightStatService : IShipFlightStatService {
        private readonly ShipRunConfig _config;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly ShipFlightLengthDebugModel _shipFlightLengthDebugModel;

        public ShipFlightStatService(ShipRunConfig config, ShipFlightLengthDebugModel shipFlightLengthDebugModel) {
            _config = config;
            _shipFlightLengthDebugModel = shipFlightLengthDebugModel;
        }
#else
        public ShipFlightStatService(ShipRunConfig config) =>
            _config = config;
#endif

        public FlightRunStats Sample(ShipBase ship, int loopIndex) {
            FlightRunStats stats = new FlightRunStats {
                DodgeRangeScale = 1f,
                CruiseSeconds = EvaluateRouteWork(loopIndex)
            };

            stats.TotalThrust = ship != null ? ship.Modules.GetStatFull(ShipStatType.FlightSpeed) : 0f;

            float dodge = ship != null ? ship.Modules.GetStatFull(ShipStatType.DodgeRange) : 0f;
            if (dodge > 0f)
                stats.DodgeRangeScale = dodge;

            stats.CruiseSeconds = Mathf.Max(1f, stats.CruiseSeconds);
            stats.DodgeRangeScale = Mathf.Max(0.1f, stats.DodgeRangeScale);
            return stats;
        }

        // seconds(loop) = first + loop * per loop, capped by the max when it is set; times CruiseSpeed it is the
        // distance from the hover point to the next station.
        public float EvaluateRouteWork(int loopIndex) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_shipFlightLengthDebugModel.FlightSeconds > 0f)
                return _shipFlightLengthDebugModel.FlightSeconds;
#endif
            float seconds = _config.FirstFlightSeconds + Mathf.Max(0, loopIndex) * _config.PerLoopFlightSeconds;
            return _config.MaxFlightSeconds > 0f ? Mathf.Min(seconds, _config.MaxFlightSeconds) : seconds;
        }

        public float ReadFlightSpeed(ShipBase ship) {
            float speed = ship != null ? ship.Modules.GetStatFull(ShipStatType.FlightSpeed) : 1f;
            return Mathf.Max(ShipTransit.MinSpeed, speed);
        }
    }
}
