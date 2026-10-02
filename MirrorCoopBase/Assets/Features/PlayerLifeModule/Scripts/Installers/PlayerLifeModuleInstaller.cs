using Features.PlayerLifeModule.Scripts.Spectator;
using Zenject;

namespace Features.PlayerLifeModule.Scripts.Installers {
    public sealed class PlayerLifeModuleInstaller : Installer<PlayerLifeModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesTo<PlayerBodyRegistry>().AsSingle();
            Container.BindInterfacesTo<PlayerDamageRule>().AsSingle();
            Container.BindInterfacesTo<FallKillRule>().AsSingle();
            Container.BindInterfacesTo<AllDeadRule>().AsSingle();
            Container.BindInterfacesTo<PlayerLifeStateMachineFactory>().AsSingle();
            Container.BindInterfacesTo<FallKillSystem>().AsSingle();
            Container.BindInterfacesTo<PlayerLifeSystem>().AsSingle();
            Container.BindInterfacesTo<PlayerLifeQuery>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesTo<DebugLocalPlayerKillSystem>().AsSingle();
            // Fallback for a setup without the life states; HasBinding instead of IfNotBound: IfNotBound is checked per
            // contract and would also skip ITickable / IGameplaySession.
            if (Container.HasBinding<IPlayerLifeQuery>() == false)
                Container.BindInterfacesTo<DebugPlayerLifeQuery>().AsSingle();
#endif
            Container.Bind<ISpectatorTargetService>().To<SpectatorTargetService>().AsSingle();
            Container.BindInterfacesTo<SpectatorSystem>().AsSingle();
        }
    }
}
