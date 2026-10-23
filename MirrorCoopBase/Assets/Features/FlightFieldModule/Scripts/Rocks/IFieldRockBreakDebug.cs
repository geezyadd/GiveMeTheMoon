#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.FlightFieldModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Editor / development only, server: breaks the breakable rock nearest to the run's ship through the one break
    // entry (cause ShipCollision, no instigator).
    public interface IFieldRockBreakDebug {
        public FieldRockBreakResult DebugBreakNearestRock(out FieldRockId id);
    }
}
#endif
