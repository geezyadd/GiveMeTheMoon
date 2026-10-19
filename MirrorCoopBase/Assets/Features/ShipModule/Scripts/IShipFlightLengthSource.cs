namespace Features.ShipModule.Scripts {
    // The flight length of a loop in cruise seconds at flight speed 1.
    public interface IShipFlightLengthSource {
        public float GetFlightSeconds(int loopIndex);
    }
}
