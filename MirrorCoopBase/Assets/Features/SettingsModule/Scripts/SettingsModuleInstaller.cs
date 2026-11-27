using Features.SettingsModule.Video;
using Zenject;

namespace Features.SettingsModule {
    // The video settings' appliers; the settings model is bound in GameCore's DataInstaller, the catalog and the video
    // configuration in its ConfigurationInstaller.
    public sealed class SettingsModuleInstaller : Installer<SettingsModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesTo<DisplayModeSettingsApplier>().AsSingle();
            Container.BindInterfacesTo<FrameRateSettingsApplier>().AsSingle();
            Container.BindInterfacesTo<RenderPipelineSettingsApplier>().AsSingle();
        }
    }
}
