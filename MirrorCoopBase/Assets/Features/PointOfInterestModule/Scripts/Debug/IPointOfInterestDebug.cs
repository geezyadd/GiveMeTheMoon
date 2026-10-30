#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.PointOfInterestModule.Scripts.Server;

namespace Features.PointOfInterestModule.Scripts.Debug {
    // Debug only, on the host: the debug window's point of interest actions. Public for the loop tests.
    public interface IPointOfInterestDebug {
        // Puts the ship just outside the zone of the nearest point whose container stands, facing it; false without one.
        public bool TryDebugMoveShipToNearest(out int pointId);
        public PointOfInterestTriggerResult DebugTriggerNearest(out int pointId);
    }
}
#endif
