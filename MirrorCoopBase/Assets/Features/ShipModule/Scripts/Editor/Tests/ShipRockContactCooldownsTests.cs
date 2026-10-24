using Features.FlightFieldModule.Contracts;
using Features.ShipModule.Scripts.Impacts;
using NUnit.Framework;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipRockContactCooldownsTests {
        private const float COOLDOWN_SECONDS = 1f;
        private const float STEP_SECONDS = 0.02f;
        private const float LONG_CONTACT_SECONDS = 3f;

        private static readonly FieldRockId _rock = FieldRockId.CreateCluster(4, 0);
        private static readonly FieldRockId _otherRock = FieldRockId.CreateCluster(5, 0);

        private ShipRockContactCooldowns _cooldowns;

        [SetUp]
        public void SetUp() =>
            _cooldowns = new ShipRockContactCooldowns();

        [Test]
        public void WhenTheShipFirstTouchesARock_ThenItIsAHit() =>
            Assert.IsTrue(_cooldowns.RecordContact(_rock, 0f, COOLDOWN_SECONDS));

        [Test]
        public void WhenTheContactLastsLongerThanTheCooldown_ThenTheRockDoesNotHitAgain() {
            _cooldowns.RecordContact(_rock, 0f, COOLDOWN_SECONDS);

            for (float time = STEP_SECONDS; time < LONG_CONTACT_SECONDS; time += STEP_SECONDS)
                Assert.IsFalse(_cooldowns.RecordContact(_rock, time, COOLDOWN_SECONDS), $"The same contact hit again at {time:0.00} s.");
        }

        [Test]
        public void WhenTheContactEndedWithinTheCooldown_ThenTheRockDoesNotHitAgain() {
            _cooldowns.RecordContact(_rock, 0f, COOLDOWN_SECONDS);
            _cooldowns.ForgetCooledDown(COOLDOWN_SECONDS * 0.5f, COOLDOWN_SECONDS);

            Assert.IsFalse(_cooldowns.RecordContact(_rock, COOLDOWN_SECONDS * 0.5f, COOLDOWN_SECONDS));
        }

        [Test]
        public void WhenTheContactEndedLongerThanTheCooldownAgo_ThenTheRockHitsAgain() {
            _cooldowns.RecordContact(_rock, 0f, COOLDOWN_SECONDS);
            float later = COOLDOWN_SECONDS + STEP_SECONDS;
            _cooldowns.ForgetCooledDown(later, COOLDOWN_SECONDS);

            Assert.AreEqual(0, _cooldowns.Count, "A cooled down rock is still remembered.");
            Assert.IsTrue(_cooldowns.RecordContact(_rock, later, COOLDOWN_SECONDS));
        }

        [Test]
        public void WhenAnotherRockIsTouched_ThenItHitsDespiteTheFirstOnesCooldown() {
            _cooldowns.RecordContact(_rock, 0f, COOLDOWN_SECONDS);

            Assert.IsTrue(_cooldowns.RecordContact(_otherRock, STEP_SECONDS, COOLDOWN_SECONDS));
        }
    }
}
