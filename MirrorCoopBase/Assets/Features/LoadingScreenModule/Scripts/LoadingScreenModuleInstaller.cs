using Zenject;

namespace Features.LoadingScreenModule {
    public sealed class LoadingScreenModuleInstaller : Installer<LoadingScreenModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<LoadingScreenView>().FromComponentInHierarchy().AsSingle();
            Container.BindInterfacesTo<LoadingScreenService>().AsSingle();
        }
    }
}
