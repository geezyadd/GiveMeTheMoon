using Features.CameraModule.Scripts.Models;
using Features.CameraModule.Scripts.Shake;
using Features.PlayerLifeModule.Scripts.Spectator;
using Features.CharacterMovableModule.Scripts.Models;
using Features.CrewInviteModule.Data.Generated;
using Features.FlightFieldModule.Scripts.Decor;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Server;
using Features.FlightFieldModule.Scripts.Targets;
using Features.GrabModule.Scripts;
using Features.GrabModule.Scripts.Generated;
using Features.GrabModule.Scripts.Hold;
using Features.MvpModule;
using Features.PlayerLifeModule.Scripts.Generated;
using Features.PlayerProfileModule.Data.Generated;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Data;
using Features.ShipModule.Scripts.Generated;
using Features.ShopModule.Scripts.Generated;
using Features.StagePresetModule.Data;
using Features.StagePresetModule.Data.Generated;
using Game.Connection;
using Zenject;

namespace Features.GameCoreModule.Scripts.Installers {
    public sealed class DataInstaller : Installer<DataInstaller> {
        public override void InstallBindings() {
            Container.Bind<ConnectionSessionModel>().AsSingle();
            Container.Bind<ConnectionSpawnModel>().AsSingle();
            Container.Bind<ConnectionNetworkEvents>().AsSingle();
            Container.Bind<SteamLobbyModel>().AsSingle();
            Container.Bind<ConnectionRejectModel>().AsSingle();
            Container.Bind<PreloadedWindowsModel>().AsSingle();
            Container.Bind<CharacterMovableModel>().AsSingle();
            Container.Bind<PlayerControlBlockModel>().AsSingle();
            Container.Bind<GameCameraModel>().AsSingle();
            Container.Bind<CameraShakeModel>().AsSingle();
            Container.Bind<IShipRunBindingModel>().To<ShipRunBindingModel>().AsSingle();
            Container.Bind<IShipClientBindingModel>().To<ShipClientBindingModel>().AsSingle();
            Container.Bind<HoveredInteractableModel>().AsSingle();
            Container.Bind<HoldUseModel>().AsSingle();
            Container.Bind<SpectatorModel>().AsSingle();
            Container.Bind<ShopVisitModel>().AsSingle();
            Container.Bind<DeadCrewModel>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.Bind<Features.ShipModule.Scripts.Debug.WeaponDebugTargetModel>().AsSingle();
#endif
            WalletModelInstaller.Install(Container);
            PlayerLifeModelInstaller.Install(Container);
            PlayerHandModelInstaller.Install(Container);
            PlayerProfileModelInstaller.Install(Container);
            ShipRunModelInstaller.Install(Container);
            ShipSocketsModelInstaller.Install(Container);
            ShipConditionModelInstaller.Install(Container);
            ShipStatsModelInstaller.Install(Container);
            CrewRosterModelInstaller.Install(Container);
            RunStagePresetModelInstaller.Install(Container);
            Container.BindInterfacesAndSelfTo<StagePresetSelectionModel>().AsSingle();
            LiveFieldModelInstaller.Install(Container);
            Container.Bind<IFlightFieldSyncBindingModel>().To<FlightFieldSyncBindingModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<FlightFieldServerModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<FlightFieldSceneModel>().AsSingle();
            Container.Bind<FieldRockDamageModel>().AsSingle();
            Container.Bind<FieldTargetDamageModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<FieldTargetSceneModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<FieldTargetCountScaleModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<CrewWalletBindingModel>().AsSingle();
            Container.Bind<FieldDecorSceneModel>().AsSingle();
        }
    }
}
