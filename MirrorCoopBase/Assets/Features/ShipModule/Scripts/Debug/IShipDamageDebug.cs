#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.ShipModule.Scripts.Debug {
    // Public for the loop tests: they break modules and open breaches the same way the debug key does.
    public interface IShipDamageDebug {
        public bool DebugBreakModule(string socketId);
        public bool DebugOpenBreach(string pointId);
        // A random working breakable module and a random free damage point, as F5 does.
        public void DebugBreakRandom();
    }
}
#endif
