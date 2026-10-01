using System.Collections.Generic;
using NUnit.Framework;

namespace Features.PlayerLifeModule.Scripts.Editor.Tests {
    public sealed class AllDeadRuleTests {
        private readonly AllDeadRule _rule = new AllDeadRule();

        [Test]
        public void WhenNoPlayers_ThenNotAllDead() {
            Assert.That(_rule.AreAllDead(new List<PlayerLifeStateMachine>()), Is.False);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void WhenEveryoneAlive_ThenNotAllDead(int count) {
            List<PlayerLifeStateMachine> players = StartPlayers(count, out _);

            Assert.That(_rule.AreAllDead(players), Is.False);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void WhenEveryoneDied_ThenAllDead(int count) {
            List<PlayerLifeStateMachine> players = StartPlayers(count, out List<FakePlayer> fakes);

            for (int i = 0; i < fakes.Count; i++)
                fakes[i].ServerKill(DamageType.Fall);

            Assert.That(_rule.AreAllDead(players), Is.True);
        }

        [TestCase(2)]
        [TestCase(3)]
        public void WhenAllButOneDied_ThenNotAllDead(int count) {
            List<PlayerLifeStateMachine> players = StartPlayers(count, out List<FakePlayer> fakes);

            for (int i = 1; i < fakes.Count; i++)
                fakes[i].ServerKill(DamageType.Fall);

            Assert.That(_rule.AreAllDead(players), Is.False);
        }

        [Test]
        public void WhenTheLastAliveOfThreeLeaves_ThenAllDead() {
            List<PlayerLifeStateMachine> players = StartPlayers(3, out List<FakePlayer> fakes);
            fakes[1].ServerKill(DamageType.Fall);
            fakes[2].ServerKill(DamageType.Fall);

            players.RemoveAt(0);

            Assert.That(_rule.AreAllDead(players), Is.True);
        }

        [Test]
        public void WhenAllDeadAndOneRevived_ThenNotAllDead() {
            List<PlayerLifeStateMachine> players = StartPlayers(2, out List<FakePlayer> fakes);
            fakes[0].ServerKill(DamageType.Fall);
            fakes[1].ServerKill(DamageType.Fall);

            players[0].Revive();

            Assert.That(_rule.AreAllDead(players), Is.False);
        }

        private static List<PlayerLifeStateMachine> StartPlayers(int count, out List<FakePlayer> fakes) {
            List<PlayerLifeStateMachine> players = new List<PlayerLifeStateMachine>(count);
            fakes = new List<FakePlayer>(count);
            for (int i = 0; i < count; i++) {
                FakePlayer fake = new FakePlayer();
                PlayerLifeStateMachine machine = new PlayerLifeStateMachine(fake, fake);
                machine.Start();
                fakes.Add(fake);
                players.Add(machine);
            }

            return players;
        }
    }
}
