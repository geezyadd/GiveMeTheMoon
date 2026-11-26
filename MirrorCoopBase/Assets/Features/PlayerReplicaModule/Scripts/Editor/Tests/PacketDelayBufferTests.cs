using Features.PlayerReplicaModule.Packets;
using Features.PlayerReplicaModule.Receiving;
using NUnit.Framework;

namespace Features.PlayerReplicaModule.Editor.Tests {
    public sealed class PacketDelayBufferTests {
        private PacketDelayBuffer _buffer;

        [SetUp]
        public void SetUp() =>
            _buffer = new PacketDelayBuffer();

        [Test]
        public void WhenAPacketsMomentHasNotCome_ThenItWaits() {
            _buffer.Add(Packet(1, 10d));

            Assert.IsFalse(_buffer.Advance(9.9d));
            Assert.IsFalse(_buffer.HasCurrent);
        }

        [Test]
        public void WhenAPacketsMomentComes_ThenItBecomesCurrent() {
            _buffer.Add(Packet(1, 10d));

            Assert.IsTrue(_buffer.Advance(10d));
            Assert.AreEqual(1, _buffer.Current.Number);
        }

        [Test]
        public void WhenSeveralPacketsAreDue_ThenTheNewestDueOneIsCurrentAndTheRestWait() {
            _buffer.Add(Packet(1, 10d));
            _buffer.Add(Packet(2, 10.03d));
            _buffer.Add(Packet(3, 10.2d));

            Assert.IsTrue(_buffer.Advance(10.1d));
            Assert.AreEqual(2, _buffer.Current.Number);

            Assert.IsFalse(_buffer.Advance(10.15d));
            Assert.IsTrue(_buffer.Advance(10.2d));
            Assert.AreEqual(3, _buffer.Current.Number);
        }

        [Test]
        public void WhenRestarted_ThenTheGivenPacketIsCurrentAndNothingWaits() {
            _buffer.Add(Packet(1, 10d));
            _buffer.Add(Packet(2, 11d));

            _buffer.Restart(Packet(5, 12d));

            Assert.AreEqual(5, _buffer.Current.Number);
            Assert.IsFalse(_buffer.Advance(20d));
        }

        [Test]
        public void WhenCleared_ThenThereIsNoCurrent() {
            _buffer.Restart(Packet(1, 10d));

            _buffer.Clear();

            Assert.IsFalse(_buffer.HasCurrent);
        }

        private static BodyStatePacket Packet(ushort sequence, double serverTime) =>
            new() { Number = sequence, ServerTime = serverTime };
    }
}
