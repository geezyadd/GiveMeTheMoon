#if UNITY_INCLUDE_TESTS
using System;
using Features.ShipModule.Scripts;
using NUnit.Framework;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // Binds a copy of ShipRunConfig with test values when the ProjectContext is installed; the asset is not changed.
    public sealed class LoopSmokeShipRunConfigOverride : IDisposable {
        // Only timings are shortened; distances, heights and pads stay as shipped.
        public const string FAST_TIMINGS_JSON =
            "{\"_takeoffSeconds\":1.5,\"_cruiseSeconds\":2,\"_landingSeconds\":1.5,\"_perLoopCruiseSeconds\":0}";

        private readonly string[] _overridesJson;
        private ShipRunConfig _config;

        public ShipRunConfig Config =>
            _config;

        public LoopSmokeShipRunConfigOverride(params string[] overridesJson) {
            Assert.IsFalse(ProjectContext.HasInstance, "ProjectContext exists before the test: the config override cannot be bound.");
            _overridesJson = overridesJson;
            ProjectContext.PostInstall += OnProjectContextPostInstall;
        }

        public void Dispose() {
            ProjectContext.PostInstall -= OnProjectContextPostInstall;
            if (_config != null)
                Object.Destroy(_config);
        }

        public void AssertBound() =>
            Assert.AreSame(_config, ProjectContext.Instance.Container.Resolve<ShipRunConfig>(), "The test ShipRunConfig override is not bound.");

        private void OnProjectContextPostInstall() {
            DiContainer container = ProjectContext.Instance.Container;
            _config = Object.Instantiate(container.Resolve<ShipRunConfig>());
            foreach (string json in _overridesJson)
                JsonUtility.FromJsonOverwrite(json, _config);

            container.Unbind<ShipRunConfig>();
            container.Bind<ShipRunConfig>().FromInstance(_config);
        }
    }
}
#endif
