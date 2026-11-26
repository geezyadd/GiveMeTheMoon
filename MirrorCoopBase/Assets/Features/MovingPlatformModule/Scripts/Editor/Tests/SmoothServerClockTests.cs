using Features.GameCoreModule.Contracts;
using NUnit.Framework;

namespace Features.MovingPlatformModule.Editor.Tests {
    public sealed class SmoothServerClockTests {
        private const double STEP = 0.02d;
        private const double TOLERANCE = 0.000001d;

        private SmoothServerClock _clock;

        [SetUp]
        public void SetUp() {
            _clock = new SmoothServerClock(0.05d, 1d, 0.5d);
            _clock.Start(100d);
        }

        [Test]
        public void WhenInStep_ThenEachPhysicsStepMovesItByTheStepTime() {
            _clock.Compare(100d, 100d);
            _clock.Step(STEP);

            Assert.AreEqual(100.02d, _clock.Time, TOLERANCE);
        }

        [Test]
        public void WhenTheServerIsAhead_ThenTheStepsRunAtMostFivePercentFaster() {
            _clock.Compare(100.3d, 100d);
            _clock.Step(STEP);

            Assert.AreEqual(100d + STEP * 1.05d, _clock.Time, TOLERANCE);
        }

        [Test]
        public void WhenTheServerIsSlightlyBehind_ThenTheStepsRunALittleSlower() {
            _clock.Compare(99.98d, 100d);
            _clock.Step(STEP);

            Assert.AreEqual(100d + STEP * 0.98d, _clock.Time, TOLERANCE);
        }

        [Test]
        public void WhenTheGapIsLarge_ThenItIsClosedAtOnce() {
            _clock.Compare(102d, 100d);

            Assert.AreEqual(102d, _clock.Time, TOLERANCE);
            _clock.Step(STEP);
            Assert.AreEqual(102.02d, _clock.Time, TOLERANCE);
        }

        [Test]
        public void WhenFramesStall_ThenNoStepJumpsAndTheClockCatchesUp() {
            double server = 100d;
            double previous = _clock.Time;
            for (int frame = 0; frame < 200; frame++) {
                server += frame % 20 == 0 ? 0.05d : STEP;
                _clock.Compare(server, _clock.Time);
                _clock.Step(STEP);
                Assert.LessOrEqual(_clock.Time - previous, STEP * 1.05d + TOLERANCE, $"jump at frame {frame}");
                previous = _clock.Time;
            }
        }
    }
}
