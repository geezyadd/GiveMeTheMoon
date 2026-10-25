using Features.GrabModule.Scripts.Held;
using Features.ShipModule.Scripts.Throttle;
using NUnit.Framework;

namespace Features.ShipModule.Scripts.Editor.Tests {
    // The holder's peer: it sends the handle at most once per send interval and the last position on release, and
    // it lets go when the server gives the handle to someone else or never confirms the take.
    public sealed class ShipThrottleLocalHoldTests {
        private const string HOLDER = "client:holder";
        private const string OTHER = "client:other";
        private const float STEP_SECONDS = 1f / 60f;
        private const float PUSH = 5f;
        private const float HOLD_SECONDS = 1f;
        // Send interval 0.05 s over 1 s of pushing: about 20 sends, never one a frame (60).
        private const int MAX_SENDS_PER_SECOND = 21;

        private ShipThrottleTestConfiguration _configuration;
        private ShipThrottleLocalHold _hold;

        [SetUp]
        public void SetUp() {
            _configuration = new ShipThrottleTestConfiguration();
            _hold = new ShipThrottleLocalHold(_configuration.Configuration, _configuration.CreateHandle());
        }

        [TearDown]
        public void TearDown() =>
            _configuration.Dispose();

        [Test]
        public void WhenTheHolderPushesForASecond_ThenItSendsAtTheSendRate_AndTheLastPositionOnRelease() {
            _hold.Begin(HOLDER, 0f);
            int sends = 0;
            for (float time = 0f; time < HOLD_SECONDS; time += STEP_SECONDS) {
                if (_hold.Drive(PUSH, STEP_SECONDS, 1f, HOLDER).Frame.HasPositionToSend)
                    sends++;
            }

            HeldControlFrame release = _hold.End(1f);
            Assert.Greater(sends, 1, "The holder did not send the handle while pushing it.");
            Assert.LessOrEqual(sends, MAX_SENDS_PER_SECOND, "The holder sent the handle more often than the send interval.");
            Assert.IsFalse(_hold.IsHolding, "The hold did not end on release.");
            Assert.AreEqual(_hold.Position, release.Position, "The release did not carry the handle's last position.");
        }

        [Test]
        public void WhenTheServerGivesTheHandleToAnother_ThenTheLocalHoldIsLost() {
            _hold.Begin(HOLDER, 0f);
            _hold.Drive(PUSH, STEP_SECONDS, 1f, HOLDER);

            Assert.IsTrue(_hold.Drive(PUSH, STEP_SECONDS, 1f, OTHER).Frame.IsLost, "The hold went on while another player held the handle.");
        }

        [Test]
        public void WhenTheServerLetsGoAfterConfirmingTheTake_ThenTheLocalHoldIsLost() {
            _hold.Begin(HOLDER, 0f);
            _hold.Drive(PUSH, STEP_SECONDS, 1f, HOLDER);

            Assert.IsTrue(_hold.Drive(PUSH, STEP_SECONDS, 1f, string.Empty).Frame.IsLost, "The hold went on after the server let the handle go.");
        }

        [Test]
        public void WhenTheServerNeverConfirmsTheTake_ThenTheLocalHoldEndsAfterTheTimeout() {
            _hold.Begin(HOLDER, 0f);

            Assert.IsFalse(_hold.Drive(PUSH, STEP_SECONDS, 1f, string.Empty).Frame.IsLost, "The hold was lost before the server could answer the take.");
            Assert.IsTrue(_hold.Drive(PUSH, ShipThrottleTestConfiguration.TAKE_TIMEOUT_SECONDS, 1f, string.Empty).Frame.IsLost, "An unconfirmed take never ended.");
        }
    }
}
