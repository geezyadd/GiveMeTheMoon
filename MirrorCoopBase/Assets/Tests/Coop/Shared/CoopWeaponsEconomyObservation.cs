#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Tests.Coop.Shared {
    // The fields only the economy and weapons scenarios fill (CoopWeaponsEconomyObserver): module items lying loose
    // (not installed), the field's popped balloons and broken rocks, what the local player holds and its hold-to-use
    // progress. The hull, broken sockets and open breaches are shared with the flight tests (CoopObservation.Flight.cs).
    public sealed partial class CoopObservation {
        public int LooseModuleItems;
        public int[] PoppedTargets;
        public string[] BrokenRocks;
        public string HeldItem;
        public float HoldProgress;
    }
}
#endif
