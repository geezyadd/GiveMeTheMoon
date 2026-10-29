using Features.ShipModule.Scripts.Overboard;
using NUnit.Framework;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipOverboardPullMotionTests {
        private const float PULL_SPEED = 9f;
        private const float FIXED_STEP = 0.02f;
        private const float TOLERANCE = 0.001f;
        private const float WORLD_SHIFT_METERS = 800f;

        private readonly ShipOverboardPullMotion _pullMotion = new ShipOverboardPullMotion();

        [Test]
        public void WhenBodyIsFarFromTarget_ThenItComesAtPullSpeedOnTopOfShipVelocity() {
            Vector3 shipVelocity = new Vector3(0f, 0f, 8f);

            Vector3 velocity = _pullMotion.Step(Vector3.zero, new Vector3(30f, 0f, 0f), shipVelocity, PULL_SPEED, FIXED_STEP);

            Assert.AreEqual(PULL_SPEED, velocity.x, TOLERANCE);
            Assert.AreEqual(shipVelocity.z, velocity.z, TOLERANCE);
        }

        [Test]
        public void WhenBodyIsAlmostAtTarget_ThenOneStepNeverPassesIt() {
            Vector3 target = new Vector3(0.05f, 0f, 0f);

            Vector3 velocity = _pullMotion.Step(Vector3.zero, target, Vector3.zero, PULL_SPEED, FIXED_STEP);

            Assert.AreEqual(target.x, velocity.x * FIXED_STEP, TOLERANCE);
        }

        [Test]
        public void WhenBodyIsAtTarget_ThenItMovesWithTheShip() {
            Vector3 shipVelocity = new Vector3(3f, 0f, 4f);

            Vector3 velocity = _pullMotion.Step(Vector3.one, Vector3.one, shipVelocity, PULL_SPEED, FIXED_STEP);

            Assert.AreEqual(shipVelocity, velocity);
        }

        [Test]
        public void WhenTrackedShipMovesSteadily_ThenVelocityIsItsSpeed() {
            WorldShiftSafeVelocity tracker = new WorldShiftSafeVelocity();
            tracker.Track(Vector3.zero, FIXED_STEP);
            tracker.Track(new Vector3(0f, 0f, 0.16f), FIXED_STEP);

            Assert.AreEqual(8f, tracker.Velocity.z, TOLERANCE);
        }

        [Test]
        public void WhenWorldShiftJumpsShip_ThenLastVelocityIsKept() {
            WorldShiftSafeVelocity tracker = new WorldShiftSafeVelocity();
            tracker.Track(Vector3.zero, FIXED_STEP);
            tracker.Track(new Vector3(0f, 0f, 0.16f), FIXED_STEP);
            tracker.Track(new Vector3(0f, 0f, WORLD_SHIFT_METERS), FIXED_STEP);

            Assert.AreEqual(8f, tracker.Velocity.z, TOLERANCE);
        }

        [Test]
        public void WhenTrackIsReset_ThenFirstSampleOnlyRestartsIt() {
            WorldShiftSafeVelocity tracker = new WorldShiftSafeVelocity();
            tracker.Track(Vector3.zero, FIXED_STEP);
            tracker.Track(new Vector3(0f, 0f, 0.16f), FIXED_STEP);
            tracker.Reset();
            tracker.Track(new Vector3(50f, 0f, 0f), FIXED_STEP);

            Assert.AreEqual(8f, tracker.Velocity.z, TOLERANCE);
            Assert.AreEqual(0f, tracker.Velocity.x, TOLERANCE);
        }
    }
}
