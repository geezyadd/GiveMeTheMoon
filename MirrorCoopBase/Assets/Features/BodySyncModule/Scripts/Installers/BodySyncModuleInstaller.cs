using Zenject;

namespace Features.BodySyncModule.Installers {
    public sealed class BodySyncModuleInstaller : Installer<BodySyncModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<BodyStateChannelRegistry>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesTo<Diagnostics.BodySyncDebugSystem>().AsSingle();
            Container.BindInterfacesTo<Diagnostics.BodySyncModeDebugSystem>().AsSingle();
#endif
        }
    }
}
