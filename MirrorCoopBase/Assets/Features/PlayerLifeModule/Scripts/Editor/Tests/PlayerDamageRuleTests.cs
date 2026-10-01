using NUnit.Framework;

namespace Features.PlayerLifeModule.Scripts.Editor.Tests {
    public sealed class PlayerDamageRuleTests {
        private readonly PlayerDamageRule _rule = new PlayerDamageRule();

        [Test]
        public void WhenDamaged_ThenHealthDropsByAmount() {
            bool applied = _rule.TryApply(100f, 40f, out float nextHealth, out float appliedAmount);

            Assert.That(applied, Is.True);
            Assert.That(nextHealth, Is.EqualTo(60f));
            Assert.That(appliedAmount, Is.EqualTo(40f));
            Assert.That(_rule.IsDead(nextHealth), Is.False);
        }

        [Test]
        public void WhenDamageExceedsHealth_ThenHealthClampsAtZero() {
            _rule.TryApply(100f, 250f, out float nextHealth, out _);

            Assert.That(nextHealth, Is.EqualTo(0f));
            Assert.That(_rule.IsDead(nextHealth), Is.True);
        }

        [Test]
        public void WhenDamagedAfterDeath_ThenNothingIsApplied() {
            bool applied = _rule.TryApply(0f, 10f, out float nextHealth, out float appliedAmount);

            Assert.That(applied, Is.False);
            Assert.That(nextHealth, Is.EqualTo(0f));
            Assert.That(appliedAmount, Is.EqualTo(0f));
        }

        [Test]
        public void WhenDamageIsNegative_ThenHealthIsUnchanged() {
            _rule.TryApply(100f, -5f, out float nextHealth, out float appliedAmount);

            Assert.That(nextHealth, Is.EqualTo(100f));
            Assert.That(appliedAmount, Is.EqualTo(0f));
        }

        [Test]
        public void WhenKilledWithLethalAmount_ThenDead() {
            const float health = 37f;

            _rule.TryApply(health, health, out float nextHealth, out _);

            Assert.That(_rule.IsDead(nextHealth), Is.True);
        }
    }
}
