using NUnit.Framework;

namespace Features.AttackModule.Editor.Tests {
    public sealed class AttackTimelineTests {
        private static readonly AttackTiming Slap = new(0.2f, 0.34f, 0.5f, 0.8f, 0.15f, 0.6f, 0.8f);

        [TestCase(-0.1f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(0.1f, 0f)]
        [TestCase(0.2f, 0f)]
        [TestCase(0.27f, 0.5f)]
        [TestCase(0.34f, 1f)]
        [TestCase(0.45f, 1f)]
        [TestCase(0.65f, 0.5f)]
        [TestCase(0.8f, 0f)]
        [TestCase(1f, 0f)]
        public void TheReachWeight_WaitsForTheWindUpThenEasesInHoldsAndEasesOut(float sinceStart, float expected) {
            Assert.AreEqual(expected, AttackTimeline.ReachWeight(Slap, sinceStart), 0.001f);
        }

        [TestCase(0.1f, false)]
        [TestCase(0.15f, true)]
        [TestCase(0.4f, true)]
        [TestCase(0.6f, true)]
        [TestCase(0.61f, false)]
        public void TheHitWindow_IsOpenOnlyBetweenItsStartAndEnd(float sinceStart, bool expected) {
            Assert.AreEqual(expected, AttackTimeline.IsInHitWindow(Slap, sinceStart));
        }

        [Test]
        public void WhenTheHitWindowHasEnded_ThenTheAttackIsPastIt() {
            Assert.IsFalse(AttackTimeline.IsPastHitWindow(Slap, 0.6f));
            Assert.IsTrue(AttackTimeline.IsPastHitWindow(Slap, 0.61f));
        }

        [Test]
        public void WhenTheDurationHasPassed_ThenTheAttackIsOver() {
            Assert.IsFalse(AttackTimeline.IsOver(Slap, 0.79f));
            Assert.IsTrue(AttackTimeline.IsOver(Slap, 0.8f));
        }
    }
}
