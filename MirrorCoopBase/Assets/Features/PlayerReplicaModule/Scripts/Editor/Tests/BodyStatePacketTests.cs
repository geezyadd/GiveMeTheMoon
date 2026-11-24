using Features.PlayerReplicaModule.Packets;
using NUnit.Framework;

namespace Features.PlayerReplicaModule.Editor.Tests {
    public sealed class BodyStatePacketTests {
        private const float YAW_TOLERANCE_DEGREES = 0.01f;

        [TestCase(0f)]
        [TestCase(90f)]
        [TestCase(179.5f)]
        [TestCase(359.99f)]
        public void WhenYawIsPackedAndUnpacked_ThenItStaysWithinTheStep(float yaw) {
            float unpacked = YawCompression.Unpack(YawCompression.Pack(yaw));

            Assert.AreEqual(0f, UnityEngine.Mathf.DeltaAngle(yaw, unpacked), YAW_TOLERANCE_DEGREES);
        }

        [Test]
        public void WhenYawIsNegativeOrPastAFullTurn_ThenItWrapsIntoOneTurn() {
            Assert.AreEqual(YawCompression.Pack(270f), YawCompression.Pack(-90f));
            Assert.AreEqual(YawCompression.Pack(10f), YawCompression.Pack(370f));
        }

        [Test]
        public void WhenYawIsAlmostAFullTurn_ThenItPacksAsZero() =>
            Assert.AreEqual(0, YawCompression.Pack(359.999f));

        [Test]
        public void WhenFlagsAreSet_ThenHasSeesOnlyThem() {
            var packet = new BodyStatePacket { States = BodyStateFlags.Sprinting | BodyStateFlags.Grounded };

            Assert.IsTrue(packet.Has(BodyStateFlags.Sprinting));
            Assert.IsTrue(packet.Has(BodyStateFlags.Grounded));
            Assert.IsFalse(packet.Has(BodyStateFlags.JumpHeld));
            Assert.IsFalse(packet.Has(BodyStateFlags.Dead));
        }
    }
}
