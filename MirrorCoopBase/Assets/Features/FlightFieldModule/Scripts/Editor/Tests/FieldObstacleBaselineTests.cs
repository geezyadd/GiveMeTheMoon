using Features.FlightFieldModule.Scripts.Model;
using NUnit.Framework;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The obstacle fields of a few seeds at loops 0, 4 and 9, generated as the game requests them, are the same as the
    // ones recorded: the placement count and the fingerprint of the whole field. The fingerprints were recorded again
    // on 2026-10-28 when the target balloons left the field (the placements did not change) and on 2026-10-29 when the
    // points of interest began to keep their circles free of rocks (fewer or more giants in a few fields).
    public sealed class FieldObstacleBaselineTests {
        private readonly FieldDebugScenario _scenario = new();

        private FlightFieldEditorComposition _composition;

        [OneTimeSetUp]
        public void OneTimeSetUp() =>
            _composition = new FlightFieldEditorCompositionFactory().Create();

        [TestCase(1, 0, 4, 1176359992948358199UL)]
        [TestCase(1, 4, 28, 16327222701710090695UL)]
        [TestCase(1, 9, 40, 17668616857568888889UL)]
        [TestCase(17, 0, 3, 12288706068405941610UL)]
        [TestCase(17, 4, 19, 13031565478490903231UL)]
        [TestCase(17, 9, 40, 17000124006079962307UL)]
        [TestCase(4242, 0, 4, 3993064328226909250UL)]
        [TestCase(4242, 4, 19, 3898574930994223302UL)]
        [TestCase(4242, 9, 51, 13023943383329471538UL)]
        [TestCase(90001, 0, 2, 15220784567837437174UL)]
        [TestCase(90001, 4, 19, 4263682984381317007UL)]
        [TestCase(90001, 9, 40, 3592024311172688534UL)]
        public void WhenAnObstacleFieldIsGenerated_ThenItIsTheRecordedOne(int seed, int loop, int placementCount, ulong fingerprint) {
            FlightField field = _composition.Generator.Generate(_scenario.CreateRequest(_composition.RequestFactory, seed, loop));

            Assert.AreEqual((placementCount, fingerprint), (field.Placements.Count, FieldObstacleFingerprint.Create(field)),
                $"Seed {seed} loop {loop} ({field.ProfileId}): the field is not the recorded one.");
        }
    }
}
