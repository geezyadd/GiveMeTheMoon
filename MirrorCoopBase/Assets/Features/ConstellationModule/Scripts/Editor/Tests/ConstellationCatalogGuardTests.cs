using System.Collections.Generic;
using System.Linq;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.ConstellationModule.Data;
using Features.ConstellationModule.Scripts.Core;
using Features.ConstellationModule.Scripts.Definitions;
using NUnit.Framework;

namespace Features.ConstellationModule.Editor.Tests {
    // The synced lobby sky names a constellation by its id, so every catalog constellation has one, no two share it and
    // none takes a reserved sky id; "Show all" draws the whole catalog in one mesh, so it fits the shader's step array.
    public sealed class ConstellationCatalogGuardTests {
        private const string ORION = "orion";
        private const string LEO = "leo";

        private ConstellationCatalog _catalog;

        [OneTimeSetUp]
        public void OneTimeSetUp() =>
            _catalog = AddressableConfigurationLoader.Load<ConstellationCatalog>(Address.Configurations.ConstellationCatalog_Default);

        [Test]
        public void WhenTheCatalogIsChecked_ThenEveryConstellationHasAnIdAndNoTwoShareIt() {
            List<string> ids = _catalog.Constellations.Select(constellation => constellation.Id).ToList();

            Assert.IsTrue(ids.All(id => string.IsNullOrEmpty(id) == false), "A catalog constellation has no id.");
            CollectionAssert.AllItemsAreUnique(ids, "Two catalog constellations share an id.");
        }

        [Test]
        public void WhenTheCatalogIsChecked_ThenNoConstellationTakesTheAllId() =>
            Assert.IsFalse(_catalog.TryFindIndex(ConstellationSkyIds.ALL_CONSTELLATIONS, out _));

        [Test]
        public void WhenTheWholeCatalogIsCounted_ThenItsStarsAndLinesFitTheRevealSteps() {
            int stars = _catalog.Constellations.Sum(constellation => constellation.Stars.Count);
            int lines = _catalog.Constellations.Sum(constellation => constellation.Lines.Count);

            Assert.LessOrEqual(stars, ConstellationMeshBuilder.MAX_REVEAL_STEPS);
            Assert.LessOrEqual(lines, ConstellationMeshBuilder.MAX_REVEAL_STEPS);
        }

        [Test]
        public void WhenAnIdIsLookedUp_ThenItFindsItsConstellation() {
            Assert.IsTrue(_catalog.TryFindIndex(ORION, out int orion));
            Assert.IsTrue(_catalog.TryFindIndex(LEO, out int leo));
            Assert.AreEqual(ORION, _catalog.Constellations[orion].Id);
            Assert.AreEqual(LEO, _catalog.Constellations[leo].Id);
        }
    }
}
