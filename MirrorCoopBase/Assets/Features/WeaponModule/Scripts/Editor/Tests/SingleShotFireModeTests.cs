using Features.WeaponModule.Scripts.Parts.FireModes;
using NUnit.Framework;

namespace Features.WeaponModule.Editor.Tests {
    public sealed class SingleShotFireModeTests {
        private const float COOLDOWN_SECONDS = 2f;
        private const float FRAME_SECONDS = 0.125f;
        private const float READINESS_TOLERANCE = 0.001f;

        private SingleShotFireMode _fireMode;

        [SetUp]
        public void SetUp() =>
            _fireMode = new SingleShotFireMode(COOLDOWN_SECONDS);

        [Test]
        public void WhenTriggerIsPressedWhileReady_ThenOneShotIsBornAtOnce() {
            int shots = _fireMode.Advance(true, FRAME_SECONDS);

            Assert.AreEqual(1, shots);
            Assert.IsTrue(_fireMode.IsFiring);
            Assert.AreEqual(0f, _fireMode.Readiness, READINESS_TOLERANCE);
        }

        [Test]
        public void WhenTriggerIsHeldPastTheCooldown_ThenNoSecondShotIsBorn() {
            _fireMode.Advance(true, FRAME_SECONDS);

            int shots = Advance(true, COOLDOWN_SECONDS * 2f);

            Assert.AreEqual(0, shots);
            Assert.AreEqual(1f, _fireMode.Readiness, READINESS_TOLERANCE);
        }

        [Test]
        public void WhenTriggerIsPressedAgainWithinTheCooldown_ThenThePressIsBlocked() {
            _fireMode.Advance(true, FRAME_SECONDS);
            Advance(false, COOLDOWN_SECONDS - 2f * FRAME_SECONDS);

            int shots = _fireMode.Advance(true, FRAME_SECONDS);

            Assert.AreEqual(0, shots);
            Assert.IsFalse(_fireMode.IsFiring);
            Assert.Less(_fireMode.Readiness, 1f);
        }

        [Test]
        public void WhenTriggerIsPressedAgainAfterTheCooldown_ThenTheNextShotIsBorn() {
            _fireMode.Advance(true, FRAME_SECONDS);
            Advance(false, COOLDOWN_SECONDS - FRAME_SECONDS);

            int shots = _fireMode.Advance(true, FRAME_SECONDS);

            Assert.AreEqual(1, shots);
        }

        [Test]
        public void WhenTheCooldownRuns_ThenReadinessClimbsWithTheTime() {
            _fireMode.Advance(true, FRAME_SECONDS);

            Advance(false, COOLDOWN_SECONDS * 0.5f);

            Assert.AreEqual(0.5f, _fireMode.Readiness, READINESS_TOLERANCE);
            Assert.IsFalse(_fireMode.IsFiring);
        }

        [Test]
        public void WhenCreatedFromTheDefinition_ThenTheCooldownIsOneShotInterval() {
            IWeaponFireMode fireMode = new SingleShotFireModeDefinition().CreateFireMode(1f / COOLDOWN_SECONDS);
            fireMode.Advance(true, FRAME_SECONDS);
            fireMode.Advance(false, COOLDOWN_SECONDS - 2f * FRAME_SECONDS);

            Assert.AreEqual(0, fireMode.Advance(true, FRAME_SECONDS), "A press within the cooldown fired.");
            fireMode.Advance(false, FRAME_SECONDS);
            Assert.AreEqual(1, fireMode.Advance(true, FRAME_SECONDS), "A press after the cooldown did not fire.");
        }

        private int Advance(bool isTriggerHeld, float seconds) {
            int shots = 0;
            for (float elapsed = 0f; elapsed < seconds - READINESS_TOLERANCE; elapsed += FRAME_SECONDS)
                shots += _fireMode.Advance(isTriggerHeld, FRAME_SECONDS);

            return shots;
        }
    }
}
