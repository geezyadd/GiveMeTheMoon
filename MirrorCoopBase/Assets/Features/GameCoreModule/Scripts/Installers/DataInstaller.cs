using Features.CameraModule.Scripts.Models;
using Features.PlayerLifeModule.Scripts.Spectator;
using Features.CharacterMovableModule.Scripts.Models;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GrabModule.Scripts;
using Features.MvpModule;
using Features.PlayerLifeModule.Scripts.Generated;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Generated;
using Game.Connection;
using Zenject;

namespace Features.GameCoreModule.Scripts.Installers {
    public sealed class DataInstaller : Installer<DataInstaller> {
        public override void InstallBindings() {
            Container.Bind<GameFlowStateLifecycleEventClass>().AsSingle();
            Container.Bind<ConnectionSessionModel>().AsSingle();
            Container.Bind<SteamLobbyModel>().AsSingle();
            Container.Bind<PreloadedWindowsModel>().AsSingle();
            Container.Bind<CharacterMovableModel>().AsSingle();
            Container.Bind<PlayerControlBlockModel>().AsSingle();
            Container.Bind<GameCameraModel>().AsSingle();
            Container.Bind<ShipRunModel>().AsSingle();
            Container.Bind<HoveredInteractableModel>().AsSingle();
            Container.Bind<SpectatorModel>().AsSingle();
            WalletModelInstaller.Install(Container);
            PlayerLifeModelInstaller.Install(Container);
        }
    }
}
