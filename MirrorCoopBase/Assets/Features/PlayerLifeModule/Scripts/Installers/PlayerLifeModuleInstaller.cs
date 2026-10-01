using Features.PlayerLifeModule.Scripts.Spectator;
using Zenject;

namespace Features.PlayerLifeModule.Scripts.Installers {
    public sealed class PlayerLifeModuleInstaller : Installer<PlayerLifeModuleInstaller> {
        public override void InstallBindings() {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Debug stand-in until the life states bind IPlayerLifeQuery; that binding must be installed before this installer.
            // HasBinding instead of IfNotBound: IfNotBound is checked per contract and would also skip ITickable / IGameplaySession.
            if (Container.HasBinding<IPlayerLifeQuery>() == false)
                Container.BindInterfacesTo<DebugPlayerLifeQuery>().AsSingle();
#endif
            Container.Bind<ISpectatorTargetService>().To<SpectatorTargetService>().AsSingle();
            Container.BindInterfacesTo<SpectatorSystem>().AsSingle();
        }
    }
}
