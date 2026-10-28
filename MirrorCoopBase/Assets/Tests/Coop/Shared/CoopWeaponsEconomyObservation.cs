#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Tests.Coop.Shared {
    // The fields only the economy and weapons scenarios fill (CoopWeaponsEconomyObserver): module items lying loose
    // (not installed), the ship's broken sockets, open breaches and hull, the field's popped balloons and broken rocks,
    // what the local player holds and its hold-to-use progress.
    public sealed partial class CoopObservation {
        public int LooseModuleItems;
        public string[] BrokenSockets;
        public string[] OpenBreaches;
        public float Hull;
        public int[] PoppedTargets;
        public string[] BrokenRocks;
        public string HeldItem;
        public float HoldProgress;
    }
}
#endif
