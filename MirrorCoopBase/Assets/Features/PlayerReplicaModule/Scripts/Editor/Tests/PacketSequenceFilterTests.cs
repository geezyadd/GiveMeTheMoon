using Features.PlayerReplicaModule.Packets;
using NUnit.Framework;

namespace Features.PlayerReplicaModule.Editor.Tests {
    public sealed class PacketSequenceFilterTests {
        private PacketSequenceFilter _filter;

        [SetUp]
        public void SetUp() =>
            _filter = new PacketSequenceFilter();

        [Test]
        public void WhenTheFirstPacketArrives_ThenAnyNumberIsAccepted() =>
            Assert.IsTrue(_filter.TryAccept(40000));

        [Test]
        public void WhenANewerPacketArrives_ThenItIsAccepted() {
            _filter.TryAccept(10);

            Assert.IsTrue(_filter.TryAccept(11));
            Assert.IsTrue(_filter.TryAccept(15));
        }

        [Test]
        public void WhenAnOlderOrRepeatedPacketArrives_ThenItIsDropped() {
            _filter.TryAccept(10);

            Assert.IsFalse(_filter.TryAccept(9));
            Assert.IsFalse(_filter.TryAccept(10));
        }

        [Test]
        public void WhenTheNumberWrapsPastTheTop_ThenTheWrappedOneIsNewer() {
            _filter.TryAccept(65534);

            Assert.IsTrue(_filter.TryAccept(1));
            Assert.IsFalse(_filter.TryAccept(65535));
        }

        [Test]
        public void WhenTheFilterIsReset_ThenAnOlderNumberIsAcceptedAgain() {
            _filter.TryAccept(100);
            _filter.Reset();

            Assert.IsTrue(_filter.TryAccept(5));
        }
    }
}
