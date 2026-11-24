using NUnit.Framework;
using UnityEngine;

namespace Features.MovingPlatformModule.Editor.Tests {
    public sealed class CirclePathTests {
        private const float TOLERANCE = 0.0001f;
        private const float ANGULAR_SPEED = 0.5f;

        [Test]
        public void WhenRunning_ThenTheAngleGrowsWithTimeFromTheLastChange() =>
            Assert.AreEqual(1.2f, CirclePath.AngleAt(true, 0.2f, 10d, ANGULAR_SPEED, 12d), TOLERANCE);

        [Test]
        public void WhenStopped_ThenTheAngleStays() =>
            Assert.AreEqual(0.2f, CirclePath.AngleAt(false, 0.2f, 10d, ANGULAR_SPEED, 500d), TOLERANCE);

        [Test]
        public void WhenALapPasses_ThenTheAngleWrapsIntoOneTurn() {
            float angle = CirclePath.AngleAt(true, 0f, 0d, ANGULAR_SPEED, (Mathf.PI * 2f + 1f) / ANGULAR_SPEED);

            Assert.AreEqual(1f, angle, 0.001f);
        }

        [Test]
        public void WhenRunForHours_ThenTheAngleStaysPrecise() {
            double hours = 3d * 3600d;
            float angle = CirclePath.AngleAt(true, 0f, 0d, ANGULAR_SPEED, hours);

            Assert.GreaterOrEqual(angle, 0f);
            Assert.Less(angle, Mathf.PI * 2f);
            Assert.AreEqual((float)(ANGULAR_SPEED * hours % (System.Math.PI * 2d)), angle, 0.0001f);
        }

        [Test]
        public void WhenTheAngleIsZero_ThenThePlatformIsAtTheRadiusOnX() {
            Vector3 position = CirclePath.PositionAt(new Vector3(1f, 2f, 3f), 10f, 0f);

            Assert.AreEqual(0f, Vector3.Distance(new Vector3(11f, 2f, 3f), position), TOLERANCE);
        }

        [Test]
        public void WhenOnTheCircle_ThenThePlatformFacesAlongItsWay() {
            Vector3 forward = CirclePath.RotationAt(0f) * Vector3.forward;

            Assert.AreEqual(0f, Vector3.Distance(Vector3.forward, forward), TOLERANCE);
        }
    }
}
