using NUnit.Framework;

namespace Features.CharacterMovableModule.Scripts.Editor {
    public sealed class JumpPushTests {
        private const float STEP_SECONDS = 0.02f;
        private const float TAKEOFF_SPEED = 8.8f;
        private const float TOLERANCE = 0.0001f;

        [Test]
        public void WhenPushRunsToItsEnd_ThenItGivesTheWholeTakeoffSpeed() {
            JumpPush push = new JumpPush();
            push.Begin(TAKEOFF_SPEED, 0.07f);

            float total = 0f;
            int steps = 0;
            while (push.IsPushing) {
                total += push.Step(STEP_SECONDS);
                steps++;
            }

            Assert.AreEqual(TAKEOFF_SPEED, total, TOLERANCE);
            Assert.AreEqual(4, steps);
        }

        [Test]
        public void WhenPushIsLonger_ThenFirstStepGivesOnlyItsShare() {
            JumpPush push = new JumpPush();
            push.Begin(TAKEOFF_SPEED, 0.08f);

            Assert.AreEqual(TAKEOFF_SPEED * 0.25f, push.Step(STEP_SECONDS), TOLERANCE);
        }

        [Test]
        public void WhenPushTimeIsZero_ThenWholeSpeedComesInOneStep() {
            JumpPush push = new JumpPush();
            push.Begin(TAKEOFF_SPEED, 0f);

            Assert.AreEqual(TAKEOFF_SPEED, push.Step(STEP_SECONDS), TOLERANCE);
            Assert.IsFalse(push.IsPushing);
        }

        [Test]
        public void WhenPushIsStopped_ThenNothingMoreIsGiven() {
            JumpPush push = new JumpPush();
            push.Begin(TAKEOFF_SPEED, 0.08f);
            push.Step(STEP_SECONDS);

            push.Stop();

            Assert.IsFalse(push.IsPushing);
            Assert.AreEqual(0f, push.Step(STEP_SECONDS), TOLERANCE);
        }
    }
}
