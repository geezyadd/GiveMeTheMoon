using System.Linq;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Requests;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using Features.StagePresetModule.Scripts.Configurations;
using Features.StagePresetModule.Scripts.Core;
using NUnit.Framework;

namespace Features.StagePresetModule.Editor.Tests {
    // Guards the shipped stage presets: the startup validator passes, every preset fits ShipPlatform's sockets, the
    // Early / Mid / Late stages start where the playtests expect them and their money buys one to three shop items.
    public sealed class StagePresetCatalogGuardTests {
        private const int MIN_AFFORDABLE_ITEMS = 1;
        private const int MAX_AFFORDABLE_EXPENSIVE_ITEMS = 3;

        private StagePresetCatalog _stagePresetCatalog;
        private ModuleCatalog _moduleCatalog;
        private FlightFieldConfiguration _flightFieldConfiguration;

        [SetUp]
        public void SetUp() {
            _stagePresetCatalog = AddressableConfigurationLoader.Load<StagePresetCatalog>(Address.Configurations.StagePresetCatalog_Default);
            _moduleCatalog = AddressableConfigurationLoader.Load<ModuleCatalog>(Address.Configurations.ModuleCatalog_Default);
            _flightFieldConfiguration = AddressableConfigurationLoader.Load<FlightFieldConfiguration>(Address.Configurations.FlightFieldConfiguration_Default);
        }

        [Test]
        public void WhenShippedCatalogIsValidated_ThenStartupValidatorPasses() {
            StagePresetCatalogValidator validator = new StagePresetCatalogValidator(_stagePresetCatalog, _moduleCatalog, new FieldProfileLookup(_flightFieldConfiguration));

            Assert.DoesNotThrow(validator.Initialize);
        }

        [Test]
        public void WhenShippedPresetsAreResolvedOnShipPlatform_ThenEverySocketExistsAndTakesItsModule() {
            ShipSocket[] sockets = StagePresetTestAssets.LoadShipPlatformSockets();
            StagePresetSocketResolver resolver = new StagePresetSocketResolver();

            foreach (StagePreset preset in _stagePresetCatalog.Presets)
                Assert.AreEqual(preset.Modules.Count, resolver.ResolveSockets(preset, sockets).Count, $"{preset.name}: not every module found its socket.");
        }

        [TestCase("early", 0)]
        [TestCase("mid", 4)]
        [TestCase("late", 9)]
        public void WhenStagePresetIsRead_ThenItStartsOnItsLoopWithoutStationDrops(string presetId, int startLoopIndex) {
            StagePreset preset = _stagePresetCatalog.Presets.FirstOrDefault(candidate => candidate.Id == presetId);

            Assert.IsNotNull(preset, $"No stage preset '{presetId}' in {_stagePresetCatalog.name}.");
            Assert.AreEqual(startLoopIndex, preset.StartLoopIndex, $"{preset.name}: start loop.");
            Assert.IsFalse(preset.AreStationDropsEnabled, $"{preset.name}: playtest presets give no free drops.");
            Assert.IsFalse(preset.HasFieldProfile, $"{preset.name}: the loop picks the field, as in a normal run.");
        }

        [Test]
        public void WhenPresetMoneyIsComparedWithShopPrices_ThenItBuysOneToThreeItems() {
            ShopCatalog shopCatalog = AddressableConfigurationLoader.Load<ShopCatalog>(Address.Configurations.ShopCatalog_Default);
            long cheapest = shopCatalog.Modules.Min(module => module.Price);
            long mostExpensive = shopCatalog.Modules.Max(module => module.Price);

            foreach (StagePreset preset in _stagePresetCatalog.Presets) {
                Assert.GreaterOrEqual(preset.StartingBalance / cheapest, MIN_AFFORDABLE_ITEMS, $"{preset.name}: the money buys nothing.");
                Assert.LessOrEqual(preset.StartingBalance / mostExpensive, MAX_AFFORDABLE_EXPENSIVE_ITEMS, $"{preset.name}: the money buys too much, the shop would not matter.");
            }
        }
    }
}
