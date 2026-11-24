namespace Features.BodySyncModule.Receiving {
    // One report of BodyStateReceiveStats.
    public readonly struct BodyStateReceiveReport {
        public readonly int Accepted;
        public readonly int Dropped;
        public readonly double PacketsPerSecond;
        public readonly double AverageAgeSeconds;
        public readonly double LastAgeSeconds;

        public BodyStateReceiveReport(int accepted, int dropped, double packetsPerSecond, double averageAgeSeconds, double lastAgeSeconds) {
            Accepted = accepted;
            Dropped = dropped;
            PacketsPerSecond = packetsPerSecond;
            AverageAgeSeconds = averageAgeSeconds;
            LastAgeSeconds = lastAgeSeconds;
        }
    }
}
