using Features.ShipModule.Scripts.Impacts;
using NUnit.Framework;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipKnockbackTests {
        private const float STEP_SECONDS = 1f / 60f;
        private const float FADE_SECONDS = 1.4f;
        private const float MAX_ACCELERATION = 22f;
        private const float TOLERANCE = 0.0001f;
        // The velocity follows the target at the acceleration limit, so it settles a little after the fade.
        private const float SETTLE_SECONDS = 1.5f;
        private const float PUSH_OUT_SPEED = 3f;
        // The held target fades for the one step between two contacts.
        private const float PUSH_OUT_TOLERANCE = 0.01f;

        private static readonly Vector3 _impulse = new(4f, 0f, -17f);

        [Test]
        public void WhenHit_ThenTheVelocityNeverChangesFasterThanTheLimit() {
            ShipKnockback knockback = new ShipKnockback();
            knockback.Apply(_impulse, FADE_SECONDS, MAX_ACCELERATION);

            Vector3 previous = knockback.Velocity;
            for (float time = 0f; time < FADE_SECONDS + SETTLE_SECONDS; time += STEP_SECONDS) {
                knockback.Step(STEP_SECONDS);
                float acceleration = (knockback.Velocity - previous).magnitude / STEP_SECONDS;
                Assert.LessOrEqual(acceleration, MAX_ACCELERATION + TOLERANCE, $"The knockback accelerated at {acceleration} m/s2 at {time:0.00} s.");
                previous = knockback.Velocity;
            }
        }

        [Test]
        public void WhenHit_ThenTheTargetFadesToZeroOverTheFadeTime() {
            ShipKnockback knockback = new ShipKnockback();
            knockback.Apply(_impulse, FADE_SECONDS, MAX_ACCELERATION);

            float previous = knockback.Target.magnitude;
            Assert.AreEqual(_impulse.magnitude, previous, TOLERANCE, "A hit does not start at its full impulse.");
            for (float time = 0f; time < FADE_SECONDS; time += STEP_SECONDS) {
                knockback.Step(STEP_SECONDS);
                Assert.LessOrEqual(knockback.Target.magnitude, previous + TOLERANCE, $"The knockback grew at {time:0.00} s.");
                previous = knockback.Target.magnitude;
            }

            knockback.Step(STEP_SECONDS);
            Assert.AreEqual(Vector3.zero, knockback.Target, "The knockback is still pushing after its fade time.");
        }

        [Test]
        public void WhenTheFadeIsOver_ThenTheKnockbackComesToRestAndStops() {
            ShipKnockback knockback = new ShipKnockback();
            knockback.Apply(_impulse, FADE_SECONDS, MAX_ACCELERATION);

            for (float time = 0f; time < FADE_SECONDS + SETTLE_SECONDS; time += STEP_SECONDS)
                knockback.Step(STEP_SECONDS);

            Assert.IsFalse(knockback.IsActive, "The knockback did not come to rest.");
            Assert.AreEqual(Vector3.zero, knockback.Step(STEP_SECONDS), "A knockback at rest still moves the ship.");
        }

        [Test]
        public void WhenTheContactLasts_ThenTheKnockbackKeepsPushingOut() {
            ShipKnockback knockback = new ShipKnockback();
            knockback.Apply(_impulse, FADE_SECONDS, MAX_ACCELERATION);

            for (float time = 0f; time < FADE_SECONDS * 2f; time += STEP_SECONDS) {
                knockback.Sustain(Vector3.right, PUSH_OUT_SPEED);
                knockback.Step(STEP_SECONDS);
            }

            Assert.GreaterOrEqual(Vector3.Dot(knockback.Target, Vector3.right), PUSH_OUT_SPEED - PUSH_OUT_TOLERANCE, "The knockback stopped pushing out of the contact.");
            Assert.GreaterOrEqual(Vector3.Dot(knockback.Velocity, Vector3.right), PUSH_OUT_SPEED - PUSH_OUT_TOLERANCE, "The ship does not move out of the contact.");
        }
    }
}
