using Features.CharacterMovableModule.Scripts.Drift;
using NUnit.Framework;
using UnityEngine;

namespace Features.CharacterMovableModule.Editor.Tests {
    public sealed class CharacterDriftMotionTests {
        private const float STEP_SECONDS = 0.02f;
        private const float DAMPING = 1.2f;
        private const float DRIFT_SPEED = 0.6f;
        private const float PUSH_ACCELERATION = 0.35f;
        private const float MAX_PUSH_SPEED = 1.5f;
        private const float TOLERANCE = 0.01f;

        private readonly CharacterDriftMotion _motion = new CharacterDriftMotion();
        private readonly CharacterDriftSettings _settings = new CharacterDriftSettings(DAMPING, DRIFT_SPEED, PUSH_ACCELERATION, MAX_PUSH_SPEED);

        [Test]
        public void WhenBodyLeavesFast_AndDriftsLong_ThenItSlowsToDriftSpeedKeepingDirection() {
            Vector3 velocity = new Vector3(8f, -3f, 0f);
            Vector3 direction = velocity.normalized;

            velocity = Run(velocity, Vector3.zero, 10f);

            Assert.AreEqual(DRIFT_SPEED, velocity.magnitude, TOLERANCE);
            Assert.AreEqual(1f, Vector3.Dot(velocity.normalized, direction), TOLERANCE);
        }

        [Test]
        public void WhenBodyDriftsSlowerThanDriftSpeed_ThenItKeepsItsSpeed() {
            Vector3 velocity = new Vector3(0f, 0f, 0.3f);

            Assert.AreEqual(velocity, Run(velocity, Vector3.zero, 3f));
        }

        [Test]
        public void WhenNoForceActs_ThenHeightBarelyChanges() {
            Vector3 position = Vector3.zero;
            Vector3 velocity = new Vector3(6f, 0f, 0f);
            for (float time = 0f; time < 5f; time += STEP_SECONDS) {
                velocity = _motion.Step(velocity, Vector3.zero, _settings, STEP_SECONDS);
                position += velocity * STEP_SECONDS;
            }

            Assert.AreEqual(0f, position.y, TOLERANCE);
        }

        [Test]
        public void WhenMoveKeysPushLong_ThenSpeedStaysUnderPushCap() {
            Vector3 velocity = Run(Vector3.zero, Vector3.forward, 30f);

            Assert.LessOrEqual(velocity.z, MAX_PUSH_SPEED + TOLERANCE);
            Assert.Greater(velocity.z, DRIFT_SPEED);
        }

        [Test]
        public void WhenMoveKeysPushOneSecond_ThenTheyGiveOnlyAWeakNudge() {
            Vector3 velocity = Run(Vector3.zero, Vector3.forward, 1f);

            Assert.AreEqual(PUSH_ACCELERATION, velocity.z, TOLERANCE);
        }

        private Vector3 Run(Vector3 velocity, Vector3 push, float seconds) {
            for (float time = 0f; time < seconds; time += STEP_SECONDS)
                velocity = _motion.Step(velocity, push, _settings, STEP_SECONDS);

            return velocity;
        }
    }
}
