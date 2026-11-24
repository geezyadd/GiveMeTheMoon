using Features.BodySyncModule.Packets;
using Features.BodySyncModule.Sending;
using NUnit.Framework;
using UnityEngine;

namespace Features.BodySyncModule.Editor.Tests {
    public sealed class BodySendScheduleTests {
        private const float SEND_INTERVAL = 1f / 30f;
        private const float IDLE_INTERVAL = 1f / 5f;
        private const float IDLE_SPEED = 0.05f;
        private const double START = 10d;

        private static readonly BodyStatePacket Standing = new();
        private static readonly BodyStatePacket Walking = new() { LocalVelocity = new Vector3(3f, 0f, 0f), Wish = Vector2.up };

        private BodySendSchedule _schedule;

        [SetUp]
        public void SetUp() =>
            _schedule = new BodySendSchedule();

        [Test]
        public void WhenNothingWasSentYet_ThenThePacketIsDue() =>
            Assert.IsTrue(IsDue(Standing, START));

        [Test]
        public void WhenTheBodyMoves_ThenPacketsGoAtTheSendRate() {
            _schedule.MarkSent(Walking, START);

            Assert.IsFalse(IsDue(Walking, START + SEND_INTERVAL * 0.5d));
            Assert.IsTrue(IsDue(Walking, START + SEND_INTERVAL * 1.01d));
        }

        [Test]
        public void WhenTheBodyStandsAndNothingChanges_ThenPacketsGoAtTheIdleRate() {
            _schedule.MarkSent(Standing, START);

            Assert.IsFalse(IsDue(Standing, START + SEND_INTERVAL * 2d));
            Assert.IsTrue(IsDue(Standing, START + IDLE_INTERVAL * 1.01d));
        }

        [Test]
        public void WhenAStandingBodyStartsToMove_ThenThePacketGoesAtTheSendRate() {
            _schedule.MarkSent(Standing, START);

            Assert.IsTrue(IsDue(Walking, START + SEND_INTERVAL * 1.01d));
        }

        [Test]
        public void WhenOnlyAFlagChanges_ThenThePacketGoesAtTheSendRate() {
            _schedule.MarkSent(Standing, START);
            var landed = new BodyStatePacket { Flags = BodyStateFlags.Grounded };

            Assert.IsTrue(IsDue(landed, START + SEND_INTERVAL * 1.01d));
        }

        [Test]
        public void WhenTheOwnerJumpedInPlace_ThenThePacketGoesAtTheSendRate() {
            _schedule.MarkSent(Standing, START);
            var jumped = new BodyStatePacket { JumpCounter = 1 };

            Assert.IsTrue(IsDue(jumped, START + SEND_INTERVAL * 1.01d));
        }

        [Test]
        public void WhenTheOwnerOnlyTurnsTheLook_ThenThePacketGoesAtTheSendRate() {
            _schedule.MarkSent(Standing, START);
            var turned = new BodyStatePacket { FacingYaw = YawCompression.Pack(45f) };

            Assert.IsTrue(IsDue(turned, START + SEND_INTERVAL * 1.01d));
        }

        private bool IsDue(in BodyStatePacket packet, double now) =>
            _schedule.IsDue(packet, now, SEND_INTERVAL, IDLE_INTERVAL, IDLE_SPEED);
    }
}
