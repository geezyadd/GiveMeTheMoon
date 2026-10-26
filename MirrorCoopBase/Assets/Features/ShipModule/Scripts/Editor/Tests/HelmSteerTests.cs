using Features.NetworkModelModule.Scripts;
using Features.ShipModule.Scripts.Helm;
using NUnit.Framework;

namespace Features.ShipModule.Editor.Tests {
    // The helm station's steer: the host pilot's own stick, a client pilot's sent steer and its send cadence, and no
    // steer left over from a pilot who has gone.
    public sealed class HelmSteerTests {
        private const float STICK = -1f;
        private const float SENT_STEER = 0.7f;
        private const float SEND_SECONDS = 0.03f;
        private const double NOW = 10d;

        private static readonly PlayerKey _pilot = new PlayerKey("client:pilot");
        private static readonly PlayerKey _otherPilot = new PlayerKey("client:other");

        private HelmSteer _steer;

        [SetUp]
        public void SetUp() =>
            _steer = new HelmSteer();

        [Test]
        public void WhenNobodySits_ThenSteerIsZero() =>
            Assert.AreEqual(0f, _steer.Read(STICK));

        [Test]
        public void WhenHostPilotHoldsTheStick_ThenHelmReadsTheStick() {
            SitDown(_pilot, true);

            Assert.AreEqual(STICK, _steer.Read(STICK));
        }

        [Test]
        public void WhenRemotePilotSendsSteer_ThenHelmReadsTheSentSteerNotTheHostStick() {
            SitDown(_pilot, false);

            _steer.ServerReceive(SENT_STEER);

            Assert.AreEqual(SENT_STEER, _steer.Read(STICK));
        }

        [Test]
        public void WhenRemotePilotStandsUp_AndAnotherPilotSits_ThenThePreviousSteerIsNotApplied() {
            SitDown(_pilot, false);
            _steer.ServerReceive(SENT_STEER);

            _steer.Exit();
            SitDown(_otherPilot, false);

            Assert.AreEqual(0f, _steer.Read(STICK));
        }

        [Test]
        public void WhenPilotsObjectIsNotOnThisPeerYet_ThenItIsNotLocal_AndSendsNothing() {
            _steer.Enter(_pilot);

            Assert.IsTrue(_steer.HasPilot);
            Assert.IsFalse(_steer.IsOccupantLocal);
            Assert.IsFalse(_steer.TryTakeSendTurn(false, NOW, SEND_SECONDS));
        }

        [Test]
        public void WhenClientPilotSits_ThenItSendsAtOnce_AndThenOncePerInterval() {
            SitDown(_pilot, true);

            Assert.IsTrue(_steer.TryTakeSendTurn(false, NOW, SEND_SECONDS));
            Assert.IsFalse(_steer.TryTakeSendTurn(false, NOW + SEND_SECONDS / 2d, SEND_SECONDS));
            Assert.IsTrue(_steer.TryTakeSendTurn(false, NOW + SEND_SECONDS, SEND_SECONDS));
        }

        [Test]
        public void WhenHostPilotSits_ThenItSendsNothing() {
            SitDown(_pilot, true);

            Assert.IsFalse(_steer.TryTakeSendTurn(true, NOW, SEND_SECONDS));
        }

        [Test]
        public void WhenRemotePilotSits_ThenThisPeerSendsNothingForIt() {
            SitDown(_pilot, false);

            Assert.IsFalse(_steer.TryTakeSendTurn(false, NOW, SEND_SECONDS));
        }

        private void SitDown(PlayerKey pilot, bool isLocal) {
            _steer.Enter(pilot);
            _steer.TakeOccupant(isLocal);
        }
    }
}
