using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The rectangular frame of long flights: the length follows A -> B, the width stays the configured one unless the
    // cruise start lies far off the axis, and the route from the cruise start to B keeps its passage.
    public sealed class FlightFieldLongFrameTests {
        private const int SEED = 23;
        private const float EASY = 0f;
        private const float HARD = 1f;
        private const float TAKEOFF_FORWARD = 350f;
        private const float TAKEOFF_HEIGHT = 100f;
        // Loop 0 and loop 4 at 120 s + 120 s per loop and 14 m/s.
        private const float FIRST_FLIGHT_DISTANCE = 1680f;
        private const float FIFTH_FLIGHT_DISTANCE = 8400f;
        private const float CONE_EDGE_DEGREES = 85f;

        private FlightFieldEditorComposition _composition;

        [OneTimeSetUp]
        public void SetUp() =>
            _composition = new FlightFieldEditorCompositionFactory().Create();

        [TestCase(FIRST_FLIGHT_DISTANCE)]
        [TestCase(FIFTH_FLIGHT_DISTANCE)]
        public void WhenFlightIsLong_ThenFrameIsAsLongAsTheFlightAndAsWideAsConfigured(float distance) {
            FlightField field = _composition.Generator.Generate(CreateRequest(distance, 0f, EASY));

            int expectedLength = Mathf.CeilToInt(distance / _composition.Configuration.CellSize) + _composition.Configuration.MarginBehindACells
                + _composition.Configuration.MarginBehindBCells;
            Assert.AreEqual(expectedLength, field.Frame.LengthCells);
            Assert.AreEqual(OddAtLeast(_composition.Configuration.FieldWidthCells), field.Frame.WidthCells);
        }

        [TestCase(-CONE_EDGE_DEGREES, EASY)]
        [TestCase(CONE_EDGE_DEGREES, EASY)]
        [TestCase(-CONE_EDGE_DEGREES, HARD)]
        [TestCase(CONE_EDGE_DEGREES, HARD)]
        public void WhenShipStartsAtTheConeEdge_ThenCruiseStartHasSideRoomAndTheRouteHasAPassage(float launchYaw, float difficulty) {
            FlightField field = _composition.Generator.Generate(CreateRequest(FIFTH_FLIGHT_DISTANCE, launchYaw, difficulty));

            int sideRoom = _composition.Configuration.SideMarginCells;
            Assert.GreaterOrEqual(field.CruiseStartCell.y, sideRoom, "No room beside the cruise start on the left.");
            Assert.LessOrEqual(field.CruiseStartCell.y, field.Frame.WidthCells - 1 - sideRoom, "No room beside the cruise start on the right.");
            Assert.IsFalse(field.IsPassageForced, "The passage was forced.");
            FieldTestOccupancy occupancy = new(field, _composition.Catalog, _composition.FootprintReader);
            foreach (FieldRoute route in field.Routes)
                Assert.IsTrue(occupancy.HasPassage(route.Start, field.StationBCell, _composition.Configuration.PassageClearanceCells), $"Route {route.Id} has no passage.");
        }

        // A on the origin, B straight along +z; the ship takes off with the given yaw from the A -> B axis.
        private static FieldRequest CreateRequest(float distance, float launchYaw, float difficulty) {
            Vector3 launchForward = Quaternion.AngleAxis(launchYaw, Vector3.up) * Vector3.forward;
            Vector3 cruiseStart = launchForward * TAKEOFF_FORWARD + Vector3.up * TAKEOFF_HEIGHT;
            List<FieldRouteRequest> routeRequests = new() { new FieldRouteRequest(difficulty) };
            return new FieldRequest(Vector3.zero, Vector3.forward * distance, cruiseStart, 0, SEED, routeRequests);
        }

        private static int OddAtLeast(int cells) =>
            cells % 2 == 0 ? cells + 1 : cells;
    }
}
