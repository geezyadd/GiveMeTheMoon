using System.Collections.Generic;
using Features.CameraModule.Scripts.Services;
using Features.SonarModule.Contracts;
using Features.SonarModule.Data;
using Features.SonarModule.Data.Generated;
using Features.SonarModule.Scripts.Core;
using Features.SonarModule.Scripts.Sources;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.SonarModule.Scripts.Systems {
    // Every peer: lays the crew's synced marks, every other player drifting overboard and this peer's own station
    // marker out on its screen once per frame, after the camera moved. A mark that ran out hides at once here; the server takes it out of the model a moment
    // later.
    public sealed class SonarMarkerSystem : ILateTickable {
        private readonly IReadOnlySonarConsoleModel _sonarConsoleModel;
        private readonly SonarMarkerScreenModel _sonarMarkerScreenModel;
        private readonly IGameCameraService _gameCameraService;
        private readonly ISonarContactService _sonarContactService;
        private readonly ISonarMarkRule _sonarMarkRule;
        private readonly ISonarMarkerProjection _sonarMarkerProjection;
        private readonly ISonarStationMarkerSource _sonarStationMarkerSource;
        private readonly IOverboardSonarContactSource _overboardSonarContactSource;
        private readonly List<Vector3> _drifters = new List<Vector3>();

        private bool _hasShownMarkers;

        public SonarMarkerSystem(
            IReadOnlySonarConsoleModel sonarConsoleModel,
            SonarMarkerScreenModel sonarMarkerScreenModel,
            IGameCameraService gameCameraService,
            ISonarContactService sonarContactService,
            ISonarMarkRule sonarMarkRule,
            ISonarMarkerProjection sonarMarkerProjection,
            ISonarStationMarkerSource sonarStationMarkerSource,
            IOverboardSonarContactSource overboardSonarContactSource) {
            _sonarConsoleModel = sonarConsoleModel;
            _sonarMarkerScreenModel = sonarMarkerScreenModel;
            _gameCameraService = gameCameraService;
            _sonarContactService = sonarContactService;
            _sonarMarkRule = sonarMarkRule;
            _sonarMarkerProjection = sonarMarkerProjection;
            _sonarStationMarkerSource = sonarStationMarkerSource;
            _overboardSonarContactSource = overboardSonarContactSource;
        }

        public void LateTick() {
            Camera camera = _gameCameraService.OutputCamera;
            bool hasStation = _sonarStationMarkerSource.TryGetStation(out SonarContact station);
            _drifters.Clear();
            _overboardSonarContactSource.CollectOtherDrifterPositions(_drifters);
            bool hasMarkers = camera != null && (_sonarConsoleModel.Marks.Count > 0 || hasStation || _drifters.Count > 0);
            if (hasMarkers == false && _hasShownMarkers == false)
                return;

            _sonarMarkerScreenModel.BeginFrame();
            if (hasMarkers)
                LayOutMarkers(camera, hasStation, station);

            _hasShownMarkers = hasMarkers;
            _sonarMarkerScreenModel.CommitFrame();
        }

        private void LayOutMarkers(Camera camera, bool hasStation, SonarContact station) {
            IReadOnlyList<SonarMark> marks = _sonarConsoleModel.Marks;
            for (int i = 0; i < marks.Count; i++) {
                SonarMark mark = marks[i];
                if (_sonarMarkRule.IsExpired(mark.ExpiresAt, NetworkTime.time) || _sonarContactService.TryGetKind(mark.KindId, out SonarContactKind kind) == false)
                    continue;

                AddMarker(camera, kind, mark.Position);
            }

            if (hasStation)
                AddMarker(camera, station.Kind, station.Position + Vector3.up * station.Radius);

            for (int i = 0; i < _drifters.Count; i++)
                AddMarker(camera, _overboardSonarContactSource.Kind, _drifters[i]);
        }

        private void AddMarker(Camera camera, SonarContactKind kind, Vector3 position) {
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            SonarMarkerPlacement placement = _sonarMarkerProjection.Place(camera.WorldToScreenPoint(position), screenSize);
            float distance = Vector3.Distance(camera.transform.position, position);
            _sonarMarkerScreenModel.AddMarker(new SonarMarkerDisplay(kind, placement.ScreenPosition, placement.IsOnScreen, placement.ArrowDegrees, distance));
        }
    }
}
