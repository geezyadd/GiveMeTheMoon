using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.SettingsModule.Data;
using NUnit.Framework;

namespace Features.SettingsModule.Editor.Tests {
    // Keeps the data rules of the settings catalog asset the game loads.
    public sealed class SettingsCatalogGuardTests {
        private SettingsCatalog _catalog;

        [OneTimeSetUp]
        public void OneTimeSetUp() =>
            _catalog = AddressableConfigurationLoader.Load<SettingsCatalog>(Address.Configurations.SettingsCatalog_Default);

        [Test]
        public void Catalog_HasNoEmptyEntries_AndKeysAreUnique() {
            Assert.That(_catalog, Is.Not.Null);
            Assert.That(_catalog.Settings, Has.None.Null);

            List<string> keys = _catalog.Settings.Select(definition => definition.Key).ToList();
            Assert.That(keys, Has.None.Null.And.None.Empty);
            Assert.That(keys, Is.Unique);
        }

        [Test]
        public void Catalog_EverySettingHasLabel() {
            foreach (SettingDefinition definition in _catalog.Settings)
                Assert.That(definition.Label, Is.Not.Null.And.Not.Empty, definition.name);
        }

        [Test]
        public void Catalog_HasDefinitionForEverySettingKey() {
            IEnumerable<string> settingKeys = typeof(SettingKeys)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral)
                .Select(field => (string)field.GetRawConstantValue());
            HashSet<string> catalogKeys = _catalog.Settings.Select(definition => definition.Key).ToHashSet();

            foreach (string key in settingKeys)
                Assert.That(catalogKeys, Does.Contain(key), $"No definition for {nameof(SettingKeys)} key '{key}'.");
        }

        [Test]
        public void Sliders_DefaultIsInRangeAndOnStep() {
            foreach (SliderSettingDefinition slider in _catalog.Settings.OfType<SliderSettingDefinition>()) {
                Assert.That(slider.Maximum, Is.GreaterThan(slider.Minimum), slider.name);
                Assert.That(slider.Normalize(slider.DefaultValue), Is.EqualTo(slider.DefaultValue).Within(0.0001f), slider.name);
            }
        }

        [Test]
        public void Choices_DefaultIsOneOfOptions_AndOptionIdsAreUnique() {
            foreach (ChoiceSettingDefinition choice in _catalog.Settings.OfType<ChoiceSettingDefinition>()) {
                Assert.That(choice.Options, Has.Count.GreaterThan(1), choice.name);
                Assert.That(choice.Options.Select(option => option.Id), Is.Unique, choice.name);
                Assert.That(choice.Options.Select(option => option.Label), Has.None.Null.And.None.Empty, choice.name);
                Assert.That(choice.AcceptsOption(choice.DefaultOptionId), Is.True, choice.name);
            }
        }

        [Test]
        public void Model_BuildsFromCatalog() =>
            Assert.DoesNotThrow(() => _ = new SettingsModel(_catalog));
    }
}
