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
            Container.BindInterfacesTo<ShipRadarService>().AsSingle();
            Container.BindInterfacesTo<ShipStationPads>().AsSingle();
            Container.BindInterfacesTo<ShipStationDropService>().AsSingle();
            Container.BindInterfacesTo<ShipFlightStatService>().AsSingle();
            Container.BindInterfacesTo<ShipRoute>().AsSingle();
            Container.BindInterfacesTo<ShipWorldShiftService>().AsSingle();
            Container.BindInterfacesTo<ShipRunRiderService>().AsSingle();
            Container.BindInterfacesTo<ShipRunService>().AsSingle();
            Container.BindInterfacesTo<ShipRunJoinGate>().AsSingle();
            Container.BindInterfacesTo<ShipDeckJoinSpawn>().AsSingle();
            Container.BindInterfacesTo<ShipPositionSource>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesAndSelfTo<ShipFlightLengthDebugModel>().AsSingle();
            Container.BindInterfacesTo<Debug.DeckFeelHarness>().AsSingle();
            Container.Bind<Debug.ShipStatsDebugOverlay>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
#endif
        }
    }
}
