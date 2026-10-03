using NUnit.Framework;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class HelmSteerTests {
        private const uint PILOT = 7;
        private const uint OTHER_PILOT = 9;
        private const float STEER = 0.7f;

        private HelmSteer _steer;

        [SetUp]
        public void SetUp() =>
            _steer = new HelmSteer();

        [Test]
        public void WhenPilotSteers_ThenHelmReadsItsSteer() {
            _steer.Set(PILOT, STEER);

            Assert.AreEqual(STEER, _steer.Read(PILOT));
        }

        [Test]
        public void WhenPilotLeavesTheHelm_ThenSteerIsZero() {
            _steer.Set(PILOT, STEER);

            Assert.AreEqual(0f, _steer.Read(0));
        }

        [Test]
        public void WhenAnotherPilotTakesTheHelm_ThenPreviousSteerIsNotApplied() {
            _steer.Set(PILOT, STEER);

            Assert.AreEqual(0f, _steer.Read(OTHER_PILOT));
        }

        [Test]
        public void WhenCleared_AndSamePilotSitsAgain_ThenSteerIsZero() {
            _steer.Set(PILOT, STEER);
            _steer.Clear();

            Assert.AreEqual(0f, _steer.Read(PILOT));
        }
    }
}
