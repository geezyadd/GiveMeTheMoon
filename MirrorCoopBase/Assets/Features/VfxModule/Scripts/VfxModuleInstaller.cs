using Zenject;

namespace Features.VfxModule {
    public sealed class VfxModuleInstaller : Installer<VfxModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<IVfxFactory>().To<VfxFactory>().AsSingle();
            Container.BindInterfacesTo<VfxPoolRegistry>().AsSingle();
            Container.Bind<IVfxService>().To<VfxService>().AsSingle();
            Container.BindInterfacesTo<VfxSystem>().AsSingle();
        }
    }
}
