using NUnit.Framework;

namespace Features.PlayerLifeModule.Scripts.Editor.Tests {
    public sealed class CrewLobbyReturnScheduleTests {
        private const float NOW = 10f;
        private const float DELAY = 3f;
        private const float RETURN_AT = NOW + DELAY;
        private const float LATER = RETURN_AT + 1f;

        private CrewLobbyReturnSchedule _schedule;

        [SetUp]
        public void SetUp() =>
            _schedule = new CrewLobbyReturnSchedule();

        [Test]
        public void WhenShipLost_ThenReturnFiresOnceAfterTheDelay() {
            _schedule.RequestShipLost(RETURN_AT);

            Assert.That(_schedule.TryConsume(NOW), Is.False);
            Assert.That(_schedule.TryConsume(RETURN_AT), Is.True);
            Assert.That(_schedule.TryConsume(LATER), Is.False);
        }

        [Test]
        public void WhenShipLostTwice_ThenReturnFiresOnce() {
            _schedule.RequestShipLost(RETURN_AT);
            _schedule.RequestShipLost(LATER);

            Assert.That(_schedule.TryConsume(RETURN_AT), Is.True);
            Assert.That(_schedule.TryConsume(LATER), Is.False);
        }

        [Test]
        public void WhenShipLost_AndCrewIsNotAllDead_ThenReturnStillFires() {
            _schedule.RequestShipLost(RETURN_AT);

            _schedule.SetAllDead(false, LATER);

            Assert.That(_schedule.TryConsume(RETURN_AT), Is.True);
        }

        [Test]
        public void WhenShipLost_AndThenAllDead_ThenReturnFiresOnceAtTheFirstTime() {
            _schedule.RequestShipLost(RETURN_AT);
            _schedule.SetAllDead(true, LATER);

            Assert.That(_schedule.TryConsume(RETURN_AT), Is.True);
            Assert.That(_schedule.TryConsume(LATER), Is.False);
        }

        [Test]
        public void WhenAllDead_AndOneRevives_ThenNoReturn() {
            _schedule.SetAllDead(true, RETURN_AT);

            _schedule.SetAllDead(false, LATER);

            Assert.That(_schedule.TryConsume(LATER), Is.False);
        }

        [Test]
        public void WhenShipLost_AndGameplayCleanedUp_ThenNoReturn() {
            _schedule.RequestShipLost(RETURN_AT);

            _schedule.Cancel();

            Assert.That(_schedule.TryConsume(LATER), Is.False);
        }
    }
}
