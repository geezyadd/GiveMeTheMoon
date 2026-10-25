using NUnit.Framework;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    // The cruise speed follows the throttle within the acceleration limit: the ship eases to a stop at 0 and stands
    // still in the field, and speeds up again just as smoothly.
    public sealed class ShipCruiseSpeedTests {
        private const float STEP_SECONDS = 1f / 60f;
        private const float MAX_SPEED = 28f;
        private const float MAX_ACCELERATION = 8f;
        private const float TOLERANCE = 0.001f;
        private const float STOP_SECONDS = 5f;
        private const float STILL_SECONDS = 2f;
        private const float TAKEOFF_SECONDS = 0.5f;
        private const float DODGE_RANGE_SCALE = 1f;
        private const float STILL_DRIFT_METERS = 0.01f;

        [Test]
        public void WhenTheThrottleDropsToStop_ThenTheSpeedFallsWithinTheAccelerationLimit_AndReachesZero() {
            ShipCruiseSpeed speed = new ShipCruiseSpeed();
            speed.Step(MAX_SPEED, 0f, STEP_SECONDS);

            float previous = speed.Current;
            for (float time = 0f; time < STOP_SECONDS; time += STEP_SECONDS) {
                speed.Step(0f, MAX_ACCELERATION, STEP_SECONDS);
                float acceleration = Mathf.Abs(speed.Current - previous) / STEP_SECONDS;
                Assert.LessOrEqual(acceleration, MAX_ACCELERATION + TOLERANCE, $"The speed changed at {acceleration} m/s2.");
                previous = speed.Current;
            }

            Assert.AreEqual(0f, speed.Current, TOLERANCE, "The ship did not come to a stop at throttle 0.");
        }

        [Test]
        public void WhenTheLimitIsZero_ThenTheSpeedTakesTheTargetAtOnce_AndNeverGoesBelowZero() {
            ShipCruiseSpeed speed = new ShipCruiseSpeed();

            Assert.AreEqual(MAX_SPEED, speed.Step(MAX_SPEED, 0f, STEP_SECONDS), TOLERANCE, "No limit did not reach the target at once.");
            Assert.AreEqual(0f, speed.Step(-MAX_SPEED, MAX_ACCELERATION, STEP_SECONDS * 1000f), TOLERANCE, "A negative target made the ship fly backwards.");
        }

        [Test]
        public void WhenTheCruiseThrottleIsStop_ThenTheShipStandsStillInTheField() {
            ShipFlight flight = new ShipFlight();
            flight.BeginTakeoff(Vector3.zero, Vector3.up * 10f, Quaternion.identity, null, TAKEOFF_SECONDS, DODGE_RANGE_SCALE, ShipFlightMode.TravelInSpace);
            for (float time = 0f; time < TAKEOFF_SECONDS; time += STEP_SECONDS)
                flight.Simulate(STEP_SECONDS);

            flight.BeginCruise();
            flight.SetTravelSpeed(0f);
            for (float time = 0f; time < STILL_SECONDS; time += STEP_SECONDS)
                flight.Simulate(STEP_SECONDS);

            Vector3 start = flight.Position;
            for (float time = 0f; time < STILL_SECONDS; time += STEP_SECONDS)
                flight.Simulate(STEP_SECONDS);

            Assert.Less(Vector3.Distance(start, flight.Position), STILL_DRIFT_METERS, "The ship moved at throttle 0.");
        }
    }
}
