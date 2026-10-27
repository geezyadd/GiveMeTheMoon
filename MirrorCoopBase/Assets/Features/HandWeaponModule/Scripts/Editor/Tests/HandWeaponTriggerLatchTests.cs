using Features.HandWeaponModule.Scripts;
using NUnit.Framework;

namespace Features.HandWeaponModule.Editor.Tests {
    public sealed class HandWeaponTriggerLatchTests {
        private const float MIN_REPORT_SECONDS = 0.25f;
        private const float FRAME_SECONDS = 0.05f;

        private HandWeaponTriggerLatch _latch;

        [SetUp]
        public void SetUp() =>
            _latch = new HandWeaponTriggerLatch(MIN_REPORT_SECONDS);

        [Test]
        public void WhenButtonIsPressed_ThenTheTriggerIsReportedAtOnce() =>
            Assert.IsTrue(_latch.Advance(true, FRAME_SECONDS));

        [Test]
        public void WhenAShortClickIsLetGo_ThenTheTriggerStaysReportedForTheMinimumTime() {
            _latch.Advance(true, FRAME_SECONDS);

            Assert.IsTrue(_latch.Advance(false, FRAME_SECONDS), "A one-frame click was dropped at once.");
            Assert.IsTrue(_latch.Advance(false, MIN_REPORT_SECONDS - 3f * FRAME_SECONDS), "The click was dropped before the minimum time.");
            Assert.IsFalse(_latch.Advance(false, 2f * FRAME_SECONDS), "The click stays reported after the minimum time.");
        }

        [Test]
        public void WhenButtonIsHeld_ThenTheTriggerStaysReportedUntilItIsLetGo() {
            _latch.Advance(true, FRAME_SECONDS);
            _latch.Advance(true, MIN_REPORT_SECONDS * 4f);

            Assert.IsTrue(_latch.Advance(true, FRAME_SECONDS));
            Assert.IsFalse(_latch.Advance(false, FRAME_SECONDS));
        }

        [Test]
        public void WhenAShortClickEnds_ThenTheNextPressIsReportedAgain() {
            _latch.Advance(true, FRAME_SECONDS);
            _latch.Advance(false, MIN_REPORT_SECONDS * 2f);
            Assert.IsFalse(_latch.IsReported);

            Assert.IsTrue(_latch.Advance(true, FRAME_SECONDS));
        }
    }
}
