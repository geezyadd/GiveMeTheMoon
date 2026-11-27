using NUnit.Framework;

namespace Features.BehaviourTreeModule.Editor.Tests {
    // The delay counts tree clock from the tick it starts on.
    public sealed class BtDelayTests {
        private const float DELAY = 1f;
        private const float FRAME = 0.25f;
        private const int TICKS_WITHIN_DELAY = 4;

        private BtTestTree _tree;

        [SetUp]
        public void SetUp() =>
            _tree = BtTestTree.CreateBuilt(builder => builder
                .Delay("Delay", DELAY)
                .Build());

        [Test]
        public void WhenLessThanTheDelayPassedSinceTheStart_ThenTheDelayRuns() {
            BtStatus result = BtStatus.Inactive;
            for (int i = 0; i < TICKS_WITHIN_DELAY; i++)
                result = _tree.Update(FRAME);

            Assert.AreEqual(BtStatus.Running, result);
        }

        [Test]
        public void WhenTheDelayPassedSinceTheStart_ThenTheDelaySucceeds() {
            for (int i = 0; i < TICKS_WITHIN_DELAY; i++)
                _tree.Update(FRAME);

            BtStatus result = _tree.Update(FRAME);

            Assert.AreEqual(BtStatus.Success, result);
        }

        [Test]
        public void WhenOneLongTickCoversTheDelay_ThenTheDelaySucceedsOnTheNextTick() {
            _tree.Update(FRAME);

            BtStatus result = _tree.Update(DELAY);

            Assert.AreEqual(BtStatus.Success, result);
        }
    }
}
