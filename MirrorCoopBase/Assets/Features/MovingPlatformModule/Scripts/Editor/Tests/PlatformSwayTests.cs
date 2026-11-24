using NUnit.Framework;
using UnityEngine;

namespace Features.MovingPlatformModule.Editor.Tests {
    public sealed class PlatformSwayTests {
        private const float ROLL = 6f;
        private const float PITCH = 3f;
        private const float PERIOD = 4f;
        private const float TOLERANCE = 0.01f;

        [Test]
        public void WhenAQuarterPeriodPasses_ThenTheRollIsFull() {
            Quaternion tilt = PlatformSway.TiltAt(1d, ROLL, PITCH, PERIOD);

            Assert.AreEqual(ROLL, Mathf.DeltaAngle(0f, tilt.eulerAngles.z), TOLERANCE);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, tilt.eulerAngles.x), TOLERANCE);
        }

        [Test]
        public void WhenThePeriodStarts_ThenOnlyThePitchLeans() {
            Quaternion tilt = PlatformSway.TiltAt(8d, ROLL, PITCH, PERIOD);

            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, tilt.eulerAngles.z), TOLERANCE);
            Assert.AreEqual(PITCH, Mathf.DeltaAngle(0f, tilt.eulerAngles.x), TOLERANCE);
        }

        [Test]
        public void WhenTheSwayIsOn_ThenTheHeadingDoesNotChange() {
            Quaternion heading = Quaternion.Euler(0f, 70f, 0f);
            Quaternion rocked = heading * PlatformSway.TiltAt(1.3d, ROLL, PITCH, PERIOD);

            Assert.AreEqual(70f, rocked.eulerAngles.y, TOLERANCE);
        }

        [Test]
        public void WhenThePeriodIsZero_ThenThereIsNoTilt() =>
            Assert.AreEqual(Quaternion.identity, PlatformSway.TiltAt(3d, ROLL, PITCH, 0f));
    }
}
