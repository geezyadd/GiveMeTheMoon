#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.ShipModule.Scripts {
    public interface IShipRouteDebug {
        public void DebugUseFlightMode(ShipFlightMode mode);
        public void DebugClearFlightMode();
    }
}
#endif
