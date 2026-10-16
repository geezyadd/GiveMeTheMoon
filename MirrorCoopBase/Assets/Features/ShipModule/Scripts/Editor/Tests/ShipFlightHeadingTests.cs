using NUnit.Framework;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipFlightHeadingTests {
        private const float STEP_SECONDS = 1f / 60f;
        private const float TAKEOFF_SECONDS = 9f;
        private const float CRUISE_SECONDS = 5f;
        private const float DODGE_RANGE_SCALE = 1f;
        private const float TRAVEL_SPEED = 14f;
        private const float HEADING_TOLERANCE_DEGREES = 0.1f;

        private static readonly Vector3 _takeoffForward = new Vector3(1f, 0f, 0.4f).normalized;
        private static readonly Vector3 _hover = _takeoffForward * 350f + Vector3.up * 100f;

        [Test]
        public void WhenTakeoffEnds_ThenTheHeadingIsTheTakeoffHeading() {
            ShipFlight flight = BeginTakeoff();

            Simulate(flight, TAKEOFF_SECONDS);

            Assert.IsTrue(flight.IsTakeoffComplete, "The takeoff did not end in its time.");
            AssertHeadingIsTakeoffHeading(flight);
        }

        [Test]
        public void WhenCruiseRunsWithoutSteer_ThenTheShipKeepsTheTakeoffHeading() {
            ShipFlight flight = BeginTakeoff();
            Simulate(flight, TAKEOFF_SECONDS);

            flight.BeginCruise();
            flight.SetTravelSpeed(TRAVEL_SPEED);
            Simulate(flight, CRUISE_SECONDS);

            AssertHeadingIsTakeoffHeading(flight);
            Vector3 travel = flight.Position - _hover;
            travel.y = 0f;
            Assert.Less(Vector3.Angle(_takeoffForward, travel), HEADING_TOLERANCE_DEGREES, "The ship left the takeoff heading in cruise.");
        }

        private static ShipFlight BeginTakeoff() {
            ShipFlight flight = new ShipFlight();
            flight.BeginTakeoff(
                Vector3.zero,
                _hover,
                Quaternion.LookRotation(_takeoffForward, Vector3.up),
                null,
                TAKEOFF_SECONDS,
                DODGE_RANGE_SCALE,
                ShipFlightMode.TravelInSpace);
            return flight;
        }

        private static void Simulate(ShipFlight flight, float seconds) {
            for (float time = 0f; time < seconds; time += STEP_SECONDS)
                flight.Simulate(STEP_SECONDS);
        }

        private static void AssertHeadingIsTakeoffHeading(ShipFlight flight) {
            Vector3 nose = flight.Rotation * Vector3.forward;
            nose.y = 0f;
            Assert.Less(Vector3.Angle(_takeoffForward, nose), HEADING_TOLERANCE_DEGREES, "The ship turned away from the takeoff heading.");
        }
    }
}
