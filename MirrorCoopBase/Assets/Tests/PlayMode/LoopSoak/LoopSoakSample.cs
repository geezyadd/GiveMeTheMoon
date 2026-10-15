#if UNITY_INCLUDE_TESTS
using Tests.PlayMode.LoopSmoke;

namespace Tests.PlayMode.LoopSoak {
    // One row of the soak report: what a loop took and what the session held after it.
    public sealed class LoopSoakSample {
        public int Loop { get; }
        public float Seconds { get; }
        public long ManagedBytes { get; }
        public long AllocatedBytes { get; }
        public LoopSoakSpawnCensus Census { get; }
        public int WorldShifts { get; }
        public float ShipDistanceFromOrigin { get; }
        public int FieldPlacements { get; }
        public int FieldObjects { get; }
        public int FieldPooledObjects { get; }

        public LoopSoakSample(
            int loop,
            float seconds,
            long managedBytes,
            long allocatedBytes,
            LoopSoakSpawnCensus census,
            int worldShifts,
            float shipDistanceFromOrigin,
            LoopSmokeFieldCheck field) {
            Loop = loop;
            Seconds = seconds;
            ManagedBytes = managedBytes;
            AllocatedBytes = allocatedBytes;
            Census = census;
            WorldShifts = worldShifts;
            ShipDistanceFromOrigin = shipDistanceFromOrigin;
            FieldPlacements = field.LastPlacements;
            FieldObjects = field.LastFieldObjects;
            FieldPooledObjects = field.PooledObjects;
        }
    }
}
#endif
