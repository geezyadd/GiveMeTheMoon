namespace Features.PlayerReplicaModule.Receiving {
    // One report of BodyStateReceiveStats.
    public readonly struct BodyStateReceiveReport {
        public readonly int Accepted;
        public readonly int Dropped;
        public readonly int Snaps;
        public readonly double PacketsPerSecond;
        public readonly double AverageAgeSeconds;
        public readonly double LastAgeSeconds;
        // The copy's distance to its owner's packet over the physics steps it was corrected in, metres.
        public readonly double AverageErrorMeters;
        public readonly double MaxErrorMeters;

        public BodyStateReceiveReport(
            int accepted,
            int dropped,
            int snaps,
            double packetsPerSecond,
            double averageAgeSeconds,
            double lastAgeSeconds,
            double averageErrorMeters,
            double maxErrorMeters) {
            Accepted = accepted;
            Dropped = dropped;
            Snaps = snaps;
            PacketsPerSecond = packetsPerSecond;
            AverageAgeSeconds = averageAgeSeconds;
            LastAgeSeconds = lastAgeSeconds;
            AverageErrorMeters = averageErrorMeters;
            MaxErrorMeters = maxErrorMeters;
        }
    }
}
