#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.ShipModule.Scripts.Debug {
    // Public for the loop tests: they hit the hull the same way the debug keys do.
    public interface IShipHullDebug {
        public void DebugDamage(float amount);
        public void DebugDestroy();
    }
}
#endif
