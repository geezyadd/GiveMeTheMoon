using NUnit.Framework;

namespace Features.PlayerLifeModule.Scripts.Editor.Tests {
    public sealed class PlayerHealthTests {
        [Test]
        public void WhenDamaged_ThenHealthDropsByAmount() {
            PlayerHealth health = new PlayerHealth(100f);
            DamageInfo applied = default;
            health.OnDamaged += info => applied = info;

            bool appliedDamage = health.TryApply(new DamageInfo(40f, DamageType.Generic));

            Assert.That(appliedDamage, Is.True);
            Assert.That(health.Health, Is.EqualTo(60f));
            Assert.That(health.IsDead, Is.False);
            Assert.That(applied.Amount, Is.EqualTo(40f));
            Assert.That(applied.Type, Is.EqualTo(DamageType.Generic));
        }

        [Test]
        public void WhenDamageExceedsHealth_ThenHealthClampsAtZero() {
            PlayerHealth health = new PlayerHealth(100f);

            health.TryApply(new DamageInfo(250f, DamageType.Generic));

            Assert.That(health.Health, Is.EqualTo(0f));
            Assert.That(health.IsDead, Is.True);
        }

        [Test]
        public void WhenDamagedAgainAfterDeath_ThenDiesOnce() {
            PlayerHealth health = new PlayerHealth(100f);
            int died = 0;
            int damaged = 0;
            health.OnDied += _ => died++;
            health.OnDamaged += _ => damaged++;

            bool first = health.TryApply(new DamageInfo(100f, DamageType.Generic));
            bool second = health.TryApply(new DamageInfo(10f, DamageType.Generic));

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(health.Health, Is.EqualTo(0f));
            Assert.That(died, Is.EqualTo(1));
            Assert.That(damaged, Is.EqualTo(1));
        }

        [Test]
        public void WhenKilledTwice_ThenDiesOnce() {
            PlayerHealth health = new PlayerHealth(100f);
            int died = 0;
            health.OnDied += _ => died++;

            bool first = health.TryKill(DamageType.Fall);
            bool second = health.TryKill(DamageType.Fall);

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(health.Health, Is.EqualTo(0f));
            Assert.That(health.IsDead, Is.True);
            Assert.That(died, Is.EqualTo(1));
        }

        [Test]
        public void WhenRestored_ThenFullHealthAndAlive() {
            PlayerHealth health = new PlayerHealth(100f);
            health.TryApply(new DamageInfo(80f, DamageType.Generic));
            health.TryKill(DamageType.Fall);

            health.Restore();

            Assert.That(health.Health, Is.EqualTo(100f));
            Assert.That(health.MaxHealth, Is.EqualTo(100f));
            Assert.That(health.IsDead, Is.False);

            int died = 0;
            health.OnDied += _ => died++;
            health.TryKill(DamageType.Fall);

            Assert.That(died, Is.EqualTo(1));
            Assert.That(health.IsDead, Is.True);
        }
    }
}
