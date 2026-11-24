namespace Features.BodySyncModule.Receiving {
    // Counts what one copy receives between two reports: packets taken, packets dropped as old or out of order, and
    // how old the taken packets were. For the debug window; it changes nothing in the sync.
    public sealed class BodyStateReceiveStats {
        private int _accepted;
        private int _dropped;
        private double _ageSum;
        private double _lastAge;
        private double _reportStartedAt;
        private bool _hasStart;

        public void RecordAccepted(double ageSeconds, double now) {
            StartIfNeeded(now);
            _accepted++;
            _ageSum += ageSeconds;
            _lastAge = ageSeconds;
        }

        public void RecordDropped(double now) {
            StartIfNeeded(now);
            _dropped++;
        }

        // Returns the counts since the last report (or the first packet) and starts a new report.
        public BodyStateReceiveReport TakeReport(double now) {
            double seconds = _hasStart ? now - _reportStartedAt : 0d;
            var report = new BodyStateReceiveReport(
                _accepted,
                _dropped,
                seconds > 0d ? _accepted / seconds : 0d,
                _accepted > 0 ? _ageSum / _accepted : 0d,
                _lastAge);

            _accepted = 0;
            _dropped = 0;
            _ageSum = 0d;
            _reportStartedAt = now;
            _hasStart = true;
            return report;
        }

        private void StartIfNeeded(double now) {
            if (_hasStart)
                return;

            _reportStartedAt = now;
            _hasStart = true;
        }
    }
}
