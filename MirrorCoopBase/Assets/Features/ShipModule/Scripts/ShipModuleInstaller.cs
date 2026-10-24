using Features.ShipModule.Scripts.Damage;
using Features.ShipModule.Scripts.Hull;
using Features.ShipModule.Scripts.Impacts;
using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Factories;
using Zenject;

namespace Features.ShipModule.Scripts {
    public sealed class ShipModuleInstaller : Installer<ShipModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<IStatFactory<ShipStatType>>().To<ShipStatFactory>().AsSingle();
            Container.Bind<IStatEntityFactory<ShipStatType>>().To<ShipStatEntityFactory>().AsSingle();
            Container.BindInterfacesTo<ShipDeckSpawnPointRegistry>().AsSingle();
            Container.BindInterfacesTo<ShipStatsConfigurationValidator>().AsSingle();
            Container.BindInterfacesTo<ModuleCatalogValidator>().AsSingle();
            Container.BindInterfacesTo<ShipRunConfigValidator>().AsSingle();
            Container.BindInterfacesTo<ShipHullConfigurationValidator>().AsSingle();
            Container.BindInterfacesTo<ShipRadarService>().AsSingle();
            Container.BindInterfacesTo<ShipStationPads>().AsSingle();
            Container.BindInterfacesTo<ShipStationDropService>().AsSingle();
            InstallFlightLength();
            Container.BindInterfacesTo<ShipFlightStatService>().AsSingle();
            Container.BindInterfacesTo<ShipRoute>().AsSingle();
            Container.BindInterfacesTo<ShipWorldShiftService>().AsSingle();
            Container.BindInterfacesTo<ShipHullStatSource>().AsSingle();
            Container.BindInterfacesTo<ShipHullDamageRule>().AsSingle();
            Container.BindInterfacesTo<ShipHullEventBus>().AsSingle();
            Container.BindInterfacesTo<ShipHullService>().AsSingle();
            InstallRockImpacts();
            InstallDamage();
            Container.BindInterfacesTo<ShipRunRiderService>().AsSingle();
            Container.BindInterfacesTo<ShipRunService>().AsSingle();
            Container.BindInterfacesTo<ShipRunJoinGate>().AsSingle();
            Container.BindInterfacesTo<ShipDeckJoinSpawn>().AsSingle();
            Container.BindInterfacesTo<ShipPositionSource>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesTo<Debug.DeckFeelHarness>().AsSingle();
            Container.BindInterfacesTo<Debug.ShipHullDebugSystem>().AsSingle();
            Container.BindInterfacesTo<Debug.ShipRockImpactDebugSystem>().AsSingle();
            Container.BindInterfacesTo<Debug.ShipDamageDebugSystem>().AsSingle();
            Container.BindInterfacesTo<Debug.WeaponDebugTargetSystem>().AsSingle();
            Container.BindInterfacesTo<Debug.WeaponDebugTargetPrefabSystem>().AsSingle();
            Container.Bind<Debug.ShipStatsDebugOverlay>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
#endif
        }

        private void InstallRockImpacts() {
            Container.BindInterfacesTo<ShipRockImpactConfigurationValidator>().AsSingle();
            Container.BindInterfacesTo<ShipRockImpactClassifier>().AsSingle();
            Container.BindInterfacesTo<ShipRockDamageRule>().AsSingle();
            Container.BindInterfacesTo<ShipRockKnockbackRule>().AsSingle();
            Container.BindInterfacesTo<ShipHullRockScanner>().AsSingle();
            Container.BindInterfacesTo<ShipHullStepTracker>().AsSingle();
            Container.BindInterfacesTo<ShipRockCollisionSystem>().AsSingle();
            Container.BindInterfacesTo<ShipFieldTargetCollisionSystem>().AsSingle();
            Container.BindInterfacesTo<ShipImpactEventBus>().AsSingle();
            Container.BindInterfacesTo<ShipServerImpactEventBus>().AsSingle();
            Container.BindInterfacesTo<ShipRockImpactFeelSource>().AsSingle();
            Container.BindInterfacesTo<ShipImpactEffectPool>().AsSingle();
            Container.BindInterfacesTo<ShipImpactEffectSystem>().AsSingle();
            Container.BindInterfacesTo<ShipImpactCameraShakeSystem>().AsSingle();
        }

        private void InstallDamage() {
            Container.BindInterfacesTo<ShipDamageConfigurationValidator>().AsSingle();
            Container.BindInterfacesTo<ShipModuleCondition>().AsSingle();
            Container.BindInterfacesTo<ShipBreachCondition>().AsSingle();
            Container.BindInterfacesTo<ShipDamageEventBus>().AsSingle();
            Container.BindInterfacesTo<ShipDamageRule>().AsSingle();
            Container.BindInterfacesTo<ShipDamageRandom>().AsSingle();
            Container.BindInterfacesTo<ShipConditionService>().AsSingle();
            Container.BindInterfacesTo<ShipImpactDamageService>().AsSingle();
            Container.BindInterfacesTo<ShipLandingRepair>().AsSingle();
            Container.BindInterfacesTo<ShipImpactDamageSystem>().AsSingle();
            Container.BindInterfacesTo<ShipHullLeakRule>().AsSingle();
            Container.BindInterfacesTo<ShipHullLeakSystem>().AsSingle();
        }

        // Editor / development builds decorate the config's flight length with the debug length the harness sets.
        private void InstallFlightLength() {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesAndSelfTo<Debug.ShipFlightLengthDebugModel>().AsSingle();
            Container.Bind<IShipFlightLengthSource>().To<ShipRunConfigFlightLengthSource>().AsSingle().WhenInjectedInto<Debug.ShipDebugFlightLengthSource>();
            Container.Bind<IShipFlightLengthSource>().To<Debug.ShipDebugFlightLengthSource>().AsSingle().WhenNotInjectedInto<Debug.ShipDebugFlightLengthSource>();
#else
            Container.Bind<IShipFlightLengthSource>().To<ShipRunConfigFlightLengthSource>().AsSingle();
#endif
        }
    }
}
