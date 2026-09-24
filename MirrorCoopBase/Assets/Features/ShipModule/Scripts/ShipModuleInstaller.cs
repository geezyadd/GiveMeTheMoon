using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Factories;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts {
    public sealed class ShipModuleInstaller : Installer<ShipModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<ItemViewCatalog>().FromMethod(LoadCatalog).AsSingle();
            Container.Bind<EngineCatalog>().FromMethod(LoadEngines).AsSingle();
            Container.Bind<ShipStationCatalog>().FromMethod(LoadStations).AsSingle();
            Container.Bind<ShipRadarCatalog>().FromMethod(LoadRadar).AsSingle();
            Container.Bind<IStatFactory<ShipStatType>>().To<ShipStatFactory>().AsSingle();
            Container.Bind<IStatEntityFactory<ShipStatType>>().To<ShipStatEntityFactory>().AsSingle();
            Container.BindInterfacesAndSelfTo<ShipRadarService>().AsSingle();
            Container.BindInterfacesAndSelfTo<ShipRunService>().AsSingle();
        }

        private static ItemViewCatalog LoadCatalog() {
            ItemViewCatalog catalog = Resources.Load<ItemViewCatalog>(ItemViewCatalog.ResourceName);
            return catalog != null ? catalog : ScriptableObject.CreateInstance<ItemViewCatalog>();
        }

        private static EngineCatalog LoadEngines() {
            EngineCatalog catalog = Resources.Load<EngineCatalog>(EngineCatalog.ResourceName);
            return catalog != null ? catalog : ScriptableObject.CreateInstance<EngineCatalog>();
        }

        private static ShipStationCatalog LoadStations() {
            ShipStationCatalog catalog = Resources.Load<ShipStationCatalog>(ShipStationCatalog.ResourceName);
            return catalog != null ? catalog : ScriptableObject.CreateInstance<ShipStationCatalog>();
        }

        private static ShipRadarCatalog LoadRadar() {
            ShipRadarCatalog catalog = Resources.Load<ShipRadarCatalog>(ShipRadarCatalog.ResourceName);
            return catalog != null ? catalog : ScriptableObject.CreateInstance<ShipRadarCatalog>();
        }
    }
}
