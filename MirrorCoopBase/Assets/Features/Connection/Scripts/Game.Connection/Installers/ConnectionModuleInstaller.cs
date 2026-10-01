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
            Container.Bind<IPlayerIdentityService>().To<ConnectionPlayerIdentityService>().AsSingle();
        }
    }
}
