using UnityEngine;

namespace Features.ShipModule.Scripts {
    // Reads the ship's stats into the numbers a flight is planned with: route work, dodge range, flight speed.
    internal sealed class ShipFlightStatService : IShipFlightStatService {
        private readonly ShipRunConfig _config;
        private readonly IFlightStatContributor[] _contributors = System.Array.Empty<IFlightStatContributor>();

        public ShipFlightStatService(ShipRunConfig config) {
            _config = config;
        }

        public FlightRunStats Sample(ShipBase ship, int loopIndex) {
            FlightRunStats stats = new FlightRunStats {
                DodgeRangeScale = 1f,
                CruiseSeconds = EvaluateRouteWork(loopIndex)
            };

            ShipSocket[] sockets = ship != null ? ship.Sockets : null;
            stats.TotalThrust = ship != null ? ship.GetStatFull(ShipStatType.FlightSpeed) : 0f;

            float dodge = ship != null ? ship.GetStatFull(ShipStatType.DodgeRange) : 0f;
            if (dodge > 0f)
                stats.DodgeRangeScale = dodge;

            for (int i = 0; i < _contributors.Length; i++) {
                if (_contributors[i] != null)
                    _contributors[i].Contribute(sockets, loopIndex, stats);
            }

            stats.CruiseSeconds = Mathf.Max(1f, stats.CruiseSeconds);
            stats.DodgeRangeScale = Mathf.Max(0.1f, stats.DodgeRangeScale);
            return stats;
        }

        public float EvaluateRouteWork(int loopIndex) =>
            _config.RouteWorkSeconds + loopIndex * _config.PerLoopCruiseSeconds;

        public float ReadFlightSpeed(ShipBase ship) {
            float speed = ship != null ? ship.GetStatFull(ShipStatType.FlightSpeed) : 1f;
            return Mathf.Max(ShipTransit.MinSpeed, speed);
        }
    }
}
