using Features.SavingSystemModule.Contracts;
using Features.SettingsModule.Data;
using UnityEngine;
using Zenject;

namespace Features.SettingsModule.Saving {
    // At startup (GlobalScene, after Steam is up, before the appliers: see the execution order in
    // SettingsModuleInstaller) reads the save files and puts the saved settings into the live model.
    // It is the save service's first caller, so it runs LoadFiles; move that to a shared startup step when a second
    // module saves.
    public sealed class SettingsSaveSystem : ISettingsSaving, IInitializable {
        private readonly ISaveService _saveService;
        private readonly SettingsModel _settingsModel;
        private readonly SettingsSaveStore _store;

        public SettingsSaveSystem(ISaveService saveService, SettingsModel settingsModel) {
            _saveService = saveService;
            _settingsModel = settingsModel;
            _store = new SettingsSaveStore(saveService);
        }

        public void Initialize() {
            try {
                _saveService.LoadFiles();
            }
            catch (System.Exception exception) {
                Debug.LogWarning($"Settings: the save files could not be read, the defaults stay. {exception.Message}");
                return;
            }

            _store.Load(_settingsModel);
        }

        public void Save() =>
            _store.Save(_settingsModel);
    }
}
