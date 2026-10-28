using Features.FlightFieldModule.Scripts.Model;
using NUnit.Framework;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The obstacle fields of a few seeds at loops 0, 4 and 9, generated as the game requests them, are the same as the
    // ones recorded: the placement count and the fingerprint of the whole field. The fingerprints were recorded again
    // on 2026-10-28 when the target balloons left the field (the placements did not change).
    public sealed class FieldObstacleBaselineTests {
        private readonly FieldDebugScenario _scenario = new();

        private FlightFieldEditorComposition _composition;

        [OneTimeSetUp]
        public void OneTimeSetUp() =>
            _composition = new FlightFieldEditorCompositionFactory().Create();

        [TestCase(1, 0, 4, 9310539641746725722UL)]
        [TestCase(1, 4, 27, 3442379235916804690UL)]
        [TestCase(1, 9, 40, 9299011368910141891UL)]
        [TestCase(17, 0, 3, 8813004346680604886UL)]
        [TestCase(17, 4, 19, 11875610887326397289UL)]
        [TestCase(17, 9, 40, 14707228628138915923UL)]
        [TestCase(4242, 0, 4, 12068548170684716041UL)]
        [TestCase(4242, 4, 19, 376226051473175674UL)]
        [TestCase(4242, 9, 54, 5027669935819369183UL)]
        [TestCase(90001, 0, 3, 5227632656245087023UL)]
        [TestCase(90001, 4, 19, 17514344904788608429UL)]
        [TestCase(90001, 9, 40, 2636113005272920798UL)]
        public void WhenAnObstacleFieldIsGenerated_ThenItIsTheRecordedOne(int seed, int loop, int placementCount, ulong fingerprint) {
            FlightField field = _composition.Generator.Generate(_scenario.CreateRequest(_composition.RequestFactory, seed, loop));

            Assert.AreEqual((placementCount, fingerprint), (field.Placements.Count, FieldObstacleFingerprint.Create(field)),
                $"Seed {seed} loop {loop} ({field.ProfileId}): the field is not the recorded one.");
        }
    }
}
