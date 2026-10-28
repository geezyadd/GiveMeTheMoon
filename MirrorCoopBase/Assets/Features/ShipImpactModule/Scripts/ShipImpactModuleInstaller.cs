using Features.ShipModule.Scripts;
using Zenject;

namespace Features.ShipImpactModule.Scripts {
    public sealed class ShipImpactModuleInstaller : Installer<ShipImpactModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesTo<ShipRockImpactConfigurationValidator>().AsSingle();
            Container.BindInterfacesTo<ShipRockImpactClassifier>().AsSingle();
            Container.BindInterfacesTo<ShipRockDamageRule>().AsSingle();
            Container.BindInterfacesTo<ShipRockKnockbackRule>().AsSingle();
            Container.BindInterfacesTo<ShipRockContactRule>().AsSingle();
            Container.BindInterfacesTo<ShipHullRockScanner>().AsSingle();
            Container.BindInterfacesTo<ShipHullStepTracker>().AsSingle();
            Container.BindInterfacesTo<ComponentRegistry<ShipImpactSync>>().AsSingle();
            Container.BindInterfacesTo<ShipRockCollisionSystem>().AsSingle();
            Container.BindInterfacesTo<ShipOreCatchSystem>().AsSingle();
            Container.BindInterfacesTo<ShipImpactEventBus>().AsSingle();
            Container.BindInterfacesTo<ShipServerImpactEventBus>().AsSingle();
            Container.BindInterfacesTo<ShipRockImpactFeelSource>().AsSingle();
            Container.BindInterfacesTo<ShipImpactEffectPool>().AsSingle();
            Container.BindInterfacesTo<ShipImpactEffectSystem>().AsSingle();
            Container.BindInterfacesTo<ShipImpactCameraShakeSystem>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesTo<Debug.ShipRockImpactDebugSystem>().AsSingle();
            Container.BindInterfacesTo<Debug.ShipImpactVolumeDebugDrawer>().AsSingle();
            Container.BindInterfacesTo<Debug.ShipImpactVolumeDebugSystem>().AsSingle();
#endif
        }
    }
}
