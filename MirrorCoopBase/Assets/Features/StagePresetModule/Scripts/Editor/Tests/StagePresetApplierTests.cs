using System.Collections.Generic;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Scripts.Targets;
using Features.ShopModule.Scripts.Configurations;
using Features.StagePresetModule.Data;
using Features.StagePresetModule.Scripts.Configurations;
using Features.StagePresetModule.Scripts.Core;
using Features.StagePresetModule.Scripts.Systems;
using NUnit.Framework;
using UnityEngine;

namespace Features.StagePresetModule.Editor.Tests {
    // The run values the applier gives the ship run and the wallet, with and without a selected preset, on the shipped
    // catalog. Installing the modules needs the ship's network objects: LoopSmokeTest checks it in a real run.
    public sealed class StagePresetApplierTests {
        private const string LATE_PRESET_ID = "late";
        private const int LATE_START_LOOP_INDEX = 9;
        private const string PROFILE_ID = "any-profile";

        private StagePresetSelectionModel _stagePresetSelectionModel;
        private StagePresetSelectionService _stagePresetSelectionService;
        private RecordingFieldProfileDebug _fieldProfileDebug;
        private WalletConfiguration _walletConfiguration;
        private StagePresetApplier _stagePresetApplier;
        private readonly List<Object> _created = new List<Object>();

        [SetUp]
        public void SetUp() {
            StagePresetCatalog catalog = AddressableConfigurationLoader.Load<StagePresetCatalog>(Address.Configurations.StagePresetCatalog_Default);
            _walletConfiguration = AddressableConfigurationLoader.Load<WalletConfiguration>(Address.Configurations.WalletConfiguration_Default);
            _stagePresetSelectionModel = new StagePresetSelectionModel();
            _stagePresetSelectionService = new StagePresetSelectionService(catalog, _stagePresetSelectionModel);
            _fieldProfileDebug = new RecordingFieldProfileDebug();
            _stagePresetApplier = new StagePresetApplier(_stagePresetSelectionService, new StagePresetSocketResolver(), _fieldProfileDebug, _walletConfiguration, new FieldTargetCountScaleModel());
        }

        [TearDown]
        public void TearDown() {
            foreach (Object created in _created)
                Object.DestroyImmediate(created);

            _created.Clear();
        }

        [Test]
        public void WhenNoPresetIsSelected_ThenRunStartsAsNormalGame() {
            Assert.AreEqual(0, _stagePresetApplier.StartLoopIndex, "Start loop of a normal run.");
            Assert.IsTrue(_stagePresetApplier.AreStationDropsEnabled, "A normal run keeps the station drops.");
            Assert.AreEqual(_walletConfiguration.StartingBalance, _stagePresetApplier.StartingBalance, "A normal run starts with the wallet configuration's money.");
        }

        [Test]
        public void WhenLatePresetIsSelected_ThenRunStartsOnItsLoopWithItsMoneyAndNoDrops() {
            _stagePresetSelectionService.Select(LATE_PRESET_ID);
            Assert.IsTrue(_stagePresetSelectionService.TryGetSelectedPreset(out StagePreset late), "Late is not selected.");

            Assert.AreEqual(LATE_START_LOOP_INDEX, _stagePresetApplier.StartLoopIndex, "Start loop of Late.");
            Assert.IsFalse(_stagePresetApplier.AreStationDropsEnabled, "Late keeps station drops.");
            Assert.AreEqual(late.StartingBalance, _stagePresetApplier.StartingBalance, "Late does not start with its own money.");
        }

        [Test]
        public void WhenRunEnds_ThenSelectionReturnsToNoPreset() {
            _stagePresetSelectionService.Select(LATE_PRESET_ID);

            _stagePresetApplier.OnRunEnded();

            Assert.AreEqual(string.Empty, _stagePresetSelectionModel.SelectedPresetId, "The preset outlived its run.");
            Assert.AreEqual(0, _stagePresetApplier.StartLoopIndex, "The next run does not start as a normal game.");
        }

        [Test]
        public void WhenUnknownPresetIsSelected_ThenSelectionThrows() =>
            Assert.Throws<KeyNotFoundException>(() => _stagePresetSelectionService.Select("no-such-preset"));

        [Test]
        public void WhenPresetWithFieldProfilePreparesShip_ThenFieldsUseThatProfile() {
            StagePresetApplier applier = CreateApplierWithProfiledPreset();

            applier.ServerPrepareShip(StagePresetTestAssets.LoadShipPlatform());

            Assert.AreEqual(PROFILE_ID, _fieldProfileDebug.UsedProfileId, "The preset's field profile is not forced for the run.");
        }

        [Test]
        public void WhenPresetWithFieldProfileRunEnds_ThenFieldProfileOverrideIsCleared() {
            StagePresetApplier applier = CreateApplierWithProfiledPreset();

            applier.OnRunEnded();

            Assert.AreEqual(1, _fieldProfileDebug.ClearCount, "The preset's field profile stays forced after the run.");
        }

        [Test]
        public void WhenSelectedPresetHasExtensionBlock_ThenSystemFindsItByType() {
            StagePreset preset = Remember(StagePresetTestAssets.CreatePreset("blocks", 0, 0));
            TestStagePresetBlock block = Remember(ScriptableObject.CreateInstance<TestStagePresetBlock>());
            StagePresetTestAssets.SetExtensions(preset, block);
            StagePresetSelectionService service = new StagePresetSelectionService(Remember(StagePresetTestAssets.CreateCatalog(preset)), _stagePresetSelectionModel);
            service.Select(preset.Id);

            Assert.IsTrue(service.TryGetSelectedExtension(out TestStagePresetBlock found), "The extension block is not found.");
            Assert.AreSame(block, found);
        }

        // A preset without modules: preparing the ship touches no socket, so the prefab asset stands in for the ship.
        private StagePresetApplier CreateApplierWithProfiledPreset() {
            StagePreset preset = Remember(StagePresetTestAssets.CreatePreset("profiled", 0, 0));
            StagePresetTestAssets.SetFieldProfile(preset, PROFILE_ID);
            StagePresetSelectionService service = new StagePresetSelectionService(Remember(StagePresetTestAssets.CreateCatalog(preset)), _stagePresetSelectionModel);
            service.Select(preset.Id);
            return new StagePresetApplier(service, new StagePresetSocketResolver(), _fieldProfileDebug, _walletConfiguration, new FieldTargetCountScaleModel());
        }

        private T Remember<T>(T created) where T : Object {
            _created.Add(created);
            return created;
        }
    }
}
