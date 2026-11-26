using NUnit.Framework;

namespace Features.PlayerAnimationModule.Editor.Tests {
    public sealed class AirborneDelayTests {
        private const float DELAY_SECONDS = 0.2f;
        private const float STEP_SECONDS = 0.02f;

        [Test]
        public void WhenBodyIsOnGround_ThenItDoesNotFly() {
            AirborneDelay delay = new AirborneDelay();

            Assert.IsFalse(delay.Step(false, DELAY_SECONDS, STEP_SECONDS));
        }

        [Test]
        public void WhenBodyLiftsForLessThanDelay_ThenItDoesNotFly() {
            AirborneDelay delay = new AirborneDelay();

            bool isFlying = Run(delay, true, DELAY_SECONDS * 0.5f);

            Assert.IsFalse(isFlying);
        }

        [Test]
        public void WhenBodyStaysInAirForDelay_ThenItFlies() {
            AirborneDelay delay = new AirborneDelay();

            bool isFlying = Run(delay, true, DELAY_SECONDS + STEP_SECONDS);

            Assert.IsTrue(isFlying);
        }

        [Test]
        public void WhenFlyingBodyTouchesGround_ThenItStopsFlyingAtOnce() {
            AirborneDelay delay = new AirborneDelay();
            Run(delay, true, DELAY_SECONDS * 2f);

            Assert.IsFalse(delay.Step(false, DELAY_SECONDS, STEP_SECONDS));
        }

        [Test]
        public void WhenShortLiftsFollowEachOther_ThenTheyDoNotAddUp() {
            AirborneDelay delay = new AirborneDelay();
            Run(delay, true, DELAY_SECONDS * 0.6f);
            delay.Step(false, DELAY_SECONDS, STEP_SECONDS);

            bool isFlying = Run(delay, true, DELAY_SECONDS * 0.6f);

            Assert.IsFalse(isFlying);
        }

        [Test]
        public void WhenDelayIsZero_ThenBodyFliesOnFirstStepInAir() {
            AirborneDelay delay = new AirborneDelay();

            Assert.IsTrue(delay.Step(true, 0f, STEP_SECONDS));
        }

        private static bool Run(AirborneDelay delay, bool isInAir, float seconds) {
            bool isFlying = false;
            for (float time = 0f; time < seconds - 0.0001f; time += STEP_SECONDS)
                isFlying = delay.Step(isInAir, DELAY_SECONDS, STEP_SECONDS);
            return isFlying;
        }
    }
}
