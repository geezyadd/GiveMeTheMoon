using Features.FloatingControllerModule;
using NUnit.Framework;
using UnityEngine;

namespace Features.MovingPlatformModule.Editor.Tests {
    public sealed class CarrierMotionTests {
        private const float TOLERANCE = 0.0001f;

        private static readonly Pose Before = new(new Vector3(10f, 0f, 0f), Quaternion.identity);
        private static readonly Pose Moved = new(new Vector3(12f, 0f, 1f), Quaternion.identity);
        private static readonly Pose Turned = new(new Vector3(10f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));

        [Test]
        public void WhenTheCarrierMoves_ThenAPointOnItMovesTheSame() =>
            AssertNear(new Vector3(13f, 1f, 1f), CarrierMotion.CarryPoint(Before, Moved, new Vector3(11f, 1f, 0f)));

        [Test]
        public void WhenTheCarrierTurns_ThenAPointOnItTurnsAroundItsMiddle() =>
            AssertNear(new Vector3(10f, 0f, -2f), CarrierMotion.CarryPoint(Before, Turned, new Vector3(12f, 0f, 0f)));

        [Test]
        public void WhenTheCarrierTurns_ThenTheYawIsTheTurn() =>
            Assert.AreEqual(90f, CarrierMotion.YawOf(Before, Turned), TOLERANCE);

        [Test]
        public void WhenTheCarrierTurnsBackPastZero_ThenTheYawIsShort() {
            var a = new Pose(Vector3.zero, Quaternion.Euler(0f, 359f, 0f));
            var b = new Pose(Vector3.zero, Quaternion.Euler(0f, 1f, 0f));

            Assert.AreEqual(2f, CarrierMotion.YawOf(a, b), TOLERANCE);
        }

        [Test]
        public void WhenTheCarrierMoves_ThenThePointVelocityIsTheStepOverTime() =>
            AssertNear(new Vector3(100f, 0f, 50f), CarrierMotion.PointVelocity(Before, Moved, new Vector3(10f, 0f, 0f), 0.02f));

        [Test]
        public void WhenAPointGoesToLocalAndBack_ThenItIsTheSame() {
            var pose = new Pose(new Vector3(3f, 1f, -2f), Quaternion.Euler(0f, 37f, 0f));
            var point = new Vector3(5f, 2f, 7f);

            AssertNear(point, CarrierMotion.ToWorldPoint(pose, CarrierMotion.ToLocalPoint(pose, point)));
        }

        [Test]
        public void WhenADirectionGoesToLocalAndBack_ThenItIsTheSame() {
            var pose = new Pose(Vector3.zero, Quaternion.Euler(0f, -120f, 0f));
            var direction = new Vector3(1f, 0f, 2f);

            AssertNear(direction, CarrierMotion.ToWorldDirection(pose, CarrierMotion.ToLocalDirection(pose, direction)));
        }

        [Test]
        public void WhenTheCarrierRocks_ThenItsHeadingKeepsOnlyTheTurn() {
            var pose = new Pose(Vector3.zero, Quaternion.Euler(10f, 40f, -12f));

            Quaternion heading = CarrierMotion.HeadingOf(pose);

            Assert.AreEqual(40f, heading.eulerAngles.y, 0.01f);
            AssertNear(Vector3.up, heading * Vector3.up);
        }

        private static void AssertNear(Vector3 expected, Vector3 actual) =>
            Assert.AreEqual(0f, Vector3.Distance(expected, actual), TOLERANCE, $"expected {expected}, got {actual}");
    }
}
