using System;
using System.Linq;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using NUnit.Framework;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // Guards field structures as data: every structure is one definition in the catalog with an id, a prefab and a
    // footprint, every debris kind has an id and a recipe, the startup validators accept the game's assets, and no
    // field code checks a concrete structure.
    public sealed class FieldStructureCatalogGuardTests {
        private const string MODULE_SCRIPTS_FOLDER = "Assets/Features/FlightFieldModule/Scripts";
        private const string TESTS_FOLDER = "/Tests/";
        private const char QUOTE = '"';
        private const string DIFFICULTY_PROFILES_FIELD = "_difficultyProfiles";

        private FlightFieldEditorComposition _composition;

        [OneTimeSetUp]
        public void OneTimeSetUp() =>
            _composition = new FlightFieldEditorCompositionFactory().Create();

        [Test]
        public void WhenCatalogIsRead_ThenEveryIdIsSetAndUnique() {
            string[] ids = _composition.Catalog.Structures.Select(structure => structure.Id).ToArray();

            Assert.IsFalse(ids.Any(string.IsNullOrEmpty), $"A structure in {_composition.Catalog.name} has no id.");
            string[] duplicates = ids.GroupBy(id => id).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
            Assert.IsEmpty(duplicates, $"Structure ids used twice, a placement would build the wrong structure: {string.Join(", ", duplicates)}");
        }

        [Test]
        public void WhenCatalogIsRead_ThenEveryStructureHasAPrefabAKindAndAnOccupiedFootprint() {
            foreach (FieldStructureDefinition structure in _composition.Catalog.Structures) {
                Assert.IsNotNull(structure.Prefab, $"{structure.name} has no prefab.");
                Assert.AreNotEqual(FieldStructureKind.None, structure.Kind, $"{structure.name} has no kind.");
                Assert.IsNotEmpty(_composition.FootprintReader.Read(structure, FieldRotation.None).Cells, $"{structure.name} has an empty footprint.");
            }
        }

        [Test]
        public void WhenCatalogIsRead_ThenEveryDebrisKindHasAUniqueIdAndARecipe() {
            string[] ids = _composition.Catalog.Debris.Select(debris => debris.Id).ToArray();

            Assert.IsNotEmpty(ids, $"{_composition.Catalog.name} has no debris kind.");
            Assert.IsFalse(ids.Any(string.IsNullOrEmpty), $"A debris kind in {_composition.Catalog.name} has no id.");
            Assert.AreEqual(ids.Length, ids.Distinct().Count(), "A debris id is used twice.");
            Assert.IsTrue(_composition.Catalog.Debris.All(debris => debris.Recipe != null), "A debris kind has no recipe.");
        }

        [Test]
        public void WhenConfigurationHasNoProfile_ThenTheProfileValidatorFails() {
            FlightFieldConfiguration configuration = Object.Instantiate(_composition.Configuration);
            try {
                SerializedObject serializedConfiguration = new(configuration);
                serializedConfiguration.FindProperty(DIFFICULTY_PROFILES_FIELD).arraySize = 0;
                serializedConfiguration.ApplyModifiedPropertiesWithoutUndo();

                Assert.Throws<InvalidOperationException>(new FieldDifficultyProfileValidator(configuration, _composition.Catalog, _composition.FootprintReader).Initialize);
            } finally {
                Object.DestroyImmediate(configuration);
            }
        }

        [Test]
        public void WhenStartupValidatorsRunOnTheGameAssets_ThenTheyAcceptThem() {
            Assert.DoesNotThrow(new FieldStructureCatalogValidator(_composition.Catalog).Initialize);
            Assert.DoesNotThrow(new FlightFieldConfigurationValidator(_composition.Configuration).Initialize);
            Assert.DoesNotThrow(new FieldDifficultyProfileValidator(_composition.Configuration, _composition.Catalog, _composition.FootprintReader).Initialize);
        }

        [Test]
        public void WhenFieldSourcesAreScanned_ThenNoneNamesAConcreteStructure() {
            string[] quotedIds = _composition.Catalog.Structures.Select(structure => QUOTE + structure.Id + QUOTE).ToArray();

            string[] offenders = SourceScan.FindFilesMatching(MODULE_SCRIPTS_FOLDER, TESTS_FOLDER, source => quotedIds.Any(source.Contains));

            Assert.IsEmpty(offenders, $"Structure behaviour comes from FieldStructureDefinition, not from checks of a structure id: {string.Join(", ", offenders)}");
        }
    }
}
