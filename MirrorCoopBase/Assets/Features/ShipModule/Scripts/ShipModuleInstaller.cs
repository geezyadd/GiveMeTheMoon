using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Factories;
using Zenject;

namespace Features.ShipModule.Scripts {
    public sealed class ShipModuleInstaller : Installer<ShipModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<IStatFactory<ShipStatType>>().To<ShipStatFactory>().AsSingle();
            Container.Bind<IStatEntityFactory<ShipStatType>>().To<ShipStatEntityFactory>().AsSingle();
            Container.BindInterfacesAndSelfTo<ShipRadarService>().AsSingle();
            Container.BindInterfacesAndSelfTo<ShipRunService>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesTo<Debug.DeckFeelHarness>().AsSingle();
#endif
        }
    }
}
