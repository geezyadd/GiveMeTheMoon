#if UNITY_INCLUDE_TESTS
using System;

namespace Tests.PlayMode.LoopSmoke {
    public readonly struct LoopSmokeSlot : IEquatable<LoopSmokeSlot> {
        public LoopSmokePhase Phase { get; }
        public int Run { get; }
        public int Flight { get; }

        public LoopSmokeSlot(LoopSmokePhase phase, int run, int flight) {
            Phase = phase;
            Run = run;
            Flight = flight;
        }

        public static LoopSmokeSlot OfRun(LoopSmokePhase phase, int run) =>
            new LoopSmokeSlot(phase, run, 0);

        public static LoopSmokeSlot OfFlight(LoopSmokePhase phase, int run, int flight) =>
            new LoopSmokeSlot(phase, run, flight);

        public static LoopSmokeSlot InMenu() =>
            new LoopSmokeSlot(LoopSmokePhase.InMenu, 0, 0);

        public bool Equals(LoopSmokeSlot other) =>
            Phase == other.Phase && Run == other.Run && Flight == other.Flight;

        public override bool Equals(object obj) =>
            obj is LoopSmokeSlot other && Equals(other);

        public override int GetHashCode() =>
            ((int)Phase * 397 ^ Run) * 397 ^ Flight;

        public override string ToString() {
            if (Phase == LoopSmokePhase.InMenu)
                return Phase.ToString();

            return Flight == 0 ? $"{Phase} run {Run}" : $"{Phase} run {Run} flight {Flight}";
        }
    }
}
#endif
