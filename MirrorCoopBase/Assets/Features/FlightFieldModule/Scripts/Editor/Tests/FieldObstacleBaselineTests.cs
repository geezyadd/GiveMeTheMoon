using Features.FlightFieldModule.Scripts.Model;
using NUnit.Framework;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The obstacle fields of a few seeds at loops 0, 4 and 9, generated as the game requests them, are the same as the
    // ones recorded before the corridor layout was removed: the placement count and the fingerprint of the whole field.
    public sealed class FieldObstacleBaselineTests {
        private readonly FieldDebugScenario _scenario = new();

        private FlightFieldEditorComposition _composition;

        [OneTimeSetUp]
        public void OneTimeSetUp() =>
            _composition = new FlightFieldEditorCompositionFactory().Create();

        [TestCase(1, 0, 4, 14012438845407941327UL)]
        [TestCase(1, 4, 27, 3565145935162343041UL)]
        [TestCase(1, 9, 40, 6615088973800047845UL)]
        [TestCase(17, 0, 3, 17338988152066416266UL)]
        [TestCase(17, 4, 19, 12056753974636511904UL)]
        [TestCase(17, 9, 40, 12615092011371558839UL)]
        [TestCase(4242, 0, 4, 529724155212087567UL)]
        [TestCase(4242, 4, 19, 9995335679168272789UL)]
        [TestCase(4242, 9, 54, 815480881606481983UL)]
        [TestCase(90001, 0, 3, 7477742596392016989UL)]
        [TestCase(90001, 4, 19, 4566165125560628335UL)]
        [TestCase(90001, 9, 40, 10463606880954478747UL)]
        public void WhenAnObstacleFieldIsGenerated_ThenItIsTheRecordedOne(int seed, int loop, int placementCount, ulong fingerprint) {
            FlightField field = _composition.Generator.Generate(_scenario.CreateRequest(_composition.RequestFactory, seed, loop));

            Assert.AreEqual((placementCount, fingerprint), (field.Placements.Count, FieldObstacleFingerprint.Create(field)),
                $"Seed {seed} loop {loop} ({field.ProfileId}): the field is not the recorded one.");
        }
    }
}
