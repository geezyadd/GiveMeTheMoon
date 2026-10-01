using Zenject;

namespace Features.PlayerLifeModule.Scripts.Installers {
    public sealed class PlayerLifeModuleInstaller : Installer<PlayerLifeModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<PlayerDamageableRegistry>().AsSingle();
            Container.BindInterfacesTo<FallKillSystem>().AsSingle();
        }
    }
}
