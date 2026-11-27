using System;

namespace Features.BehaviourTreeModule {
    // Lets a host tick its tree at a fixed rate. Every frame's time reaches the tree: a tick passes all the time
    // gathered since the previous one, so the tree clock never loses time.
    public sealed class BtTickRateLimiter {
        private const string NEGATIVE_RATE = "A tick rate cannot be negative.";

        private readonly float _tickInterval;

        private float _pendingSeconds;
        private float _phaseSeconds;

        public BtTickRateLimiter(float ticksPerSecond) {
            if (ticksPerSecond < 0f)
                throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), ticksPerSecond, NEGATIVE_RATE);

            _tickInterval = ticksPerSecond > 0f ? 1f / ticksPerSecond : 0f;
        }

        public bool TryAdvance(float deltaTime, out float treeDeltaTime) {
            treeDeltaTime = 0f;
            _pendingSeconds += deltaTime;
            _phaseSeconds += deltaTime;
            if (_phaseSeconds < _tickInterval)
                return false;

            treeDeltaTime = _pendingSeconds;
            _pendingSeconds = 0f;
            _phaseSeconds = IsBehindByAWholeTick() ? 0f : _phaseSeconds - _tickInterval;
            return true;
        }

        // After a long frame the limiter ticks once and drops the extra phase instead of ticking in a burst.
        private bool IsBehindByAWholeTick() =>
            _phaseSeconds - _tickInterval >= _tickInterval;
    }
}
