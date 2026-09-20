using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts {
    public sealed class ShipModuleInstaller : Installer<ShipModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<ItemViewCatalog>().FromMethod(LoadCatalog).AsSingle();
            Container.Bind<EngineCatalog>().FromMethod(LoadEngines).AsSingle();
            Container.Bind<ShipStationCatalog>().FromMethod(LoadStations).AsSingle();
            Container.Bind<ShipRunService>().AsSingle();
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
    }
}
