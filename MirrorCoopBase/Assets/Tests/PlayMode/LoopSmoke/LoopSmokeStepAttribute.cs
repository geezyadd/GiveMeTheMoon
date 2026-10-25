#if UNITY_INCLUDE_TESTS
using System;

namespace Tests.PlayMode.LoopSmoke {
    // Registers a method of a loop check as a step of LoopSmokeTest: the slot it runs in and its order there (lower
    // first). Flight is the n-th flight of the run (LoopIndex for a run that starts on loop 0). The method takes no
    // arguments and returns IEnumerator or void; its class has a public constructor taking LoopSmokeContext or an
    // empty one.
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class LoopSmokeStepAttribute : Attribute {
        public LoopSmokePhase Phase { get; }
        public int Run { get; }
        public int Flight { get; }
        public int Order { get; }

        // A flight phase: BeforeTakeoff, InCruise or AfterLanding.
        public LoopSmokeStepAttribute(LoopSmokePhase phase, int run, int flight, int order) {
            Phase = phase;
            Run = run;
            Flight = flight;
            Order = order;
        }

        // A run phase: InLobby or AfterRun.
        public LoopSmokeStepAttribute(LoopSmokePhase phase, int run, int order) : this(phase, run, 0, order) { }

        // InMenu.
        public LoopSmokeStepAttribute(LoopSmokePhase phase, int order) : this(phase, 0, 0, order) { }
    }
}
#endif
