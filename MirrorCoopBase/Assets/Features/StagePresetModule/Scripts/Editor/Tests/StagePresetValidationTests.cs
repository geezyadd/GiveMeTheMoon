using System;
using System.Collections.Generic;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Requests;
using Features.ShipModule.Scripts;
using Features.StagePresetModule.Scripts.Configurations;
using Features.StagePresetModule.Scripts.Core;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Features.StagePresetModule.Editor.Tests {
    // A broken preset fails loudly: at startup (catalog validator) or at the run start (socket resolver), never by
    // silently skipping a module.
    public sealed class StagePresetValidationTests {
        private const string ENGINE_SOCKET = "engine-left";
        private const string RADAR_SOCKET = "radar";
        private const string ENGINE_MODULE_ID = "large-engine";

        private ModuleCatalog _moduleCatalog;
        private FieldProfileLookup _fieldProfileLookup;
        private ModuleDefinition _engine;
        private readonly List<Object> _created = new List<Object>();

        [SetUp]
        public void SetUp() {
            _moduleCatalog = AddressableConfigurationLoader.Load<ModuleCatalog>(Address.Configurations.ModuleCatalog_Default);
            _fieldProfileLookup = new FieldProfileLookup(AddressableConfigurationLoader.Load<FlightFieldConfiguration>(Address.Configurations.FlightFieldConfiguration_Default));
            Assert.IsTrue(_moduleCatalog.TryGet(ENGINE_MODULE_ID, out _engine), $"No module '{ENGINE_MODULE_ID}' in {_moduleCatalog.name}.");
        }

        [TearDown]
        public void TearDown() {
            foreach (Object created in _created)
                Object.DestroyImmediate(created);

            _created.Clear();
        }

        [Test]
        public void WhenTwoPresetsShareAnId_ThenStartupValidationThrows() =>
            AssertCatalogThrows(Remember(StagePresetTestAssets.CreatePreset("same", 0, 0)), Remember(StagePresetTestAssets.CreatePreset("same", 1, 0)));

        [Test]
        public void WhenPresetModuleIsNotInModuleCatalog_ThenStartupValidationThrows() {
            ModuleDefinition stranger = Remember(StagePresetTestAssets.CreateModule("stranger-engine", ShipModuleType.Engine));

            AssertCatalogThrows(Remember(StagePresetTestAssets.CreatePreset("stranger", 0, 0, (ENGINE_SOCKET, stranger))));
        }

        [Test]
        public void WhenPresetGivesOneSocketTwoModules_ThenStartupValidationThrows() =>
            AssertCatalogThrows(Remember(StagePresetTestAssets.CreatePreset("twice", 0, 0, (ENGINE_SOCKET, _engine), (ENGINE_SOCKET, _engine))));

        [Test]
        public void WhenPresetNamesUnknownFieldProfile_ThenStartupValidationThrows() {
            StagePreset preset = Remember(StagePresetTestAssets.CreatePreset("profile", 0, 0));
            StagePresetTestAssets.SetFieldProfile(preset, "no-such-profile");

            AssertCatalogThrows(preset);
        }

        [Test]
        public void WhenPresetHasTwoExtensionBlocksOfOneType_ThenStartupValidationThrows() {
            StagePreset preset = Remember(StagePresetTestAssets.CreatePreset("blocks", 0, 0));
            StagePresetTestAssets.SetExtensions(preset, Remember(ScriptableObject.CreateInstance<TestStagePresetBlock>()), Remember(ScriptableObject.CreateInstance<TestStagePresetBlock>()));

            AssertCatalogThrows(preset);
        }

        [Test]
        public void WhenPresetNamesSocketMissingOnShip_ThenSocketResolutionThrows() {
            StagePreset preset = Remember(StagePresetTestAssets.CreatePreset("socket", 0, 0, ("no-such-socket", _engine)));

            Assert.Throws<InvalidOperationException>(() => new StagePresetSocketResolver().ResolveSockets(preset, StagePresetTestAssets.LoadShipPlatformSockets()));
        }

        [Test]
        public void WhenPresetPutsModuleIntoSocketOfAnotherSlotType_ThenSocketResolutionThrows() {
            StagePreset preset = Remember(StagePresetTestAssets.CreatePreset("slot", 0, 0, (RADAR_SOCKET, _engine)));

            Assert.Throws<InvalidOperationException>(() => new StagePresetSocketResolver().ResolveSockets(preset, StagePresetTestAssets.LoadShipPlatformSockets()));
        }

        private void AssertCatalogThrows(params StagePreset[] presets) {
            StagePresetCatalog catalog = Remember(StagePresetTestAssets.CreateCatalog(presets));
            StagePresetCatalogValidator validator = new StagePresetCatalogValidator(catalog, _moduleCatalog, _fieldProfileLookup);

            Assert.Throws<InvalidOperationException>(validator.Initialize);
        }

        private T Remember<T>(T created) where T : Object {
            _created.Add(created);
            return created;
        }
    }
}
