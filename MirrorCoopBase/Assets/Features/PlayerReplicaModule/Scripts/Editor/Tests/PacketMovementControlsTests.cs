using Features.PlayerReplicaModule.Packets;
using Features.PlayerReplicaModule.Receiving;
using NUnit.Framework;
using UnityEngine;

namespace Features.PlayerReplicaModule.Editor.Tests {
    public sealed class PacketMovementControlsTests {
        private const float TOLERANCE = 0.0001f;

        private PacketMovementControls _source;

        [SetUp]
        public void SetUp() =>
            _source = new PacketMovementControls();

        [Test]
        public void WhenAPacketArrives_ThenTheControlsFollowIt() {
            _source.Apply(new BodyStatePacket {
                WalkDirection = new Vector2(0.6f, -0.8f),
                States = BodyStateFlags.Sprinting | BodyStateFlags.JumpHeld
            });

            Vector3 wish = _source.ReadWish();
            Assert.AreEqual(0.6f, wish.x, TOLERANCE);
            Assert.AreEqual(0f, wish.y, TOLERANCE);
            Assert.AreEqual(-0.8f, wish.z, TOLERANCE);
            Assert.IsTrue(_source.SprintHeld);
            Assert.IsTrue(_source.JumpHeld);
        }

        [Test]
        public void WhenTheFirstPacketArrives_ThenThereIsNoJumpPress() {
            _source.Apply(new BodyStatePacket { JumpCount = 5 });

            Assert.IsFalse(_source.ConsumeJumpPress());
        }

        [Test]
        public void WhenTheJumpCounterGrows_ThenThereIsOnePressUntilConsumed() {
            _source.Apply(new BodyStatePacket { JumpCount = 5 });
            _source.Apply(new BodyStatePacket { JumpCount = 8 });

            Assert.IsTrue(_source.ConsumeJumpPress());
            Assert.IsFalse(_source.ConsumeJumpPress());
        }

        [Test]
        public void WhenTheSourceIsReset_ThenTheControlsAndThePendingPressAreGone() {
            _source.Apply(new BodyStatePacket { JumpCount = 1, WalkDirection = Vector2.up });
            _source.Apply(new BodyStatePacket { JumpCount = 2, WalkDirection = Vector2.up });

            _source.Reset();

            Assert.IsFalse(_source.ConsumeJumpPress());
            Assert.AreEqual(Vector3.zero, _source.ReadWish());
            _source.Apply(new BodyStatePacket { JumpCount = 3 });
            Assert.IsFalse(_source.ConsumeJumpPress());
        }
    }
}
