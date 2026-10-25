using Features.FlightFieldModule.Scripts.Model;
using NUnit.Framework;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The loop tests fly the shortest route the game plans (ShipRoute: three approach distances, 210 m past the hover
    // point), so their fields are only about 22 cells long. On these seeds the field's one to three giants were drawn
    // among massive ones longer than the field, which were dropped, and the field had no placement (LoopSmoke: "The
    // field of loop 1 has no placements"); now only the giants that fit the frame are drawn.
    public sealed class FieldShortFlightTests {
        private const float SHORT_FLIGHT_METERS = 210f;

        private readonly FieldDebugScenario _scenario = new(SHORT_FLIGHT_METERS, 0f);

        private FlightFieldEditorComposition _composition;

        [OneTimeSetUp]
        public void OneTimeSetUp() =>
            _composition = new FlightFieldEditorCompositionFactory().Create();

        [TestCase(49, 0)]
        [TestCase(44, 1)]
        [TestCase(44, 2)]
        [TestCase(175, 3)]
        public void WhenAShortFlightsFieldIsGenerated_ThenItHasGiants(int seed, int loop) {
            FlightField field = _composition.Generator.Generate(_scenario.CreateRequest(_composition.RequestFactory, seed, loop));

            Assert.Greater(field.Placements.Count, 0, $"Seed {seed} loop {loop} ({field.ProfileId}, {field.Frame.LengthCells} x {field.Frame.WidthCells} cells): the field has no giant.");
        }
    }
}
