using Features.BodySyncModule.Correction;
using NUnit.Framework;

namespace Features.BodySyncModule.Editor.Tests {
    public sealed class GroundMismatchTimerTests {
        private const float LIMIT = 0.5f;
        private const float STEP = 0.02f;

        private GroundMismatchTimer _timer;

        [SetUp]
        public void SetUp() =>
            _timer = new GroundMismatchTimer();

        [Test]
        public void WhenOwnerAndCopyAgree_ThenTheCopyIsNotStuck() {
            Tick(true, true, 100);

            Assert.IsFalse(_timer.IsStuck(LIMIT));
        }

        [Test]
        public void WhenTheyDisagreeBrieflyThenAgree_ThenTheTimeStartsOver() {
            Tick(true, false, 20);
            Tick(false, false, 1);
            Tick(true, false, 20);

            Assert.IsFalse(_timer.IsStuck(LIMIT));
        }

        [Test]
        public void WhenTheyDisagreeLongerThanTheLimit_ThenTheCopyIsStuck() {
            Tick(true, false, 30);

            Assert.IsTrue(_timer.IsStuck(LIMIT));
        }

        private void Tick(bool ownerGrounded, bool copyGrounded, int steps) {
            for (int i = 0; i < steps; i++)
                _timer.Tick(ownerGrounded, copyGrounded, STEP);
        }
    }
}
