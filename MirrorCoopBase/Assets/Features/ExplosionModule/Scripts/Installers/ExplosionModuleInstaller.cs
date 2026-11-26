using Features.ExplosionModule.Network;
using Features.ExplosionModule.Wave;
using Zenject;

namespace Features.ExplosionModule.Installers {
    public sealed class ExplosionModuleInstaller : Installer<ExplosionModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesAndSelfTo<ExplosionReceiverRegistry>().AsSingle();
            Container.BindInterfacesAndSelfTo<ExplosionWaves>().AsSingle();
            Container.BindInterfacesTo<ExplosionService>().AsSingle();
            Container.BindInterfacesTo<ExplosionNetworkChannel>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesTo<DebugActions.ExplosionDebugSystem>().AsSingle();
#endif
        }
    }
}
