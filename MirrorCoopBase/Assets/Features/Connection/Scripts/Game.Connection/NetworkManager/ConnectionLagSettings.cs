namespace Game.Connection
{
    // The simulated link of one ConnectionLatencyProfile in the units the lag queue uses.
    public readonly struct ConnectionLagSettings
    {
        const double MILLISECONDS_PER_SECOND = 1000d;

        public readonly double LatencySeconds;
        public readonly double JitterSeconds;
        public readonly double LossPercent;

        public ConnectionLagSettings(double latencySeconds, double jitterSeconds, double lossPercent)
        {
            LatencySeconds = latencySeconds;
            JitterSeconds = jitterSeconds;
            LossPercent = lossPercent;
        }

        public static ConnectionLagSettings From(ConnectionLatencyProfile profile) =>
            new ConnectionLagSettings(
                profile.LatencyMilliseconds / MILLISECONDS_PER_SECOND,
                profile.JitterMilliseconds / MILLISECONDS_PER_SECOND,
                profile.LossPercent);
    }
}
