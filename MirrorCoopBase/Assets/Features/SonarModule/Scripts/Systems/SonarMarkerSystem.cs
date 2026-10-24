using System.Collections.Generic;
using Features.CameraModule.Scripts.Services;
using Features.SonarModule.Contracts;
using Features.SonarModule.Data;
using Features.SonarModule.Data.Generated;
using Features.SonarModule.Scripts.Core;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.SonarModule.Scripts.Systems {
    // Every peer: lays the crew's synced marks out on this peer's screen once per frame, after the camera moved.
    // A mark that ran out hides at once here; the server takes it out of the model a moment later.
    public sealed class SonarMarkerSystem : ILateTickable {
        private readonly IReadOnlySonarConsoleModel _sonarConsoleModel;
        private readonly SonarMarkerScreenModel _sonarMarkerScreenModel;
        private readonly IGameCameraService _gameCameraService;
        private readonly ISonarContactService _sonarContactService;
        private readonly ISonarMarkRule _sonarMarkRule;
        private readonly ISonarMarkerProjection _sonarMarkerProjection;

        private bool _hasShownMarkers;

        public SonarMarkerSystem(
            IReadOnlySonarConsoleModel sonarConsoleModel,
            SonarMarkerScreenModel sonarMarkerScreenModel,
            IGameCameraService gameCameraService,
            ISonarContactService sonarContactService,
            ISonarMarkRule sonarMarkRule,
            ISonarMarkerProjection sonarMarkerProjection) {
            _sonarConsoleModel = sonarConsoleModel;
            _sonarMarkerScreenModel = sonarMarkerScreenModel;
            _gameCameraService = gameCameraService;
            _sonarContactService = sonarContactService;
            _sonarMarkRule = sonarMarkRule;
            _sonarMarkerProjection = sonarMarkerProjection;
        }

        public void LateTick() {
            Camera camera = _gameCameraService.OutputCamera;
            bool hasMarks = camera != null && _sonarConsoleModel.Marks.Count > 0;
            if (hasMarks == false && _hasShownMarkers == false)
                return;

            _sonarMarkerScreenModel.BeginFrame();
            if (hasMarks)
                LayOutMarkers(camera);

            _hasShownMarkers = hasMarks;
            _sonarMarkerScreenModel.CommitFrame();
        }

        private void LayOutMarkers(Camera camera) {
            IReadOnlyList<SonarMark> marks = _sonarConsoleModel.Marks;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector3 cameraPosition = camera.transform.position;
            for (int i = 0; i < marks.Count; i++) {
                SonarMark mark = marks[i];
                if (_sonarMarkRule.IsExpired(mark.ExpiresAt, NetworkTime.time) || _sonarContactService.TryGetKind(mark.KindId, out SonarContactKind kind) == false)
                    continue;

                SonarMarkerPlacement placement = _sonarMarkerProjection.Place(camera.WorldToScreenPoint(mark.Position), screenSize);
                float distance = Vector3.Distance(cameraPosition, mark.Position);
                _sonarMarkerScreenModel.AddMarker(new SonarMarkerDisplay(kind, placement.ScreenPosition, placement.IsOnScreen, placement.ArrowDegrees, distance));
            }
        }
    }
}
