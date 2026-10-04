using Features.GameFlowStateMachineModule.Scripts.States;
using Zenject;

namespace Features.GameFlowStateMachineModule.Scripts.Installers {
    public sealed class GameFlowStateMachineModuleInstaller : Installer<GameFlowStateMachineModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesAndSelfTo<GlobalGameFlowState>().AsSingle();
            Container.BindInterfacesAndSelfTo<MenuGameFlowState>().AsSingle();
            Container.BindInterfacesAndSelfTo<SessionGameFlowState>().AsSingle();
            Container.Bind<GameFlowStateSceneMapper>().AsSingle();
            Container.Bind<GameFlowSceneSwitcher>().AsSingle();
            Container.BindInterfacesTo<GameFlowStateMachineService>().AsSingle();
        }
    }
}
