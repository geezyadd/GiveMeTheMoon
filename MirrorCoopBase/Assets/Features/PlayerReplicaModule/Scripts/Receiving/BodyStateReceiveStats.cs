namespace Features.PlayerReplicaModule.Receiving {
    // Counts what one copy receives between two reports: packets taken, packets dropped as old or out of order, how old
    // the taken packets were, how often the copy had to snap and how far it was from its owner's packet (the error the
    // correction sees each physics step, at the copy's delay). For the debug window and the coop test; it changes
    // nothing in the sync.
    public sealed class BodyStateReceiveStats {
        private int _accepted;
        private int _dropped;
        private int _snaps;
        private double _ageSum;
        private double _errorSum;
        private double _maxError;
        private int _errorSteps;
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

        public void RecordSnap(double now) {
            StartIfNeeded(now);
            _snaps++;
        }

        public void RecordError(double meters, double now) {
            StartIfNeeded(now);
            _errorSum += meters;
            _errorSteps++;
            if (meters > _maxError)
                _maxError = meters;
        }

        // Returns the counts since the last report (or the first packet) and starts a new report.
        public BodyStateReceiveReport TakeReport(double now) {
            double seconds = _hasStart ? now - _reportStartedAt : 0d;
            var report = new BodyStateReceiveReport(
                _accepted,
                _dropped,
                _snaps,
                seconds > 0d ? _accepted / seconds : 0d,
                _accepted > 0 ? _ageSum / _accepted : 0d,
                _lastAge,
                _errorSteps > 0 ? _errorSum / _errorSteps : 0d,
                _maxError);

            _accepted = 0;
            _dropped = 0;
            _snaps = 0;
            _ageSum = 0d;
            _errorSum = 0d;
            _maxError = 0d;
            _errorSteps = 0;
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
