namespace Features.ShipModule.Scripts {
    public interface IShipFlightStatService {
        public FlightRunStats Sample(ShipBase ship, int loopIndex);
        public float EvaluateRouteWork(int loopIndex);
        public float ReadFlightSpeed(ShipBase ship);
    }
}
