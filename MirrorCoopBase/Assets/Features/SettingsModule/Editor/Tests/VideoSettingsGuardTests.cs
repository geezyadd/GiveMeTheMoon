using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.SettingsModule.Data;
using Features.SettingsModule.Video;
using NUnit.Framework;
using UnityEngine;

namespace Features.SettingsModule.Editor.Tests {
    // Every option of the video choice settings means something in the video configuration.
    public sealed class VideoSettingsGuardTests {
        private SettingsModel _model;
        private VideoSettingsConfiguration _configuration;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _model = new SettingsModel(AddressableConfigurationLoader.Load<SettingsCatalog>(Address.Configurations.SettingsCatalog_Default));
            _configuration = AddressableConfigurationLoader.Load<VideoSettingsConfiguration>(
                Address.Configurations.VideoSettingsConfiguration_Default);
        }

        [Test]
        public void EveryWindowModeOption_HasAMode() {
            foreach (SettingChoiceOption option in GetOptions(SettingKeys.WINDOW_MODE))
                Assert.That(_configuration.TryGetWindowMode(option.Id, out _), Is.True, option.Id);
        }

        [Test]
        public void EveryAntiAliasingOption_HasSamples() {
            foreach (SettingChoiceOption option in GetOptions(SettingKeys.ANTI_ALIASING)) {
                Assert.That(_configuration.TryGetAntiAliasingSamples(option.Id, out int samples), Is.True, option.Id);
                Assert.That(new[] { 1, 2, 4, 8 }, Does.Contain(samples), option.Id);
            }
        }

        [Test]
        public void EveryShadowOption_HasALevel() {
            foreach (SettingChoiceOption option in GetOptions(SettingKeys.SHADOW_QUALITY)) {
                Assert.That(_configuration.TryGetShadowLevel(option.Id, out ShadowQualityLevel level), Is.True, option.Id);
                Assert.That(level.CascadeCount, Is.InRange(1, 4), option.Id);
                Assert.That(Mathf.IsPowerOfTwo(level.ShadowmapResolution), Is.True, option.Id);
                Assert.That(level.ShadowDistance, Is.GreaterThanOrEqualTo(0f), option.Id);
            }
        }

        [Test]
        public void EveryFpsLimitOption_IsUnlimitedOrAFrameRate() {
            foreach (SettingChoiceOption option in GetOptions(SettingKeys.FPS_LIMIT)) {
                bool isFrameRate = int.TryParse(option.Id, out int frameRate) && frameRate > 0;
                Assert.That(isFrameRate || option.Id == _configuration.UnlimitedFrameRateOptionId, Is.True, option.Id);
            }
        }

        private System.Collections.Generic.IReadOnlyList<SettingChoiceOption> GetOptions(string key) =>
            ((ChoiceSettingDefinition)_model.GetDefinition(key)).Options;
    }
}
