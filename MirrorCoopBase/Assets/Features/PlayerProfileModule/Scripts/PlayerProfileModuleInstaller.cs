using Zenject;

namespace Features.PlayerProfileModule.Scripts {
    public sealed class PlayerProfileModuleInstaller : Installer<PlayerProfileModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesTo<PlayerNameSanitizer>().AsSingle();
            Container.BindInterfacesTo<PlayerNameplateLayout>().AsSingle();
        }
    }
}
