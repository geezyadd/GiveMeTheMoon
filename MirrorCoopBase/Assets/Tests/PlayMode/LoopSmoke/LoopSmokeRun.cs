#if UNITY_INCLUDE_TESTS
namespace Tests.PlayMode.LoopSmoke {
    // One run of the loop test: how many flights it plays, whether the test puts the rocket engines in at its first
    // station and checks the landings with them (a stage preset brings its own modules) and how it ends.
    public sealed class LoopSmokeRun {
        public int Flights { get; }
        public bool HasTestEngines { get; }
        public LoopSmokeRunEnd End { get; }

        public LoopSmokeRun(int flights, bool hasTestEngines, LoopSmokeRunEnd end) {
            Flights = flights;
            HasTestEngines = hasTestEngines;
            End = end;
        }
    }
}
#endif
