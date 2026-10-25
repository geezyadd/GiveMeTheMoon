#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.ShipModule.Scripts.Debug {
    // The ship's debug actions' availability checks: each returns why the action cannot run now, or null.
    public interface IShipDebugActionConditions {
        public string ReadHostReason();
        public string ReadHostWithShipReason();
        public string ReadHostInFlightReason();
    }
}
#endif
