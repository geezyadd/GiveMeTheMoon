namespace Features.FieldAgentModule.Scripts.Agents {
    // Server: what the last agent tick and batch cost, for the debug measurements and the tests.
    public sealed class FieldAgentDiagnosticsModel {
        private const float AVERAGE_SHARE = 0.05f;

        public double AverageTickMilliseconds { get; private set; }
        public double MaxTickMilliseconds { get; private set; }
        public int BatchesSent { get; private set; }
        // How often the last-resort push had to put an agent out of a rock sphere: the steering alone should keep 0.
        public int RockPushOuts { get; private set; }

        public void RecordTick(double milliseconds) {
            AverageTickMilliseconds += (milliseconds - AverageTickMilliseconds) * AVERAGE_SHARE;
            if (milliseconds > MaxTickMilliseconds)
                MaxTickMilliseconds = milliseconds;
        }

        public void RecordBatch() =>
            BatchesSent++;

        public void RecordRockPushOut() =>
            RockPushOuts++;

        public void ResetMeasurements() {
            AverageTickMilliseconds = 0d;
            MaxTickMilliseconds = 0d;
            BatchesSent = 0;
        }
    }
}
