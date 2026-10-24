using NUnit.Framework;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    // The knockback as the deck feels it: added to the cruise, limited in acceleration, and the cruise speed comes
    // back after the fade with the ship left where it was pushed to.
    public sealed class ShipFlightKnockbackTests {
        private const float STEP_SECONDS = 1f / 60f;
        private const float TAKEOFF_SECONDS = 0.5f;
        private const float DODGE_RANGE_SCALE = 1f;
        private const float TRAVEL_SPEED = 14f;
        private const float FADE_SECONDS = 1.4f;
        private const float MAX_ACCELERATION = 22f;
        private const float BACK_SPEED_SHARE = 1.3f;
        // A head-on hit: away from the rock is straight back too.
        private const float AWAY_SPEED = 5f;
        private const float RECOVER_SECONDS = 3f;
        private const float SPEED_TOLERANCE = 0.05f;
        private const float ACCELERATION_TOLERANCE = 0.5f;
        private const float SETTLE_SECONDS = 0.5f;

        [Test]
        public void WhenCruisingShipIsKnockedBack_ThenItSlowsWithinTheAccelerationLimit_AndCruisesAgainAtFullSpeed() {
            ShipFlight flight = BeginCruise();
            Vector3 knockback = Vector3.back * (TRAVEL_SPEED * BACK_SPEED_SHARE + AWAY_SPEED);
            Vector3 cruiseVelocity = flight.Velocity;
            flight.ApplyKnockback(knockback, FADE_SECONDS, MAX_ACCELERATION);

            float slowest = float.MaxValue;
            Vector3 previous = cruiseVelocity;
            for (float time = 0f; time < RECOVER_SECONDS; time += STEP_SECONDS) {
                flight.Simulate(STEP_SECONDS);
                float acceleration = (flight.Velocity - previous).magnitude / STEP_SECONDS;
                Assert.LessOrEqual(acceleration, MAX_ACCELERATION + ACCELERATION_TOLERANCE, $"The deck accelerated at {acceleration} m/s2 at {time:0.00} s.");
                previous = flight.Velocity;
                slowest = Mathf.Min(slowest, flight.Velocity.z);
            }

            Assert.Less(slowest, 0f, "A knockback above the cruise speed did not move the ship back for a moment.");
            Assert.AreEqual(TRAVEL_SPEED, flight.Velocity.z, SPEED_TOLERANCE, "The cruise speed did not come back after the knockback.");
        }

        [Test]
        public void WhenTheKnockbackIsOver_ThenTheShipStaysWhereItWasPushedTo() {
            ShipFlight pushed = BeginCruise();
            ShipFlight calm = BeginCruise();
            pushed.ApplyKnockback(Vector3.right * TRAVEL_SPEED, FADE_SECONDS, MAX_ACCELERATION);

            for (float time = 0f; time < RECOVER_SECONDS; time += STEP_SECONDS) {
                pushed.Simulate(STEP_SECONDS);
                calm.Simulate(STEP_SECONDS);
            }

            Assert.Greater(pushed.Position.x - calm.Position.x, 1f, "The ship went back onto its old path after the knockback.");
            Assert.AreEqual(calm.Velocity.x, pushed.Velocity.x, SPEED_TOLERANCE, "The ship still drifts sideways after the knockback.");
        }

        private static ShipFlight BeginCruise() {
            ShipFlight flight = new ShipFlight();
            flight.BeginTakeoff(Vector3.zero, Vector3.up * 10f, Quaternion.identity, null, TAKEOFF_SECONDS, DODGE_RANGE_SCALE, ShipFlightMode.TravelInSpace);
            for (float time = 0f; time < TAKEOFF_SECONDS; time += STEP_SECONDS)
                flight.Simulate(STEP_SECONDS);

            flight.BeginCruise();
            flight.SetTravelSpeed(TRAVEL_SPEED);
            for (float time = 0f; time < SETTLE_SECONDS; time += STEP_SECONDS)
                flight.Simulate(STEP_SECONDS);

            return flight;
        }
    }
}
