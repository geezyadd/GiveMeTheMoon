#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.CameraModule.Scripts;
using Features.CameraModule.Scripts.Services;
using Features.DebugModule.Contracts;
using Features.DebugModule.Scripts.Models;
using Features.DebugModule.Scripts.UI;
using Features.InputModule.Realization.Scripts.Generated;
using Features.ShipModule.Scripts.Generated;
using Features.SonarModule.Data;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // The debug window's "Show station" toggle, clicked as a player does: in cruise it puts this peer's own station
    // marker on the HUD, over the station when the level camera faces it and on the screen's edge with the arrow when
    // the camera turns away; a second click takes it off again.
    public sealed class LoopSmokeStationMarkerCheck {
        private const string HIDDEN_LABEL = "Show station: off";
        private const string SHOWN_LABEL = "Show station: on";
        private const string STATION_KIND_ID = "station";
        private const float TIMEOUT_SECONDS = 4f;
        // Mouse look per frame while turning (pixels), and how straight the camera must look along the line.
        private const float TURN_LOOK_PIXELS = 40f;
        private const float FACING_ALIGNMENT = 0.97f;

        private readonly LoopSmokeSession _session;

        public LoopSmokeStationMarkerCheck(LoopSmokeContext context) =>
            _session = context.Session;

        [LoopSmokeStep(LoopSmokePhase.InCruise, 2, 5, 10)]
        public IEnumerator ShowStationInCruiseCoroutine() {
            SonarStationMarkerModel markerModel = SessionQueries.Resolve<SonarStationMarkerModel>();
            SonarMarkerScreenModel screen = SessionQueries.Resolve<SonarMarkerScreenModel>();
            Assert.IsFalse(markerModel.IsShown, "Station marker: it is on before the toggle was clicked.");
            Assert.IsFalse(TryFindStationMarker(screen, out _), "Station marker: a station marker is on the HUD before the toggle.");

            yield return ClickThroughDebugWindowCoroutine(HIDDEN_LABEL);
            Assert.IsTrue(markerModel.IsShown, $"Station marker: \"{HIDDEN_LABEL}\" did not turn it on.");

            CameraLookDriver.DebugPitchOverride = true;
            CameraLookDriver.DebugPitch = 0f;
            yield return FaceCoroutine(true);
            yield return _session.WaitForCoroutine(() => TryFindStationMarker(screen, out SonarMarkerDisplay marker) && marker.IsOnScreen, TIMEOUT_SECONDS, "Station marker: over the station ahead");
            TryFindStationMarker(screen, out SonarMarkerDisplay ahead);
            Assert.Greater(ahead.DistanceMeters, 0f, "Station marker: the marker ahead shows no distance.");

            yield return FaceCoroutine(false);
            yield return _session.WaitForCoroutine(() => TryFindStationMarker(screen, out SonarMarkerDisplay marker) && marker.IsOnScreen == false, TIMEOUT_SECONDS, "Station marker: on the edge with the arrow behind");

            CameraLookDriver.DebugPitchOverride = false;
            yield return ClickThroughDebugWindowCoroutine(SHOWN_LABEL);
            Assert.IsFalse(markerModel.IsShown, $"Station marker: \"{SHOWN_LABEL}\" did not turn it off.");
            yield return _session.WaitForCoroutine(() => TryFindStationMarker(screen, out _) == false, TIMEOUT_SECONDS, "Station marker: gone after the toggle off");
        }

        private static IEnumerator ClickThroughDebugWindowCoroutine(string label) {
            DebugPanelModel panel = SessionQueries.Resolve<DebugPanelModel>();
            DebugToggleView toggle = Object.FindAnyObjectByType<DebugToggleView>();
            Assert.IsNotNull(toggle, "Station marker: no debug corner button in the session.");

            toggle.DebugButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(panel.IsOpen, "Station marker: the corner button did not open the debug panel.");

            DebugActionRowItem row = FindRow(label);
            Assert.IsTrue(row.DebugButton.interactable, $"Station marker: \"{label}\" is disabled in cruise.");
            row.DebugButton.onClick.Invoke();
            yield return null;

            toggle.DebugButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(panel.IsOpen, "Station marker: the corner button did not close the debug panel.");
        }

        private static DebugActionRowItem FindRow(string label) {
            Assert.IsTrue(SessionQueries.Resolve<DebugActionsModel>().TryFind(DebugSections.SHIP, label, out DebugActionDefinition action), $"Station marker: no \"{label}\" debug action registered.");
            DebugPanelView view = Object.FindAnyObjectByType<DebugPanelView>();
            Assert.IsNotNull(view, "Station marker: no debug panel in the debug window.");
            Assert.IsTrue(view.TryGetDebugRow(action, out DebugActionRowItem row), $"Station marker: the debug panel shows no \"{label}\" button.");
            return row;
        }

        // Turns the camera toward the station or away from it through the look input the mouse fills.
        private static IEnumerator FaceCoroutine(bool isTowardStation) {
            IInputService input = SessionQueries.Resolve<IInputService>();
            Camera camera = SessionQueries.Resolve<IGameCameraService>().OutputCamera;
            float deadline = Time.realtimeSinceStartup + TIMEOUT_SECONDS;
            while (IsFacing(camera, isTowardStation) == false) {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Station marker: the camera did not turn {(isTowardStation ? "toward" : "away from")} the station.");

                input.Look.VectorChangedPerformed?.Invoke(new Vector2(TURN_LOOK_PIXELS, 0f));
                yield return null;
            }
        }

        private static bool IsFacing(Camera camera, bool isTowardStation) {
            Vector3 toStation = Vector3.ProjectOnPlane(SessionQueries.Resolve<IReadOnlyShipRunModel>().TransitDestination - camera.transform.position, Vector3.up).normalized;
            float alignment = Vector3.Dot(Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized, toStation);
            return isTowardStation ? alignment > FACING_ALIGNMENT : alignment < -FACING_ALIGNMENT;
        }


        private static bool TryFindStationMarker(SonarMarkerScreenModel screen, out SonarMarkerDisplay marker) {
            marker = default;
            foreach (SonarMarkerDisplay candidate in screen.Markers) {
                if (candidate.Kind.Id != STATION_KIND_ID)
                    continue;

                marker = candidate;
                return true;
            }

            return false;
        }
    }
}
#endif
