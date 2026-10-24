#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using Features.SonarModule.Contracts;
using Features.SonarModule.Scripts.Configurations;
using NUnit.Framework;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // Binds a copy of SonarConfiguration whose station kind keeps a mark for a short time, so the loop test sees a
    // mark run out within one cruise; the assets are not changed.
    public sealed class LoopSmokeSonarConfigOverride : IDisposable {
        public const float STATION_MARK_SECONDS = 2f;

        private const string MARK_SECONDS_JSON_FORMAT = "{{\"_markSeconds\":{0}}}";
        private const string KIND_REFERENCE_JSON_FORMAT = "{{\"instanceID\":{0}}}";
        private const string KINDS_JSON_FORMAT = "{{\"_kinds\":[{0}],\"_stationKind\":{1}}}";
        private const string JSON_SEPARATOR = ",";

        private SonarConfiguration _configuration;
        private SonarContactKind _stationKind;

        public LoopSmokeSonarConfigOverride() {
            Assert.IsFalse(ProjectContext.HasInstance, "ProjectContext exists before the test: the sonar config override cannot be bound.");
            ProjectContext.PostInstall += OnProjectContextPostInstall;
        }

        public void Dispose() {
            ProjectContext.PostInstall -= OnProjectContextPostInstall;
            if (_configuration != null)
                Object.Destroy(_configuration);
            if (_stationKind != null)
                Object.Destroy(_stationKind);
        }

        private void OnProjectContextPostInstall() {
            DiContainer container = ProjectContext.Instance.Container;
            SonarConfiguration shipped = container.Resolve<SonarConfiguration>();
            _stationKind = Object.Instantiate(shipped.StationKind);
            JsonUtility.FromJsonOverwrite(string.Format(MARK_SECONDS_JSON_FORMAT, STATION_MARK_SECONDS.ToString(System.Globalization.CultureInfo.InvariantCulture)), _stationKind);
            _configuration = Object.Instantiate(shipped);
            JsonUtility.FromJsonOverwrite(CreateKindsJson(shipped), _configuration);
            container.Unbind<SonarConfiguration>();
            container.Bind<SonarConfiguration>().FromInstance(_configuration);
        }

        private string CreateKindsJson(SonarConfiguration shipped) {
            List<string> kinds = new List<string>();
            foreach (SonarContactKind kind in shipped.Kinds)
                kinds.Add(Reference(kind == shipped.StationKind ? _stationKind : kind));

            return string.Format(KINDS_JSON_FORMAT, string.Join(JSON_SEPARATOR, kinds), Reference(_stationKind));
        }

        private static string Reference(Object asset) =>
            string.Format(KIND_REFERENCE_JSON_FORMAT, asset.GetInstanceID());
    }
}
#endif
