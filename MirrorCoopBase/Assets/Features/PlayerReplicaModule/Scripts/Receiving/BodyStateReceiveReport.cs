namespace Features.PlayerReplicaModule.Receiving {
    // One report of BodyStateReceiveStats.
    public readonly struct BodyStateReceiveReport {
        public readonly int Accepted;
        public readonly int Dropped;
        public readonly int Snaps;
        public readonly double PacketsPerSecond;
        public readonly double AverageAgeSeconds;
        public readonly double LastAgeSeconds;

        public BodyStateReceiveReport(int accepted, int dropped, int snaps, double packetsPerSecond, double averageAgeSeconds, double lastAgeSeconds) {
            Accepted = accepted;
            Dropped = dropped;
            Snaps = snaps;
            PacketsPerSecond = packetsPerSecond;
            AverageAgeSeconds = averageAgeSeconds;
            LastAgeSeconds = lastAgeSeconds;
        }
    }
}
