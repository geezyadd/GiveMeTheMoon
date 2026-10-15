#if UNITY_INCLUDE_TESTS
using System;
using Features.FlightFieldModule.Scripts.Structures;
using Zenject;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // The field catalog gets the debug networked beacon, so every flight also spawns Mirror field objects.
    public sealed class LoopSmokeFieldCatalogOverride : IDisposable {
        private FieldStructureCatalog _catalog;

        public LoopSmokeFieldCatalogOverride() =>
            ProjectContext.PostInstall += OnProjectContextPostInstall;

        public void Dispose() {
            ProjectContext.PostInstall -= OnProjectContextPostInstall;
            if (_catalog != null)
                Object.Destroy(_catalog);
        }

        private void OnProjectContextPostInstall() {
            DiContainer container = ProjectContext.Instance.Container;
            _catalog = LoopSmokeFieldCheck.CreateCatalogWithNetworkedStructure(container.Resolve<FieldStructureCatalog>());
            container.Unbind<FieldStructureCatalog>();
            container.Bind<FieldStructureCatalog>().FromInstance(_catalog);
        }
    }
}
#endif
