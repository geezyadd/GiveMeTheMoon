using Features.GrabModule.Scripts.Hold;
using NUnit.Framework;

namespace Features.GrabModule.Editor.Tests {
    public sealed class HoldUseProgressTests {
        private const float HOLD_SECONDS = 2f;
        private const float STEP = 0.5f;
        private const float TOLERANCE = 0.0001f;
        private const uint TOOL = 7;
        private const uint OTHER_ITEM = 8;

        private HoldUseProgress _progress;

        [SetUp]
        public void SetUp() {
            _progress = new HoldUseProgress();
            _progress.Begin(HOLD_SECONDS, TOOL);
        }

        [Test]
        public void WhenHeldForTheWholeTime_ThenItCompletesOnce() {
            Assert.AreEqual(HoldUseStatus.Holding, Advance(HOLD_SECONDS - STEP));
            Assert.AreEqual(1f - STEP / HOLD_SECONDS, _progress.Progress, TOLERANCE);

            Assert.AreEqual(HoldUseStatus.Completed, Advance(STEP));
            Assert.IsFalse(_progress.IsActive, "A completed hold is still running.");
            Assert.AreEqual(HoldUseStatus.None, Advance(STEP), "A completed hold completed again.");
        }

        [Test]
        public void WhenReleased_ThenProgressResetsToZero() {
            Advance(STEP);
            _progress.Cancel();

            Assert.IsFalse(_progress.IsActive);
            Assert.AreEqual(0f, _progress.Progress, TOLERANCE);
        }

        [Test]
        public void WhenPlayerWalksAway_ThenTheHoldIsCancelled() {
            Advance(STEP);

            Assert.AreEqual(HoldUseStatus.Cancelled, _progress.Advance(STEP, new HoldUseCheck(true, false, TOOL)));
            Assert.AreEqual(0f, _progress.Progress, TOLERANCE);
        }

        [Test]
        public void WhenAnotherItemIsInTheHand_ThenTheHoldIsCancelled() {
            Advance(STEP);

            Assert.AreEqual(HoldUseStatus.Cancelled, _progress.Advance(STEP, new HoldUseCheck(true, true, OTHER_ITEM)));
        }

        [Test]
        public void WhenTargetStopsBeingUsable_ThenTheHoldIsCancelled() {
            Advance(STEP);

            Assert.AreEqual(HoldUseStatus.Cancelled, _progress.Advance(STEP, new HoldUseCheck(false, true, TOOL)));
        }

        [Test]
        public void WhenHoldStartsAgainAfterAReset_ThenItCountsFromZero() {
            Advance(HOLD_SECONDS - STEP);
            _progress.Advance(STEP, new HoldUseCheck(true, false, TOOL));
            _progress.Begin(HOLD_SECONDS, TOOL);

            Assert.AreEqual(HoldUseStatus.Holding, Advance(HOLD_SECONDS - STEP), "An earlier hold's time counted towards the new one.");
        }

        private HoldUseStatus Advance(float seconds) =>
            _progress.Advance(seconds, new HoldUseCheck(true, true, TOOL));
    }
}
