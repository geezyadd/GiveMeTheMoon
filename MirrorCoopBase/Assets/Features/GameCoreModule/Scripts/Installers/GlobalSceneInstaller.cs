using Features.PlayerReplicaModule.Installers;
using Features.CameraModule.Scripts;
using Features.CharacterMovableModule.Scripts;
using Features.CoroutineRunnerModule;
using Features.LoadingScreenModule;
using Features.CrewInviteModule.Scripts.Installers;
using Features.EmoteModule;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.DebugModule.Scripts.Installers;
#endif
using Features.FlightFieldModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.Installers;
using Features.GrabModule.Scripts;
using Features.InputModule.Realization.Scripts;
using Features.InventoryModule.Scripts;
using Features.PlayerLifeModule.Scripts.Installers;
using Features.PlayerProfileModule.Scripts;
using Features.ShipDamageModule.Scripts;
using Features.ShipImpactModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Installers;
using Features.SonarModule.Scripts.Installers;
using Features.SpaceHazeModule.Scripts.Installers;
using Features.TargetModule.Installers;
using Features.ExplosionModule.Installers;
using Features.ScreenShakeModule;
using Features.AudioModule;
using Features.VfxModule;
using Features.StagePresetModule.Scripts.Installers;
using Features.TooltipModule.Scripts.Installers;
using Features.WeaponModule.Scripts;
using Game.Connection;
using Zenject;

namespace Features.GameCoreModule.Scripts.Installers {
    public sealed class GlobalSceneInstaller : MonoInstaller<GlobalSceneInstaller> {
        public override void InstallBindings() {
            CoroutineRunnerModuleInstaller.Install(Container);
            LoadingScreenModuleInstaller.Install(Container);
            GameFlowStateMachineModuleInstaller.Install(Container);
            InputModuleInstaller.Install(Container);
            CharacterMovableModuleInstaller.Install(Container);
            PlayerReplicaModuleInstaller.Install(Container);
            TargetModuleInstaller.Install(Container);
            VfxModuleInstaller.Install(Container);
            ScreenShakeModuleInstaller.Install(Container);
            AudioModuleInstaller.Install(Container);
            ExplosionModuleInstaller.Install(Container);
            CameraModuleInstaller.Install(Container);
            GrabModuleInstaller.Install(Container);
            InventoryModuleInstaller.Install(Container);
            ShipModuleInstaller.Install(Container);
            ShipImpactModuleInstaller.Install(Container);
            ShipDamageModuleInstaller.Install(Container);
            FlightFieldModuleInstaller.Install(Container);
            WeaponModuleInstaller.Install(Container);
            PlayerLifeModuleInstaller.Install(Container);
            PlayerProfileModuleInstaller.Install(Container);
            ShopModuleInstaller.Install(Container);
            SonarModuleInstaller.Install(Container);
            EmoteModuleInstaller.Install(Container);
            SpaceHazeModuleInstaller.Install(Container);
            StagePresetModuleInstaller.Install(Container);
            TooltipModuleInstaller.Install(Container);
            CrewInviteModuleInstaller.Install(Container);
            ConnectionModuleInstaller.Install(Container);
            WindowsModuleInstaller.Install(Container);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugModuleInstaller.Install(Container);
#endif
            Container.BindInterfacesTo<GameplaySessionLifecycle>().AsSingle();
            Container.BindInterfacesTo<SessionDroppedMenuSystem>().AsSingle();
            Container.BindInterfacesTo<AmbienceSystem>().AsSingle();
            Container.BindInterfacesTo<FieldNetworkedPrefabSystem>().AsSingle();
            Container.BindInterfacesTo<ExplosionNetworkChannelSystem>().AsSingle();
        }
    }
}
