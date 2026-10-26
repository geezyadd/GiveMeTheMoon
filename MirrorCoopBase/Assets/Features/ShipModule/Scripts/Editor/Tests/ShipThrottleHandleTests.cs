using Features.ShipModule.Scripts.Throttle;
using NUnit.Framework;

namespace Features.ShipModule.Scripts.Editor.Tests {
    // The handle under the hand: heavy (it follows the mouse with inertia, never jumps to it), soft detents that pull
    // it in and click once, and a max position it never passes.
    public sealed class ShipThrottleHandleTests {
        private const float STEP_SECONDS = 1f / 60f;
        private const float SETTLE_SECONDS = 2f;
        private const float TOLERANCE = 0.002f;
        // Look input that moves the hand to Full at once (100 units x 0.01).
        private const float LOOK_TO_FULL = 100f;
        private const float NEAR_SLOW = ShipThrottleTestConfiguration.SLOW + 0.03f;
        private const float FAR_FROM_DETENTS = 0.45f;
        // One look unit a frame: the hand crawls from Stop to Full in 100 frames, slower than the handle can follow.
        private const float SLOW_PUSH = 1f;
        private const int PUSH_FRAMES = 200;
        // Slow, Half and Full.
        private const int DETENTS_PAST_STOP = 3;

        private ShipThrottleTestConfiguration _configuration;
        private ShipThrottleDetentRule _detents;

        [SetUp]
        public void SetUp() {
            _configuration = new ShipThrottleTestConfiguration();
            _detents = new ShipThrottleDetentRule(_configuration.Configuration);
        }

        [TearDown]
        public void TearDown() =>
            _configuration.Dispose();

        [Test]
        public void WhenTheMouseJumpsToFull_ThenTheHandleFollowsWithInertia_AndArrivesThere() {
            ShipThrottleHandle handle = _configuration.CreateHandle();
            handle.Begin(0f);

            handle.Step(LOOK_TO_FULL, STEP_SECONDS, 1f);
            float firstStep = handle.Position;
            Settle(handle, 1f);

            Assert.Less(firstStep, ShipThrottleTestConfiguration.MAX_SPEED * STEP_SECONDS + TOLERANCE, "The handle moved faster than its top speed: it teleported to the mouse.");
            Assert.AreEqual(1f, handle.Position, TOLERANCE, "The handle did not arrive at Full.");
        }

        [Test]
        public void WhenTheHandIsNearADetent_ThenTheHandleIsPulledTowardsIt_AndFarFromDetentsItIsNot() {
            float pulled = _detents.PullTowardsDetent(NEAR_SLOW);

            Assert.Less(pulled, NEAR_SLOW, "A detent within its pull radius did not pull the handle.");
            Assert.Greater(pulled, ShipThrottleTestConfiguration.SLOW, "The soft detent snapped the handle onto it.");
            Assert.AreEqual(FAR_FROM_DETENTS, _detents.PullTowardsDetent(FAR_FROM_DETENTS), TOLERANCE, "A far detent pulled the handle.");
        }

        [Test]
        public void WhenTheHandleIsPushedFromStopToFull_ThenItClicksOncePerDetent() {
            ShipThrottleHandle handle = _configuration.CreateHandle();
            handle.Begin(0f);
            int clicks = 0;
            for (int i = 0; i < PUSH_FRAMES; i++) {
                if (handle.Step(SLOW_PUSH, STEP_SECONDS, 1f))
                    clicks++;
            }

            Assert.AreEqual(DETENTS_PAST_STOP, clicks, "A push from Stop to Full must click at Slow, Half and Full once each.");
        }

        [Test]
        public void WhenTheMaxPositionIsStop_ThenTheHandleNeverLeavesStop() {
            ShipThrottleHandle handle = _configuration.CreateHandle();
            handle.Begin(0f);

            Settle(handle, 0f);

            Assert.AreEqual(0f, handle.Position, TOLERANCE, "The handle left Stop with the max position at Stop.");
        }

        [Test]
        public void WhenTheHandleIsLetGoNearADetent_ThenItDropsIntoIt_AndBetweenDetentsItStays() {
            ShipThrottleHandle handle = _configuration.CreateHandle();
            handle.Begin(NEAR_SLOW);
            handle.Release(1f);
            Assert.AreEqual(ShipThrottleTestConfiguration.SLOW, handle.Position, TOLERANCE, "A handle let go next to Slow did not drop into it.");

            handle.Begin(FAR_FROM_DETENTS);
            handle.Release(1f);
            Assert.AreEqual(FAR_FROM_DETENTS, handle.Position, TOLERANCE, "A handle let go between detents did not stay where it was left.");
        }

        [TestCase(0.02f, "Stop")]
        [TestCase(0.5f, "Half")]
        [TestCase(0.9f, "Full")]
        public void WhenTheTooltipAsksTheMode_ThenItGetsTheNearestDetentName(float position, string expected) =>
            Assert.AreEqual(expected, _detents.FindNearestName(position));

        private static void Settle(ShipThrottleHandle handle, float maxPosition) {
            for (float time = 0f; time < SETTLE_SECONDS; time += STEP_SECONDS)
                handle.Step(LOOK_TO_FULL, STEP_SECONDS, maxPosition);
        }
    }
}
