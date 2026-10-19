#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.ShipModule.Scripts.Debug {
    // Editor / development only: the config's flight length unless a debug length is set in ShipFlightLengthDebugModel.
    public sealed class ShipDebugFlightLengthSource : IShipFlightLengthSource {
        private readonly IShipFlightLengthSource _shipFlightLengthSource;
        private readonly ShipFlightLengthDebugModel _shipFlightLengthDebugModel;

        public ShipDebugFlightLengthSource(IShipFlightLengthSource shipFlightLengthSource, ShipFlightLengthDebugModel shipFlightLengthDebugModel) {
            _shipFlightLengthSource = shipFlightLengthSource;
            _shipFlightLengthDebugModel = shipFlightLengthDebugModel;
        }

        public float GetFlightSeconds(int loopIndex) =>
            _shipFlightLengthDebugModel.FlightSeconds > 0f ? _shipFlightLengthDebugModel.FlightSeconds : _shipFlightLengthSource.GetFlightSeconds(loopIndex);
    }
}
#endif
