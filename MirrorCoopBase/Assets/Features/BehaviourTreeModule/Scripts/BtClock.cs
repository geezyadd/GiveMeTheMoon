namespace Features.BehaviourTreeModule {
    // The tree's own time: nodes measure durations with it instead of Time.time, so a tree ticked at a low rate or
    // in a test keeps exact timing. Elapsed is a double so long sessions do not lose precision.
    public sealed class BtClock {
        public int TickCount { get; private set; }
        public double ElapsedSeconds { get; private set; }

        internal void Advance(float deltaTime) {
            TickCount++;
            ElapsedSeconds += deltaTime;
        }
    }
}
