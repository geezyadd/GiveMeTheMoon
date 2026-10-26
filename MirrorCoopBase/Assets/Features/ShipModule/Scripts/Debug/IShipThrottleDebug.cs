#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.ShipModule.Scripts.Debug {
    // Debug only, on the host: sets the throttle as if a player pushed the handle there (at a station past the launch
    // mark it takes off). Public for the loop tests.
    public interface IShipThrottleDebug {
        public void DebugSetThrottle(float position);
    }
}
#endif
