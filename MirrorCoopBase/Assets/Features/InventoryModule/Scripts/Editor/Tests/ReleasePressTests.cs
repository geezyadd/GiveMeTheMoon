using Features.InventoryModule.Scripts;
using NUnit.Framework;

namespace Features.InventoryModule.Editor.Tests {
    // The release button: a tap drops, a hold throws once at the hold time, and the release after a throw does nothing.
    public sealed class ReleasePressTests {
        private const float HOLD_SECONDS = 0.3f;
        private const float PRESS_TIME = 10f;

        [Test]
        public void WhenReleasedBeforeTheHoldTime_ThenItIsATap() {
            ReleasePress press = new ReleasePress(HOLD_SECONDS);
            press.Press(PRESS_TIME);

            Assert.IsFalse(press.TryCompleteHold(PRESS_TIME + 0.1f));
            Assert.IsTrue(press.TryEndAsTap());
        }

        [Test]
        public void WhenHeldToTheHoldTime_ThenItIsAHoldAndTheReleaseIsNoTap() {
            ReleasePress press = new ReleasePress(HOLD_SECONDS);
            press.Press(PRESS_TIME);

            Assert.IsTrue(press.TryCompleteHold(PRESS_TIME + HOLD_SECONDS));
            Assert.IsFalse(press.TryCompleteHold(PRESS_TIME + HOLD_SECONDS * 2f));
            Assert.IsFalse(press.TryEndAsTap());
        }

        [Test]
        public void WhenCanceled_ThenNeitherATapNorAHoldFollows() {
            ReleasePress press = new ReleasePress(HOLD_SECONDS);
            press.Press(PRESS_TIME);
            press.Cancel();

            Assert.IsFalse(press.TryCompleteHold(PRESS_TIME + HOLD_SECONDS));
            Assert.IsFalse(press.TryEndAsTap());
        }
    }
}
