using NUnit.Framework;

namespace Features.BehaviourTreeModule.Editor.Tests {
    public sealed class BtTickRateLimiterTests {
        private const float FRAME = 0.04f;
        private const float TICKS_PER_SECOND = 10f;
        private const float TOLERANCE = 0.0001f;
        private const int MANY_FRAMES = 250;
        private const float ODD_FRAME = 0.013f;

        [Test]
        public void WhenTheRateIsZero_ThenEveryUpdateTicksWithItsOwnDeltaTime() {
            BtTickRateLimiter limiter = new(0f);

            bool isTicked = limiter.TryAdvance(FRAME, out float treeDeltaTime);

            Assert.IsTrue(isTicked);
            Assert.AreEqual(FRAME, treeDeltaTime, TOLERANCE);
        }

        [Test]
        public void WhenFramesAreShorterThanATick_ThenOnlyTheThirdFrameTicks() {
            BtTickRateLimiter limiter = new(TICKS_PER_SECOND);

            Assert.IsFalse(limiter.TryAdvance(FRAME, out _));
            Assert.IsFalse(limiter.TryAdvance(FRAME, out _));
            Assert.IsTrue(limiter.TryAdvance(FRAME, out _));
        }

        [Test]
        public void WhenATickComes_ThenItPassesAllTheTimeGatheredSinceThePreviousTick() {
            BtTickRateLimiter limiter = new(TICKS_PER_SECOND);
            limiter.TryAdvance(FRAME, out _);
            limiter.TryAdvance(FRAME, out _);

            limiter.TryAdvance(FRAME, out float treeDeltaTime);

            Assert.AreEqual(FRAME * 3f, treeDeltaTime, TOLERANCE);
        }

        [Test]
        public void WhenManyFramesPass_ThenTheTreeLosesNoTime() {
            BtTickRateLimiter limiter = new(TICKS_PER_SECOND);
            float passedToTree = 0f;
            for (int i = 0; i < MANY_FRAMES; i++) {
                if (limiter.TryAdvance(ODD_FRAME, out float treeDeltaTime))
                    passedToTree += treeDeltaTime;
            }

            float notYetPassed = MANY_FRAMES * ODD_FRAME - passedToTree;

            Assert.GreaterOrEqual(notYetPassed, -TOLERANCE);
            Assert.Less(notYetPassed, 1f / TICKS_PER_SECOND);
        }
    }
}
