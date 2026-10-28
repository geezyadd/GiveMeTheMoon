#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Globalization;
using Features.FlightFieldModule.Scripts.Configuration;
using NUnit.Framework;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // Binds a copy of FieldOreCatalog whose kinds live a short time, so the loop test sees ore nobody picked up go away
    // within one cruise; the assets are not changed.
    public sealed class LoopSmokeOreConfigOverride : IDisposable {
        public const float ORE_LIFETIME_SECONDS = 8f;

        private const string LIFETIME_JSON_FORMAT = "{{\"_lifetimeSeconds\":{0}}}";
        private const string ORE_REFERENCE_JSON_FORMAT = "{{\"instanceID\":{0}}}";
        private const string ORES_JSON_FORMAT = "{{\"_ores\":[{0}]}}";
        private const string JSON_SEPARATOR = ",";

        private readonly List<Object> _copies = new List<Object>();

        public LoopSmokeOreConfigOverride() {
            Assert.IsFalse(ProjectContext.HasInstance, "ProjectContext exists before the test: the ore config override cannot be bound.");
            ProjectContext.PostInstall += OnProjectContextPostInstall;
        }

        public void Dispose() {
            ProjectContext.PostInstall -= OnProjectContextPostInstall;
            foreach (Object copy in _copies)
                Object.Destroy(copy);
        }

        private void OnProjectContextPostInstall() {
            DiContainer container = ProjectContext.Instance.Container;
            FieldOreCatalog shipped = container.Resolve<FieldOreCatalog>();
            List<string> ores = new List<string>();
            string lifetime = string.Format(LIFETIME_JSON_FORMAT, ORE_LIFETIME_SECONDS.ToString(CultureInfo.InvariantCulture));
            foreach (FieldOreDefinition ore in shipped.Ores) {
                FieldOreDefinition copy = Object.Instantiate(ore);
                JsonUtility.FromJsonOverwrite(lifetime, copy);
                _copies.Add(copy);
                ores.Add(string.Format(ORE_REFERENCE_JSON_FORMAT, copy.GetInstanceID()));
            }

            FieldOreCatalog catalog = Object.Instantiate(shipped);
            JsonUtility.FromJsonOverwrite(string.Format(ORES_JSON_FORMAT, string.Join(JSON_SEPARATOR, ores)), catalog);
            _copies.Add(catalog);
            container.Unbind<FieldOreCatalog>();
            container.Bind<FieldOreCatalog>().FromInstance(catalog);
        }
    }
}
#endif
