#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Structures;
using Zenject;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // The field catalog and the difficulty profiles get the debug networked beacon, so every flight also spawns
    // Mirror field objects.
    public sealed class LoopSmokeFieldCatalogOverride : IDisposable {
        private readonly List<Object> _configurationCopies = new List<Object>();

        private FieldStructureCatalog _catalog;

        public LoopSmokeFieldCatalogOverride() =>
            ProjectContext.PostInstall += OnProjectContextPostInstall;

        public void Dispose() {
            ProjectContext.PostInstall -= OnProjectContextPostInstall;
            if (_catalog != null)
                Object.Destroy(_catalog);

            foreach (Object copy in _configurationCopies)
                Object.Destroy(copy);

            _configurationCopies.Clear();
        }

        private void OnProjectContextPostInstall() {
            DiContainer container = ProjectContext.Instance.Container;
            _catalog = LoopSmokeFieldCheck.CreateCatalogWithNetworkedStructure(container.Resolve<FieldStructureCatalog>());
            container.Unbind<FieldStructureCatalog>();
            container.Bind<FieldStructureCatalog>().FromInstance(_catalog);
            FlightFieldConfiguration configuration = LoopSmokeFieldCheck.CreateConfigurationWithNetworkedObstacle(container.Resolve<FlightFieldConfiguration>(), _configurationCopies);
            container.Unbind<FlightFieldConfiguration>();
            container.Bind<FlightFieldConfiguration>().FromInstance(configuration);
        }
    }
}
#endif
