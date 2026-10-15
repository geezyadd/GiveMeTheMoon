using System;
using System.IO;
using System.Linq;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // Guards field structures as data: every structure is one definition in the catalog with an id, a prefab and a
    // footprint, the startup validators accept the game's assets, and no field code checks a concrete structure.
    public sealed class FieldStructureCatalogGuardTests {
        private const string MODULE_SCRIPTS_FOLDER = "Assets/Features/FlightFieldModule/Scripts";
        private const string SOURCE_PATTERN = "*.cs";
        private const string TESTS_FOLDER = "/Tests/";
        private const char QUOTE = '"';

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
        public void WhenCatalogIsRead_ThenTheRandomFillHasAnObstacleToPick() =>
            Assert.IsTrue(_composition.Catalog.Structures.Any(structure => structure.IsRandomObstacle), $"{_composition.Catalog.name} has no obstacle with a positive weight.");

        [Test]
        public void WhenCatalogHasNoObstacle_ThenTheStartupValidatorFails() {
            FieldStructureCatalog emptyCatalog = ScriptableObject.CreateInstance<FieldStructureCatalog>();
            try {
                Assert.Throws<InvalidOperationException>(new FieldStructureCatalogValidator(emptyCatalog).Initialize);
            } finally {
                Object.DestroyImmediate(emptyCatalog);
            }
        }

        [Test]
        public void WhenStartupValidatorsRunOnTheGameAssets_ThenTheyAcceptThem() {
            Assert.DoesNotThrow(new FieldStructureCatalogValidator(_composition.Catalog).Initialize);
            Assert.DoesNotThrow(new FlightFieldConfigurationValidator(_composition.Configuration, _composition.Catalog, _composition.FootprintReader).Initialize);
        }

        [Test]
        public void WhenFieldSourcesAreScanned_ThenNoneNamesAConcreteStructure() {
            string[] quotedIds = _composition.Catalog.Structures.Select(structure => QUOTE + structure.Id + QUOTE).ToArray();

            string[] offenders = Directory.GetFiles(MODULE_SCRIPTS_FOLDER, SOURCE_PATTERN, SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/'))
                .Where(path => !path.Contains(TESTS_FOLDER))
                .Where(path => quotedIds.Any(File.ReadAllText(path).Contains))
                .ToArray();

            Assert.IsEmpty(offenders, $"Structure behaviour comes from FieldStructureDefinition, not from checks of a structure id: {string.Join(", ", offenders)}");
        }
    }
}
