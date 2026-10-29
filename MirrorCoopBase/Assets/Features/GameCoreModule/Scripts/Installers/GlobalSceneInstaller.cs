using Features.CameraModule.Scripts;
using Features.CharacterMovableModule.Scripts;
using Features.CrewInviteModule.Scripts.Installers;
using Features.CutsceneModule.Scripts;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.DebugModule.Scripts.Installers;
#endif
using Features.BugModule.Scripts;
using Features.FieldAgentModule.Scripts;
using Features.FlightFieldModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.Installers;
using Features.GrabModule.Scripts;
using Features.InputModule.Realization.Scripts;
using Features.PlayerLifeModule.Scripts.Installers;
using Features.PlayerProfileModule.Scripts;
using Features.PointOfInterestModule.Scripts;
using Features.ShipDamageModule.Scripts;
using Features.ShipImpactModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Installers;
using Features.SonarModule.Scripts.Installers;
using Features.SpaceHazeModule.Scripts.Installers;
using Features.StagePresetModule.Scripts.Installers;
using Features.TooltipModule.Scripts.Installers;
using Features.WeaponModule.Scripts;
using Game.Connection;
using Zenject;

namespace Features.GameCoreModule.Scripts.Installers {
    public sealed class GlobalSceneInstaller : MonoInstaller<GlobalSceneInstaller> {
        public override void InstallBindings() {
            GameFlowStateMachineModuleInstaller.Install(Container);
            InputModuleInstaller.Install(Container);
            CharacterMovableModuleInstaller.Install(Container);
            CameraModuleInstaller.Install(Container);
            GrabModuleInstaller.Install(Container);
            ShipModuleInstaller.Install(Container);
            ShipImpactModuleInstaller.Install(Container);
            ShipDamageModuleInstaller.Install(Container);
            FlightFieldModuleInstaller.Install(Container);
            FieldAgentModuleInstaller.Install(Container);
            BugModuleInstaller.Install(Container);
            WeaponModuleInstaller.Install(Container);
            PlayerLifeModuleInstaller.Install(Container);
            PlayerProfileModuleInstaller.Install(Container);
            ShopModuleInstaller.Install(Container);
            SonarModuleInstaller.Install(Container);
            CutsceneModuleInstaller.Install(Container);
            PointOfInterestModuleInstaller.Install(Container);
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
            Container.BindInterfacesTo<FieldNetworkedPrefabSystem>().AsSingle();
        }
    }
}
