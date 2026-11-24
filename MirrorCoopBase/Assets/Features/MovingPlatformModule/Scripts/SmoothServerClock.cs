using System;

namespace Features.MovingPlatformModule {
    // A clock that follows the server's time but never jumps between physics steps: it moves on by each step's time,
    // a few percent faster or slower until it meets the server time again. When this peer's frames stall (the editor in
    // the background, a long frame, a minimised game), the game time falls behind the real time the network time
    // follows; driven by the network time directly, a platform would leap and throw off what it carries. Only a large
    // gap (a long stall) is closed at once.
    public sealed class SmoothServerClock {
        private readonly double _maxRateChange;
        private readonly double _catchUpPerSecond;
        private readonly double _jumpGap;

        private double _rate = 1d;

        public SmoothServerClock(double maxRateChange, double catchUpPerSecond, double jumpGap) {
            _maxRateChange = maxRateChange;
            _catchUpPerSecond = catchUpPerSecond;
            _jumpGap = jumpGap;
        }

        public bool IsStarted { get; private set; }
        public double Time { get; private set; }

        public void Start(double serverTime) {
            Time = serverTime;
            _rate = 1d;
            IsStarted = true;
        }

        // Once per frame: how far the server time is ahead of this clock (as of now) sets the pace of the next steps.
        public void Compare(double serverTimeNow, double clockTimeNow) {
            double gap = serverTimeNow - clockTimeNow;
            if (Math.Abs(gap) > _jumpGap) {
                Time += gap;
                _rate = 1d;
                return;
            }

            _rate = 1d + Math.Clamp(gap * _catchUpPerSecond, -_maxRateChange, _maxRateChange);
        }

        // Once per physics step.
        public void Step(double stepSeconds) =>
            Time += stepSeconds * _rate;
    }
}
