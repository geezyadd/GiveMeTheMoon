using Features.PlayerReplicaModule.Receiving;
using NUnit.Framework;

namespace Features.PlayerReplicaModule.Editor.Tests {
    public sealed class PlacementFenceTests {
        private const double WAIT = 1d;

        private PlacementFence _fence;

        [SetUp]
        public void SetUp() =>
            _fence = new PlacementFence();

        [Test]
        public void WhenNothingWasPlaced_ThenNoPacketIsBlocked() =>
            Assert.IsFalse(_fence.Blocks(0, 0d, WAIT));

        [Test]
        public void WhenAPacketFromBeforeTheMoveArrives_ThenItIsBlocked() {
            _fence.Begin(3, 10d);

            Assert.IsTrue(_fence.Blocks(2, 10.2d, WAIT));
            Assert.IsTrue(_fence.IsWaiting);
        }

        [Test]
        public void WhenThePacketOfTheMoveArrives_ThenItAndAllAfterItPass() {
            _fence.Begin(3, 10d);

            Assert.IsFalse(_fence.Blocks(3, 10.2d, WAIT));
            Assert.IsFalse(_fence.IsWaiting);
            Assert.IsFalse(_fence.Blocks(2, 10.3d, WAIT));
        }

        [Test]
        public void WhenTheWaitRunsOut_ThenAnOldPacketPasses() {
            _fence.Begin(3, 10d);

            Assert.IsFalse(_fence.Blocks(2, 11d, WAIT));
            Assert.IsFalse(_fence.IsWaiting);
        }

        [Test]
        public void WhenTheCountWrapped_ThenTheMoveNumberZeroIsMatched() {
            _fence.Begin(0, 10d);

            Assert.IsTrue(_fence.Blocks(255, 10.1d, WAIT));
            Assert.IsFalse(_fence.Blocks(0, 10.2d, WAIT));
        }

        [Test]
        public void WhenCleared_ThenNothingIsBlocked() {
            _fence.Begin(3, 10d);
            _fence.Clear();

            Assert.IsFalse(_fence.Blocks(2, 10.1d, WAIT));
        }
    }
}
