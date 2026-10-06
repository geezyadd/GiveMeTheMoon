using Features.CameraModule.Scripts.Models;
using Features.PlayerLifeModule.Scripts.Spectator;
using Features.CharacterMovableModule.Scripts.Models;
using Features.GrabModule.Scripts;
using Features.MvpModule;
using Features.PlayerLifeModule.Scripts.Generated;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.ShopModule.Scripts.Generated;
using Game.Connection;
using Zenject;

namespace Features.GameCoreModule.Scripts.Installers {
    public sealed class DataInstaller : Installer<DataInstaller> {
        public override void InstallBindings() {
            Container.Bind<ConnectionSessionModel>().AsSingle();
            Container.Bind<ConnectionSpawnModel>().AsSingle();
            Container.Bind<ConnectionNetworkEvents>().AsSingle();
            Container.Bind<SteamLobbyModel>().AsSingle();
            Container.Bind<PreloadedWindowsModel>().AsSingle();
            Container.Bind<CharacterMovableModel>().AsSingle();
            Container.Bind<PlayerControlBlockModel>().AsSingle();
            Container.Bind<GameCameraModel>().AsSingle();
            Container.Bind<IShipRunBindingModel>().To<ShipRunBindingModel>().AsSingle();
            Container.Bind<HoveredInteractableModel>().AsSingle();
            Container.Bind<SpectatorModel>().AsSingle();
            WalletModelInstaller.Install(Container);
            PlayerLifeModelInstaller.Install(Container);
            ShipRunModelInstaller.Install(Container);
            ShipSocketsModelInstaller.Install(Container);
            ShipStatsModelInstaller.Install(Container);
        }
    }
}
