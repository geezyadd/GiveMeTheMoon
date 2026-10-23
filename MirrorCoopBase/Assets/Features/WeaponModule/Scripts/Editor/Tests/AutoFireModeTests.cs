using Features.WeaponModule.Scripts.Parts.FireModes;
using NUnit.Framework;

namespace Features.WeaponModule.Editor.Tests {
    public sealed class AutoFireModeTests {
        private const float SPIN_UP_SECONDS = 0.25f;
        private const float SPIN_DOWN_SECONDS = 0.4f;
        private const float SHOTS_PER_SECOND = 20f;
        private const float FRAME_SECONDS = 1f / 60f;
        private const float READINESS_TOLERANCE = 0.02f;

        private AutoFireMode _fireMode;

        [SetUp]
        public void SetUp() =>
            _fireMode = new AutoFireMode(SPIN_UP_SECONDS, SPIN_DOWN_SECONDS, SHOTS_PER_SECOND);

        [Test]
        public void WhenTriggerHeldShorterThanSpinUp_ThenNoShotIsBorn() {
            int shots = Hold(_fireMode, SPIN_UP_SECONDS * 0.9f, FRAME_SECONDS);

            Assert.AreEqual(0, shots);
            Assert.IsFalse(_fireMode.IsFiring);
            Assert.AreEqual(0.9f, _fireMode.Readiness, READINESS_TOLERANCE);
        }

        [Test]
        public void WhenBarrelsReachFullSpin_ThenTheFirstShotComesAtOnce() {
            int shots = Hold(_fireMode, SPIN_UP_SECONDS + FRAME_SECONDS, FRAME_SECONDS);

            Assert.AreEqual(1, shots);
            Assert.IsTrue(_fireMode.IsFiring);
        }

        [Test]
        public void WhenTriggerHeldOneSecondAfterSpinUp_ThenShotsMatchTheFireRate() {
            Hold(_fireMode, SPIN_UP_SECONDS, FRAME_SECONDS);

            int shots = Hold(_fireMode, 1f, FRAME_SECONDS);

            Assert.That(shots, Is.InRange(SHOTS_PER_SECOND, SHOTS_PER_SECOND + 1f));
        }

        [TestCase(1f / 30f)]
        [TestCase(1f / 144f)]
        [TestCase(0.2f)]
        public void WhenFrameRateChanges_ThenCadenceStaysTheSame(float frameSeconds) {
            int shots = Hold(new AutoFireMode(SPIN_UP_SECONDS, SPIN_DOWN_SECONDS, SHOTS_PER_SECOND), SPIN_UP_SECONDS + 2f, frameSeconds);
            int reference = Hold(new AutoFireMode(SPIN_UP_SECONDS, SPIN_DOWN_SECONDS, SHOTS_PER_SECOND), SPIN_UP_SECONDS + 2f, FRAME_SECONDS);

            Assert.That(shots, Is.InRange(reference - 1, reference + 1));
        }

        [Test]
        public void WhenTriggerReleased_ThenBarrelsSpinDownOverSpinDownTime() {
            Hold(_fireMode, SPIN_UP_SECONDS + 0.5f, FRAME_SECONDS);

            int shotsWhileHalfDown = Release(_fireMode, SPIN_DOWN_SECONDS / 2f, FRAME_SECONDS);
            float halfDownReadiness = _fireMode.Readiness;
            Release(_fireMode, SPIN_DOWN_SECONDS / 2f + FRAME_SECONDS, FRAME_SECONDS);

            Assert.AreEqual(0, shotsWhileHalfDown);
            Assert.IsFalse(_fireMode.IsFiring);
            Assert.AreEqual(0.5f, halfDownReadiness, READINESS_TOLERANCE);
            Assert.AreEqual(0f, _fireMode.Readiness);
        }

        [Test]
        public void WhenTriggerPressedAgainDuringSpinDown_ThenFiringResumesSoonerThanFromStill() {
            Hold(_fireMode, SPIN_UP_SECONDS + 0.5f, FRAME_SECONDS);
            Release(_fireMode, SPIN_DOWN_SECONDS / 4f, FRAME_SECONDS);

            int shots = Hold(_fireMode, SPIN_UP_SECONDS / 2f, FRAME_SECONDS);

            Assert.That(shots, Is.GreaterThan(0));
        }

        [Test]
        public void WhenSpinUpIsZero_ThenTheFirstFrameFires() {
            AutoFireMode instantFireMode = new AutoFireMode(0f, 0f, SHOTS_PER_SECOND);

            Assert.AreEqual(1, instantFireMode.Advance(true, FRAME_SECONDS));
        }

        private static int Hold(AutoFireMode fireMode, float seconds, float frameSeconds) =>
            Advance(fireMode, isTriggerHeld: true, seconds, frameSeconds);

        private static int Release(AutoFireMode fireMode, float seconds, float frameSeconds) =>
            Advance(fireMode, isTriggerHeld: false, seconds, frameSeconds);

        // The last frame is cut so the total time is exact.
        private static int Advance(AutoFireMode fireMode, bool isTriggerHeld, float seconds, float frameSeconds) {
            int shots = 0;
            for (float left = seconds; left > 0f; left -= frameSeconds)
                shots += fireMode.Advance(isTriggerHeld, left < frameSeconds ? left : frameSeconds);
            return shots;
        }
    }
}
