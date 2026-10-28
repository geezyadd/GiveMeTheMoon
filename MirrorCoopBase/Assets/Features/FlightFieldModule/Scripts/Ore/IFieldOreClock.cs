namespace Features.FlightFieldModule.Scripts.Ore {
    // The time every peer places the ore by: the network time, the same on the server and the clients.
    public interface IFieldOreClock {
        public double Now { get; }
    }
}
