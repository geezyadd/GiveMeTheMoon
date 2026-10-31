using Features.ShipModule.Scripts.Hull;
using Features.ShipModule.Scripts.Overboard;
using Features.ShipModule.Scripts.Throttle;
using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Factories;
using Zenject;

namespace Features.ShipModule.Scripts {
    public sealed class ShipModuleInstaller : Installer<ShipModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<IStatFactory<ShipStatType>>().To<ShipStatFactory>().AsSingle();
            Container.Bind<IStatEntityFactory<ShipStatType>>().To<ShipStatEntityFactory>().AsSingle();
            Container.BindInterfacesTo<ComponentRegistry<ShipDeckSpawnPoint>>().AsSingle();
            Container.BindInterfacesTo<ShipDeckSpawnPointRegistry>().AsSingle();
            Container.BindInterfacesTo<ShipStatsConfigurationValidator>().AsSingle();
            Container.BindInterfacesTo<ModuleCatalogValidator>().AsSingle();
            Container.BindInterfacesTo<ShipRunConfigValidator>().AsSingle();
            Container.BindInterfacesTo<ShipHullConfigurationValidator>().AsSingle();
            Container.BindInterfacesTo<ShipThrottleConfigurationValidator>().AsSingle();
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
            Container.BindInterfacesTo<ShipConditionWriter>().AsSingle();
            Container.BindInterfacesTo<ShipModuleWorkingState>().AsSingle();
            Container.BindInterfacesTo<ShipRunRiderService>().AsSingle();
            Container.BindInterfacesTo<ShipRunService>().AsSingle();
            Container.BindInterfacesTo<ShipRunJoinGate>().AsSingle();
            Container.BindInterfacesTo<ShipDeckJoinSpawn>().AsSingle();
            Container.BindInterfacesTo<ShipPositionSource>().AsSingle();
            Container.Bind<IShipThrottleDetentRule>().To<ShipThrottleDetentRule>().AsSingle();
            Container.Bind<IShipThrottleLaunchRule>().To<ShipThrottleLaunchRule>().AsSingle();
            Container.Bind<IShipThrottleStop>().To<ShipThrottleStop>().AsSingle();
            InstallOverboard();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesTo<Debug.DeckFeelHarness>().AsSingle();
            Container.Bind<Debug.IShipDebugActionConditions>().To<Debug.ShipDebugActionConditions>().AsSingle();
            Container.BindInterfacesTo<Debug.ShipHullDebugSystem>().AsSingle();
            Container.BindInterfacesTo<Debug.ShipThrottleDebugSystem>().AsSingle();
            Container.BindInterfacesTo<Debug.WeaponDebugTargetSystem>().AsSingle();
            Container.BindInterfacesTo<Debug.WeaponDebugTargetPrefabSystem>().AsSingle();
            Container.Bind<Debug.IShipOverboardDebugPlacement>().To<Debug.ShipOverboardDebugPlacement>().AsSingle();
            Container.BindInterfacesTo<Debug.ShipOverboardDebugSystem>().AsSingle();
            Container.Bind<Debug.ShipStatsDebugOverlay>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
#endif
        }

        // A player who leaves the ship in flight drifts until the ship or a rope takes it back, or the void takes it.
        private void InstallOverboard() {
            Container.BindInterfacesTo<ComponentRegistry<ShipOverboardRider>>().AsSingle();
            Container.Bind<IShipOverboardRule>().To<ShipOverboardRule>().AsSingle();
            Container.Bind<IShipOverboardWeaponIds>().To<ShipOverboardWeaponIds>().AsSingle();
            Container.Bind<IShipOverboardQuery>().To<ShipOverboardQuery>().AsSingle();
            Container.BindInterfacesTo<ShipOverboardEventBus>().AsSingle();
            Container.BindInterfacesTo<ShipOverboardSystem>().AsSingle();
            Container.BindInterfacesTo<ShipOverboardWeaponTargetSystem>().AsSingle();
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
