using Zenject;

namespace Features.ScreenShakeModule {
    // The model is bound in GameCore's DataInstaller, like every model.
    public sealed class ScreenShakeModuleInstaller : Installer<ScreenShakeModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<IScreenShakeService>().To<ScreenShakeService>().AsSingle();
            Container.BindInterfacesTo<ScreenShakeSystem>().AsSingle();
        }
    }
}
