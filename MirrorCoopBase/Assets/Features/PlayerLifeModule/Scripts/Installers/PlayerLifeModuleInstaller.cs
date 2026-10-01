using Features.PlayerLifeModule.Scripts.Spectator;
using Zenject;

namespace Features.PlayerLifeModule.Scripts.Installers {
    public sealed class PlayerLifeModuleInstaller : Installer<PlayerLifeModuleInstaller> {
        public override void InstallBindings() {
            // Debug stand-in until the life state machine binds IPlayerLifeQuery.
            // IfNotBound skips this when that binding is already registered above this line,
            // or by an installer that ran earlier. A later duplicate bind still throws.
            Container.Bind<IPlayerLifeQuery>()
                .To<DebugPlayerLifeQuery>()
                .AsSingle()
                .IfNotBound();
            Container.BindInterfacesTo<SpectatorSystem>().AsSingle();
        }
    }
}
