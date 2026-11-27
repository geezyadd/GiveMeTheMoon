using System.Collections.Generic;
using System.IO;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.SavingSystemModule;
using Features.SettingsModule.Data;
using Features.SettingsModule.Saving;
using NUnit.Framework;

namespace Features.SettingsModule.Editor.Tests {
    // The settings go through the real save service (a temporary folder instead of the Steam one) and come back into
    // a fresh model; anything that does not fit keeps its default.
    public sealed class SettingsSaveStoreTests {
        private const string PASSPHRASE = "test passphrase";

        private SettingsCatalog _catalog;
        private string _folderPath;

        [OneTimeSetUp]
        public void OneTimeSetUp() =>
            _catalog = AddressableConfigurationLoader.Load<SettingsCatalog>(Address.Configurations.SettingsCatalog_Default);

        [SetUp]
        public void SetUp() =>
            _folderPath = Path.Combine(Path.GetTempPath(), "SettingsSaveStoreTests_" + System.Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown() {
            if (Directory.Exists(_folderPath))
                Directory.Delete(_folderPath, true);
        }

        [Test]
        public void WhenSettingsAreSaved_ThenAFreshServiceLoadsThemBack() {
            SettingsModel saved = new(_catalog);
            saved.SetToggle(SettingKeys.INVERT_Y, true);
            saved.SetSlider(SettingKeys.MOUSE_SENSITIVITY, 0.3f);
            saved.SetSlider(SettingKeys.SCREEN_SHAKE, 80f);
            saved.SetChoice(SettingKeys.SHADOW_QUALITY, "high");
            saved.SetChoice(SettingKeys.RESOLUTION, "1920x1080");
            new SettingsSaveStore(CreateService()).Save(saved);

            SaveService reloaded = CreateService();
            reloaded.LoadFiles();
            SettingsModel loaded = new(_catalog);
            new SettingsSaveStore(reloaded).Load(loaded);

            Assert.That(loaded.HasSameValues(saved), Is.True);
            Assert.That(loaded.GetSlider(SettingKeys.MOUSE_SENSITIVITY), Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(loaded.GetChoice(SettingKeys.RESOLUTION), Is.EqualTo("1920x1080"));
        }

        [Test]
        public void WhenThereIsNoSave_ThenTheDefaultsStay() {
            SaveService service = CreateService();
            service.LoadFiles();
            SettingsModel model = new(_catalog);

            new SettingsSaveStore(service).Load(model);

            Assert.That(model.HasSameValues(new SettingsModel(_catalog)), Is.True);
        }

        [Test]
        public void WhenASavedValueNoLongerFits_ThenOnlyThatSettingKeepsItsDefault() {
            SaveService service = CreateService();
            service.Save(SettingsSaveStore.SAVE_ID, new SettingsSaveData {
                Toggles = new Dictionary<string, bool> { { SettingKeys.NAMEPLATES, false }, { "removed.setting", true } },
                Sliders = new Dictionary<string, float> { { SettingKeys.INVERT_Y, 1f }, { SettingKeys.FIELD_OF_VIEW, 70f } },
                Choices = new Dictionary<string, string> { { SettingKeys.ANTI_ALIASING, "16x" } }
            }, SettingsSaveStore.SAVE_FILE);
            SettingsModel model = new(_catalog);

            new SettingsSaveStore(service).Load(model);

            Assert.That(model.GetToggle(SettingKeys.NAMEPLATES), Is.False);
            Assert.That(model.GetSlider(SettingKeys.FIELD_OF_VIEW), Is.EqualTo(70f));
            Assert.That(model.GetToggle(SettingKeys.INVERT_Y), Is.False);
            Assert.That(model.GetChoice(SettingKeys.ANTI_ALIASING), Is.EqualTo("4x"));
        }

        [Test]
        public void WhenTheSaveIsCorrupt_ThenTheDefaultsStay() {
            new SettingsSaveStore(CreateService()).Save(new SettingsModel(_catalog));
            foreach (string file in Directory.GetFiles(_folderPath))
                File.WriteAllBytes(file, new byte[] { 1, 2, 3, 4 });

            SaveService reloaded = CreateService();
            reloaded.LoadFiles();
            SettingsModel model = new(_catalog);
            new SettingsSaveStore(reloaded).Load(model);

            Assert.That(model.HasSameValues(new SettingsModel(_catalog)), Is.True);
        }

        private SaveService CreateService() =>
            new(new SaveDataStore(), new SaveFileStorage(new FixedSaveFolderPath(_folderPath), new SaveFileCipher(PASSPHRASE)));

        private sealed class FixedSaveFolderPath : ISaveFolderPath {
            private readonly string _folderPath;

            public FixedSaveFolderPath(string folderPath) =>
                _folderPath = folderPath;

            public string GetFolderPath() =>
                _folderPath;
        }
    }
}
