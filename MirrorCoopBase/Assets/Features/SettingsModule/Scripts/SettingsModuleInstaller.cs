using Features.SettingsModule.UI;
using Features.SettingsModule.Video;
using Zenject;

namespace Features.SettingsModule {
    // The video settings' appliers and the settings window's service; the models are bound in GameCore's
    // DataInstaller, the configurations in its ConfigurationInstaller, the window in its WindowsModuleInstaller.
    public sealed class SettingsModuleInstaller : Installer<SettingsModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesTo<DisplayModeSettingsApplier>().AsSingle();
            Container.BindInterfacesTo<FrameRateSettingsApplier>().AsSingle();
            Container.BindInterfacesTo<RenderPipelineSettingsApplier>().AsSingle();
            Container.Bind<ISettingsWindowService>().To<SettingsWindowService>().AsSingle();
            Container.BindInterfacesTo<SettingsWindowEscapeSystem>().AsSingle();
        }
    }
}
