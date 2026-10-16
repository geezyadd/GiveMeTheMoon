using Features.CharacterMovableModule.Scripts;
using Features.FloatingControllerModule;
using NUnit.Framework;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class RideVerticalTests {
        private const float STEP = 0.02f;
        private const float GRAVITY = -9.81f;
        private const float REST = 1f;
        private const float FLOOR = 0f;
        private const float DROP = 2f;
        private const float SECONDS = 3f;
        private const float RISING_SPEED = 0.15f;
        private const float FALLING_SPEED = -0.15f;

        private static readonly HoverSpringSettings _spring = new HoverSpringSettings(1f, 250f, 25f, 50f, -5f, 0.1f, 1.3f);
        private static readonly JumpTuning _jump = new JumpTuning(4f, 0.1f, 0.2f, 0.2f, 1.2f, 2f, 3f, 0);
        private static readonly RideVerticalFrame _frame = new RideVerticalFrame(_spring, _jump, REST, FLOOR, GRAVITY, STEP);

        [Test]
        public void WhenRideRunsAtThePhysicsStep_ThenItMatchesTheStationFloat() {
            RideVertical ride = Drop(DROP);
            float height = REST + DROP;
            float stationHeight = height;
            float stationVelocity = 0f;

            for (float time = 0f; time < SECONDS; time += STEP) {
                // The ride shows its steps interpolated one step behind, like an interpolated rigidbody.
                float shownStation = stationHeight;
                height = ride.Advance(height, STEP, _frame);
                StationStep(ref stationHeight, ref stationVelocity);

                Assert.AreEqual(shownStation, height, 0.0001f, $"at {time:0.00} s");
            }
        }

        [TestCase(1f / 30f)]
        [TestCase(1f / 60f)]
        [TestCase(1f / 144f)]
        public void WhenRiderLandsAtAFrameRate_ThenItSettlesAtRestWithoutRinging(float dt) {
            RideVertical ride = Drop(DROP);
            float height = REST + DROP;
            float lowest = height;
            int restCrossings = 0;
            bool above = true;

            for (float time = 0f; time < SECONDS; time += dt) {
                height = ride.Advance(height, dt, _frame);
                lowest = Mathf.Min(lowest, height);
                bool nowAbove = height > REST;
                if (nowAbove != above)
                    restCrossings++;

                above = nowAbove;
            }

            Assert.GreaterOrEqual(lowest, FLOOR, "sank through the deck");
            Assert.LessOrEqual(restCrossings, 3, "rings around the stand height");
            Assert.AreEqual(REST, height, 0.01f);
            // Settled, not passing through: it stays at the stand height.
            for (float time = 0f; time < 1f; time += dt) {
                height = ride.Advance(height, dt, _frame);
                Assert.AreEqual(REST, height, 0.01f, "still moving after settling");
            }
        }

        [TestCase(1f / 30f)]
        [TestCase(1f / 144f)]
        public void WhenFrameRateChanges_ThenLandingFeelsTheSame(float dt) {
            RideLanding reference = Land(STEP);
            RideLanding landing = Land(dt);

            Assert.AreEqual(reference.Lowest, landing.Lowest, 0.05f, "sag");
            Assert.AreEqual(reference.Highest, landing.Highest, 0.02f, "overshoot");
            Assert.AreEqual(reference.SettleSeconds, landing.SettleSeconds, 0.05f, "settle time");
        }

        private static RideLanding Land(float dt) {
            RideVertical ride = Drop(DROP);
            float height = REST + DROP;
            RideLanding landing = new RideLanding { Lowest = height, Highest = float.MinValue };
            bool touchedDown = false;
            for (float time = 0f; time < SECONDS; time += dt) {
                height = ride.Advance(height, dt, _frame);
                touchedDown |= height < REST;
                if (touchedDown == false)
                    continue;

                landing.Lowest = Mathf.Min(landing.Lowest, height);
                landing.Highest = Mathf.Max(landing.Highest, height);
                if (Mathf.Abs(height - REST) > 0.02f)
                    landing.SettleSeconds = time;
            }

            return landing;
        }

        private static RideVertical Drop(float above) {
            RideVertical ride = new RideVertical();
            ride.Reset(REST + above, 0f, false, _jump.ExtraAirJumps);
            return ride;
        }

        // FloatingController + CharacterMovableBase over a static floor for one physics step, as PhysX integrates it.
        private static void StationStep(ref float height, ref float velocity) {
            float supportDistance = height;
            bool supported = HoverSpring.Reaches(_spring, supportDistance);
            float acceleration = GRAVITY;
            if (supported && HoverSpring.Holds(_spring, velocity))
                acceleration += HoverSpring.Acceleration(_spring, supportDistance, velocity) - GRAVITY;

            if (supported == false) {
                if (velocity > RISING_SPEED)
                    acceleration += GRAVITY * _jump.ShortHopGravity;
                else if (velocity < FALLING_SPEED)
                    acceleration += GRAVITY * _jump.FallGravity;
            }

            velocity += acceleration * STEP;
            height += velocity * STEP;
            if (height >= FLOOR)
                return;

            height = FLOOR;
            if (velocity < 0f)
                velocity = 0f;
        }
    }
}
