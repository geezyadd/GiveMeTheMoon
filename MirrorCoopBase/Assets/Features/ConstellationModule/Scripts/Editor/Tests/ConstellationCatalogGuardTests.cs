using System.Collections.Generic;
using System.Linq;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.ConstellationModule.Scripts.Definitions;
using NUnit.Framework;

namespace Features.ConstellationModule.Editor.Tests {
    // The synced lobby sky names a constellation by its id, so every catalog constellation has one and no two share it.
    public sealed class ConstellationCatalogGuardTests {
        private const string ORION = "orion";
        private const string BIG_DIPPER = "big_dipper";

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
        public void WhenAnIdIsLookedUp_ThenItFindsItsConstellation() {
            Assert.IsTrue(_catalog.TryFindIndex(ORION, out int orion));
            Assert.IsTrue(_catalog.TryFindIndex(BIG_DIPPER, out int bigDipper));
            Assert.AreEqual(ORION, _catalog.Constellations[orion].Id);
            Assert.AreEqual(BIG_DIPPER, _catalog.Constellations[bigDipper].Id);
        }
    }
}
