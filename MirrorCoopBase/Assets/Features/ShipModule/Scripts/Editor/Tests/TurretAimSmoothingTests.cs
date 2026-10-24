using Features.ShipModule.Scripts.Turret;
using NUnit.Framework;

namespace Features.ShipModule.Editor.Tests {
    // Other players' view of a turret eases towards the throttled aim it receives.
    public sealed class TurretAimSmoothingTests {
        private const float SMOOTHING_SECONDS = 0.08f;
        private const float FRAME_SECONDS = 1f / 60f;
        private const float TOLERANCE = 0.01f;

        [Test]
        public void WhenFirstAimArrives_ThenTurretShowsItAtOnce() {
            TurretAimSmoothing smoothing = new TurretAimSmoothing(SMOOTHING_SECONDS);

            smoothing.Advance(30f, 10f, FRAME_SECONDS);

            Assert.AreEqual(30f, smoothing.Yaw, TOLERANCE);
            Assert.AreEqual(10f, smoothing.Pitch, TOLERANCE);
        }

        [Test]
        public void WhenNewAimArrives_ThenTurretMovesPartWayInAFrameAndArrivesSoon() {
            TurretAimSmoothing smoothing = new TurretAimSmoothing(SMOOTHING_SECONDS);
            smoothing.Advance(0f, 0f, FRAME_SECONDS);

            smoothing.Advance(40f, 20f, FRAME_SECONDS);
            Assert.Greater(smoothing.Yaw, 0f);
            Assert.Less(smoothing.Yaw, 40f);

            for (int frame = 0; frame < 60; frame++)
                smoothing.Advance(40f, 20f, FRAME_SECONDS);

            Assert.AreEqual(40f, smoothing.Yaw, TOLERANCE);
            Assert.AreEqual(20f, smoothing.Pitch, TOLERANCE);
        }

        [Test]
        public void WhenAimCrossesTheBackOfTheTurn_ThenTurretTakesTheShortWay() {
            TurretAimSmoothing smoothing = new TurretAimSmoothing(SMOOTHING_SECONDS);
            smoothing.Advance(170f, 0f, FRAME_SECONDS);

            smoothing.Advance(-170f, 0f, FRAME_SECONDS);

            Assert.Greater(smoothing.Yaw, 170f);
        }
    }
}
