#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.ShipModule.Scripts {
    // Editor / development only: a fixed flight length in cruise seconds for every loop, so test runs stay short.
    public interface IShipFlightLengthDebug {
        public void DebugUseFlightSeconds(float seconds);
        public void DebugClearFlightSeconds();
    }
}
#endif
