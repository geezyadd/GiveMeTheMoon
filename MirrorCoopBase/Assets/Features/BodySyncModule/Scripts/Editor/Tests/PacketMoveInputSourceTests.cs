using Features.BodySyncModule.Packets;
using Features.BodySyncModule.Receiving;
using NUnit.Framework;
using UnityEngine;

namespace Features.BodySyncModule.Editor.Tests {
    public sealed class PacketMoveInputSourceTests {
        private const float TOLERANCE = 0.0001f;

        private PacketMoveInputSource _source;

        [SetUp]
        public void SetUp() =>
            _source = new PacketMoveInputSource();

        [Test]
        public void WhenAPacketArrives_ThenTheControlsFollowIt() {
            _source.Apply(new BodyStatePacket {
                Wish = new Vector2(0.6f, -0.8f),
                Flags = BodyStateFlags.Sprinting | BodyStateFlags.JumpHeld
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
            _source.Apply(new BodyStatePacket { JumpCounter = 5 });

            Assert.IsFalse(_source.ConsumeJumpPress());
        }

        [Test]
        public void WhenTheJumpCounterGrows_ThenThereIsOnePressUntilConsumed() {
            _source.Apply(new BodyStatePacket { JumpCounter = 5 });
            _source.Apply(new BodyStatePacket { JumpCounter = 8 });

            Assert.IsTrue(_source.ConsumeJumpPress());
            Assert.IsFalse(_source.ConsumeJumpPress());
        }

        [Test]
        public void WhenTheSourceIsReset_ThenTheControlsAndThePendingPressAreGone() {
            _source.Apply(new BodyStatePacket { JumpCounter = 1, Wish = Vector2.up });
            _source.Apply(new BodyStatePacket { JumpCounter = 2, Wish = Vector2.up });

            _source.Reset();

            Assert.IsFalse(_source.ConsumeJumpPress());
            Assert.AreEqual(Vector3.zero, _source.ReadWish());
            _source.Apply(new BodyStatePacket { JumpCounter = 3 });
            Assert.IsFalse(_source.ConsumeJumpPress());
        }
    }
}
