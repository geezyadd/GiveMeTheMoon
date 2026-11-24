using Features.BodySyncModule.Packets;
using NUnit.Framework;

namespace Features.BodySyncModule.Editor.Tests {
    public sealed class JumpCounterTrackerTests {
        private JumpCounterTracker _tracker;

        [SetUp]
        public void SetUp() =>
            _tracker = new JumpCounterTracker();

        [Test]
        public void WhenTheFirstPacketArrives_ThenThereIsNoJump() =>
            Assert.IsFalse(_tracker.ConsumeJump(7));

        [Test]
        public void WhenTheCounterStaysTheSame_ThenThereIsNoJump() {
            _tracker.ConsumeJump(7);

            Assert.IsFalse(_tracker.ConsumeJump(7));
        }

        [Test]
        public void WhenTheCounterGrowsByOne_ThenTheCopyJumpsOnce() {
            _tracker.ConsumeJump(7);

            Assert.IsTrue(_tracker.ConsumeJump(8));
            Assert.IsFalse(_tracker.ConsumeJump(8));
        }

        [Test]
        public void WhenSeveralJumpsWereMissed_ThenTheCopyJumpsOnlyOnce() {
            _tracker.ConsumeJump(7);

            Assert.IsTrue(_tracker.ConsumeJump(10));
            Assert.IsFalse(_tracker.ConsumeJump(10));
        }

        [Test]
        public void WhenTheCounterWrapsPastTheTop_ThenItIsAJump() {
            _tracker.ConsumeJump(255);

            Assert.IsTrue(_tracker.ConsumeJump(0));
        }
    }
}
