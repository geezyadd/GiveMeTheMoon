using Features.GameCoreModule.Contracts;
using Features.NetworkModelModule.Scripts;
using Zenject;

namespace Game.Connection
{
    public sealed class ConnectionModuleInstaller : Installer<ConnectionModuleInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<ConnectionSessionService>().AsSingle();
            Container.BindInterfacesTo<SteamLobbyService>().AsSingle();
            Container.BindInterfacesTo<SteamLobbyJoinableSystem>().AsSingle();
            Container.Bind<IJoinRule>().To<JoinRule>().AsSingle();
            Container.BindInterfacesTo<ConnectionJoinPolicy>().AsSingle();
            Container.Bind<IConnectionRejectMessages>().To<ConnectionRejectMessages>().AsSingle();
            Container.Bind<IPlayerIdentityService>().To<ConnectionPlayerIdentityService>().AsSingle();
            Container.Bind<IPlayerNameSource>().To<ConnectionPlayerNameSource>().AsSingle();
            Container.Bind<IGameplaySession>().To<ConnectionGameplaySession>().AsSingle();
        }
    }
}
