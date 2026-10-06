namespace Features.ShipModule.Scripts {
    public interface IShipFlightStatService {
        FlightRunStats Sample(ShipBase ship, int loopIndex);
        float EvaluateRouteWork(int loopIndex);
        float ReadFlightSpeed(ShipBase ship);
    }
}
