using Features.ShipModule.Scripts.Throttle;
using NUnit.Framework;

namespace Features.ShipModule.Scripts.Editor.Tests {
    // The throttle as the launch lever: a station that cannot launch keeps the handle at Stop, a push to the launch
    // mark takes off, and in flight the handle sets any share of the max speed without launching again.
    public sealed class ShipThrottleLaunchRuleTests {
        private const float BELOW_MARK = 0.1f;
        private const float FULL = 1f;
        private const float TOLERANCE = 0.0001f;

        private ShipThrottleTestConfiguration _configuration;
        private ShipThrottleLaunchRule _rule;

        [SetUp]
        public void SetUp() {
            _configuration = new ShipThrottleTestConfiguration();
            _rule = new ShipThrottleLaunchRule(_configuration.Configuration);
        }

        [TearDown]
        public void TearDown() =>
            _configuration.Dispose();

        [Test]
        public void WhenTheShipCannotLaunch_AndTheHandleIsPushedToFull_ThenItStaysAtStopAndDoesNotLaunch() {
            ShipThrottleMove move = _rule.Evaluate(ShipRunPhase.Build, false, FULL);

            Assert.AreEqual(0f, move.Position, TOLERANCE, "The handle went past Stop at a station that cannot launch.");
            Assert.IsFalse(move.IsLaunch, "A station that cannot launch took off.");
            Assert.AreEqual(0f, _rule.ReadMaxPosition(ShipRunPhase.Build, false), TOLERANCE, "The local handle may leave Stop at a station that cannot launch.");
        }

        [Test]
        public void WhenTheShipCanLaunch_AndTheHandleStaysBelowTheLaunchMark_ThenItMovesButDoesNotLaunch() {
            ShipThrottleMove move = _rule.Evaluate(ShipRunPhase.Build, true, BELOW_MARK);

            Assert.AreEqual(BELOW_MARK, move.Position, TOLERANCE, "The handle did not move below the launch mark.");
            Assert.IsFalse(move.IsLaunch, "A push below the launch mark took off.");
        }

        [Test]
        public void WhenTheShipCanLaunch_AndTheHandleReachesTheLaunchMark_ThenItLaunches() {
            ShipThrottleMove atMark = _rule.Evaluate(ShipRunPhase.Build, true, ShipThrottleTestConfiguration.LAUNCH_MARK);
            ShipThrottleMove atFull = _rule.Evaluate(ShipRunPhase.Build, true, FULL);

            Assert.IsTrue(atMark.IsLaunch, "A push to the launch mark did not take off.");
            Assert.IsTrue(atFull.IsLaunch, "A push to Full did not take off.");
            Assert.AreEqual(FULL, atFull.Position, TOLERANCE, "The handle did not stay where it was pushed at takeoff.");
        }

        [TestCase(ShipRunPhase.Takeoff)]
        [TestCase(ShipRunPhase.Cruise)]
        public void WhenFlying_AndTheHandleMoves_ThenItTakesAnyShareWithoutLaunching(ShipRunPhase phase) {
            ShipThrottleMove stop = _rule.Evaluate(phase, false, -FULL);
            ShipThrottleMove half = _rule.Evaluate(phase, false, ShipThrottleTestConfiguration.HALF);
            ShipThrottleMove over = _rule.Evaluate(phase, false, FULL * 2f);

            Assert.AreEqual(0f, stop.Position, TOLERANCE, "The throttle went below Stop.");
            Assert.AreEqual(ShipThrottleTestConfiguration.HALF, half.Position, TOLERANCE, "The throttle did not take Half in flight.");
            Assert.AreEqual(FULL, over.Position, TOLERANCE, "The throttle went past Full.");
            Assert.IsFalse(stop.IsLaunch || half.IsLaunch || over.IsLaunch, "A move in flight asked for a launch.");
        }
    }
}
