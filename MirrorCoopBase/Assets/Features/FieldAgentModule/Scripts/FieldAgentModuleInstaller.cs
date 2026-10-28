using Features.FieldAgentModule.Scripts.Cargo;
using Features.FieldAgentModule.Scripts.Client;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Deck;
using Features.FieldAgentModule.Scripts.Network;
using Features.FieldAgentModule.Scripts.Server;
using Features.FieldAgentModule.Scripts.Spawn;
using Features.FieldAgentModule.Scripts.Steering;
using Features.ShipModule.Scripts;
using Zenject;

namespace Features.FieldAgentModule.Scripts {
    public sealed class FieldAgentModuleInstaller : Installer<FieldAgentModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesTo<FieldAgentConfigurationValidator>().AsSingle();
            Container.BindInterfacesTo<ComponentRegistry<FieldAgentSync>>().AsSingle();
            Container.Bind<IFieldAgentSteering>().To<FieldAgentSteering>().AsSingle();
            Container.Bind<IFieldAgentRockAvoidance>().To<FieldAgentRockAvoidance>().AsSingle();
            Container.Bind<IFieldAgentSeparation>().To<FieldAgentSeparation>().AsSingle();
            Container.Bind<IFieldAgentWander>().To<FieldAgentWander>().AsSingle();
            Container.Bind<IFieldAgentDeckNavigation>().To<FieldAgentDeckNavigation>().AsSingle();
            Container.Bind<IFieldAgentTransitionService>().To<FieldAgentTransitionService>().AsSingle();
            Container.Bind<IFieldAgentFlightStepService>().To<FieldAgentFlightStepService>().AsSingle();
            Container.Bind<IFieldAgentDeckStepService>().To<FieldAgentDeckStepService>().AsSingle();
            Container.Bind<IFieldAgentBatchPublishService>().To<FieldAgentBatchPublishService>().AsSingle();
            Container.Bind<IFieldAgentControl>().To<FieldAgentControlService>().AsSingle();
            Container.Bind<IFieldAgentHiddenSpawnService>().To<FieldAgentHiddenSpawnService>().AsSingle();
            Container.Bind<IFieldAgentViewpointSource>().To<FieldAgentPlayerViewpointSource>().AsSingle();
            Container.Bind<IFieldAgentGroupSpawnService>().To<FieldAgentGroupSpawnService>().AsSingle();
            Container.BindInterfacesTo<FieldAgentCargoTargetRegistry>().AsSingle();
            Container.BindInterfacesTo<FieldAgentDeckNavMeshSystem>().AsSingle();
            Container.BindInterfacesTo<FieldAgentSimulationSystem>().AsSingle();
            Container.BindInterfacesTo<FieldAgentWorldShiftSystem>().AsSingle();
            Container.BindInterfacesTo<FieldAgentViewPool>().AsSingle();
            Container.BindInterfacesTo<FieldAgentClientSystem>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesTo<Debug.FieldAgentFlockDebugSystem>().AsSingle();
            Container.BindInterfacesTo<Debug.FieldAgentDeckTripDebugSystem>().AsSingle();
#endif
        }
    }
}
