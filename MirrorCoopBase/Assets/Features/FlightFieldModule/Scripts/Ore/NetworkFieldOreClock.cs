using Mirror;

namespace Features.FlightFieldModule.Scripts.Ore {
    public sealed class NetworkFieldOreClock : IFieldOreClock {
        public double Now => NetworkTime.time;
    }
}
