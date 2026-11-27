using Features.SettingsModule.Saving;
using Features.SettingsModule.UI;
using Features.SettingsModule.Video;
using Zenject;

namespace Features.SettingsModule {
    // The video settings' appliers and the settings window's service; the models are bound in GameCore's
    // DataInstaller, the configurations in its ConfigurationInstaller, the window in its WindowsModuleInstaller.
    public sealed class SettingsModuleInstaller : Installer<SettingsModuleInstaller> {
        // The saved settings are loaded before the appliers apply the startup values.
        private const int LOAD_SAVED_SETTINGS_ORDER = -100;

        public override void InstallBindings() {
            Container.BindInterfacesTo<SettingsSaveSystem>().AsSingle();
            Container.BindExecutionOrder<SettingsSaveSystem>(LOAD_SAVED_SETTINGS_ORDER);
            Container.BindInterfacesTo<DisplayModeSettingsApplier>().AsSingle();
            Container.BindInterfacesTo<FrameRateSettingsApplier>().AsSingle();
            Container.BindInterfacesTo<RenderPipelineSettingsApplier>().AsSingle();
            Container.Bind<ISettingsWindowService>().To<SettingsWindowService>().AsSingle();
            Container.BindInterfacesTo<SettingsWindowEscapeSystem>().AsSingle();
        }
    }
}
