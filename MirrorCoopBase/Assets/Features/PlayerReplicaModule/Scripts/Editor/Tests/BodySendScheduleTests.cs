using Features.PlayerReplicaModule.Packets;
using Features.PlayerReplicaModule.Sending;
using NUnit.Framework;
using UnityEngine;

namespace Features.PlayerReplicaModule.Editor.Tests {
    public sealed class BodySendScheduleTests {
        private const float SEND_INTERVAL = 1f / 30f;
        private const float IDLE_INTERVAL = 1f / 5f;
        private const float IDLE_SPEED = 0.05f;
        private const double START = 10d;

        private static readonly BodyStatePacket Standing = new();
        private static readonly BodyStatePacket Walking = new() { RelativeVelocity = new Vector3(3f, 0f, 0f), WalkDirection = Vector2.up };

        private BodySendSchedule _schedule;

        [SetUp]
        public void SetUp() =>
            _schedule = new BodySendSchedule();

        [Test]
        public void WhenNothingWasSentYet_ThenThePacketIsDue() =>
            Assert.IsTrue(IsDue(Standing, START));

        [Test]
        public void WhenTheBodyMoves_ThenPacketsGoAtThePacketRate() {
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
        public void WhenAStandingBodyStartsToMove_ThenThePacketGoesAtThePacketRate() {
            _schedule.MarkSent(Standing, START);

            Assert.IsTrue(IsDue(Walking, START + SEND_INTERVAL * 1.01d));
        }

        [Test]
        public void WhenOnlyAFlagChanges_ThenThePacketGoesAtOnce() {
            _schedule.MarkSent(Standing, START);
            var landed = new BodyStatePacket { States = BodyStateFlags.Grounded };

            Assert.IsTrue(IsDue(landed, START));
        }

        [Test]
        public void WhenTheOwnerJumpedInPlace_ThenThePacketGoesAtOnce() {
            _schedule.MarkSent(Standing, START);
            var jumped = new BodyStatePacket { JumpCount = 1 };

            Assert.IsTrue(IsDue(jumped, START));
        }

        [Test]
        public void WhenTheOwnerAttacked_ThenThePacketGoesAtOnce() {
            _schedule.MarkSent(Standing, START);
            var attacked = new BodyStatePacket { AttackCount = 1 };

            Assert.IsTrue(IsDue(attacked, START));
        }

        [Test]
        public void WhenTheOwnerEmoted_ThenThePacketGoesAtOnce() {
            _schedule.MarkSent(Standing, START);
            var emoted = new BodyStatePacket { EmoteCount = 1 };

            Assert.IsTrue(IsDue(emoted, START));
        }

        [Test]
        public void WhenTheOwnerStartsOrStopsWalking_ThenThePacketGoesAtOnce() {
            _schedule.MarkSent(Standing, START);
            Assert.IsTrue(IsDue(Walking, START));

            _schedule.MarkSent(Walking, START);
            Assert.IsTrue(IsDue(Standing, START));
        }

        [Test]
        public void WhenTheWalkDirectionOnlyTurns_ThenThePacketWaitsForThePacketRate() {
            _schedule.MarkSent(Walking, START);
            var turned = new BodyStatePacket { RelativeVelocity = Walking.RelativeVelocity, WalkDirection = Vector2.right };

            Assert.IsFalse(IsDue(turned, START + SEND_INTERVAL * 0.5d));
        }

        [Test]
        public void WhenTheOwnerOnlyTurnsTheLook_ThenThePacketGoesAtThePacketRate() {
            _schedule.MarkSent(Standing, START);
            var turned = new BodyStatePacket { LookYaw = YawCompression.Pack(45f) };

            Assert.IsTrue(IsDue(turned, START + SEND_INTERVAL * 1.01d));
        }

        private bool IsDue(in BodyStatePacket packet, double now) =>
            _schedule.IsDue(packet, now, SEND_INTERVAL, IDLE_INTERVAL, IDLE_SPEED);
    }
}
