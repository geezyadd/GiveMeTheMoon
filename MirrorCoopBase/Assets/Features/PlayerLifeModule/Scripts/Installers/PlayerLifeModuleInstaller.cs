using Zenject;

namespace Features.PlayerLifeModule.Scripts.Installers {
    public sealed class PlayerLifeModuleInstaller : Installer<PlayerLifeModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesTo<PlayerBodyRegistry>().AsSingle();
            Container.BindInterfacesTo<PlayerDamageRule>().AsSingle();
            Container.BindInterfacesTo<FallKillRule>().AsSingle();
            Container.BindInterfacesTo<AllDeadRule>().AsSingle();
            Container.BindInterfacesTo<FallKillSystem>().AsSingle();
            Container.BindInterfacesTo<PlayerLifeSystem>().AsSingle();
            Container.BindInterfacesTo<PlayerLifeQuery>().AsSingle();
        }
    }
}
