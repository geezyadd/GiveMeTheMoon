using NUnit.Framework;

namespace Features.PlayerLifeModule.Scripts.Editor.Tests {
    public sealed class PlayerLifeStateMachineTests {
        [Test]
        public void WhenStarted_ThenAlive() {
            FakePlayer player = new FakePlayer();
            PlayerLifeStateMachine machine = Start(player);

            Assert.That(machine.State, Is.EqualTo(PlayerLifeState.Alive));
            Assert.That(player.LifeState, Is.EqualTo(PlayerLifeState.Alive));
        }

        [Test]
        public void WhenDied_ThenDeadAndDeathApplied() {
            FakePlayer player = new FakePlayer();
            PlayerLifeStateMachine machine = Start(player);

            player.ServerKill(DamageType.Fall);

            Assert.That(machine.State, Is.EqualTo(PlayerLifeState.Dead));
            Assert.That(player.LifeState, Is.EqualTo(PlayerLifeState.Dead));
            Assert.That(player.DeathsApplied, Is.EqualTo(1));
        }

        [Test]
        public void WhenDiedByGenericDamage_ThenDead() {
            FakePlayer player = new FakePlayer();
            PlayerLifeStateMachine machine = Start(player);

            player.ServerApplyDamage(new DamageInfo(500f, DamageType.Generic));

            Assert.That(machine.State, Is.EqualTo(PlayerLifeState.Dead));
        }

        [Test]
        public void WhenDamagedButNotLethal_ThenStaysAlive() {
            FakePlayer player = new FakePlayer();
            PlayerLifeStateMachine machine = Start(player);

            player.ServerApplyDamage(new DamageInfo(30f, DamageType.Generic));

            Assert.That(machine.State, Is.EqualTo(PlayerLifeState.Alive));
            Assert.That(player.DeathsApplied, Is.EqualTo(0));
        }

        [Test]
        public void WhenKilledTwice_ThenTransitionsOnce() {
            FakePlayer player = new FakePlayer();
            PlayerLifeStateMachine machine = Start(player);
            int changes = 0;
            machine.OnStateChanged += () => changes++;

            player.ServerKill(DamageType.Fall);
            player.ServerKill(DamageType.Fall);

            Assert.That(changes, Is.EqualTo(1));
            Assert.That(player.DeathsApplied, Is.EqualTo(1));
        }

        [Test]
        public void WhenRevived_ThenAliveWithFullHealth() {
            FakePlayer player = new FakePlayer();
            PlayerLifeStateMachine machine = Start(player);
            player.ServerKill(DamageType.Fall);

            machine.Revive();

            Assert.That(machine.State, Is.EqualTo(PlayerLifeState.Alive));
            Assert.That(player.LifeState, Is.EqualTo(PlayerLifeState.Alive));
            Assert.That(player.Health, Is.EqualTo(player.MaxHealth));
        }

        [Test]
        public void WhenRevivedAndKilledAgain_ThenDeadAgain() {
            FakePlayer player = new FakePlayer();
            PlayerLifeStateMachine machine = Start(player);
            player.ServerKill(DamageType.Fall);
            machine.Revive();

            player.ServerKill(DamageType.Fall);

            Assert.That(machine.State, Is.EqualTo(PlayerLifeState.Dead));
            Assert.That(player.DeathsApplied, Is.EqualTo(2));
        }

        [Test]
        public void WhenAliveIsRevived_ThenHealthIsRestored() {
            FakePlayer player = new FakePlayer(PlayerLifeState.Alive, 40f);
            PlayerLifeStateMachine machine = Start(player);

            machine.Revive();

            Assert.That(machine.State, Is.EqualTo(PlayerLifeState.Alive));
            Assert.That(player.Health, Is.EqualTo(player.MaxHealth));
        }

        [Test]
        public void WhenStartedFromStoredDeadState_ThenStaysDead() {
            FakePlayer player = new FakePlayer(PlayerLifeState.Dead, 0f);

            PlayerLifeStateMachine machine = Start(player);

            Assert.That(machine.State, Is.EqualTo(PlayerLifeState.Dead));
            Assert.That(player.Health, Is.EqualTo(0f));
            Assert.That(player.Restores, Is.EqualTo(0));
        }

        [Test]
        public void WhenStopped_ThenDeathIsIgnoredAndStateKept() {
            FakePlayer player = new FakePlayer();
            PlayerLifeStateMachine machine = Start(player);

            machine.Stop();
            player.ServerKill(DamageType.Fall);

            Assert.That(machine.State, Is.EqualTo(PlayerLifeState.Alive));
            Assert.That(player.DeathsApplied, Is.EqualTo(0));
        }

        private static PlayerLifeStateMachine Start(FakePlayer player) {
            PlayerLifeStateMachine machine = new PlayerLifeStateMachine(player, player);
            machine.Start();
            return machine;
        }
    }
}
