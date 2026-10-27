#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using Features.CharacterMovableModule.Scripts;
using Features.DebugModule.Contracts;
using Features.GrabModule.Scripts;
using Features.SonarModule.Data;
using Features.SonarModule.Data.Generated;
using Features.SonarModule.Scripts.Network;
using Features.SonarModule.Scripts.UI;
using Features.SpaceHazeModule.Data;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.Coop.Client {
    // The client navigates (scenario navigation-view): in the cruise it takes the radar's sonar console through its use
    // command, pings, marks the station and the farthest rock echo for the crew and leaves the console; then it turns
    // its own haze off and on and its own station marker on and off through the debug window, capturing the screen.
    // Each step on the host's command.
    public sealed class CoopNavigationViewClient : CoopClientBot {
        private const int LOOP = 1;
        private const string STATION_CONTACT_ID = "station";
        private const string HAZE_SHOWN_LABEL = "Haze: on";
        private const string HAZE_HIDDEN_LABEL = "Haze: off";
        private const string STATION_HIDDEN_LABEL = "Show station: off";
        private const string STATION_SHOWN_LABEL = "Show station: on";
        private const string HAZE_ON_SCREENSHOT = "haze-on";
        private const string HAZE_OFF_SCREENSHOT = "haze-off";
        private const string STATION_MARKER_SCREENSHOT = "station-marker";
        private const string SONAR_SCREENSHOT = "sonar-marks";
        private const float CONSOLE_STAND_METERS = 1f;
        private const float DROP_HEIGHT = 0.5f;
        // The ping's wave sweeps the scope in this long; the far echoes come last.
        private const float PING_SWEEP_SECONDS = 1.5f;
        private static readonly string[] _rockKindIds = { "large-rock", "big-rock", "debris" };

        private readonly WaitForSeconds _pingSweep = new WaitForSeconds(PING_SWEEP_SECONDS);
        private readonly DebugWindowClicking _debugWindow = new DebugWindowClicking();
        private string _screenshot;

        protected override IEnumerator RunScenarioCoroutine() {
            yield return BoardCoroutine();
            yield return StandByConsoleCoroutine();
            yield return WaitForCruiseCoroutine(LOOP);
            yield return MarkStationAndFarRockCoroutine();
            yield return ToggleHazeCoroutine();
            yield return ToggleStationMarkerCoroutine();
            yield return WaitForLandingCoroutine(LOOP);
        }

        // Beside the console on the deck, toward the deck's middle: within the console's reach for the cruise.
        private IEnumerator StandByConsoleCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_STAND_BY_CONSOLE);
            Transform ship = Ship.transform;
            Vector3 console = ship.InverseTransformPoint(FindConsole().transform.position);
            Vector3 middle = ship.InverseTransformPoint(Ship.DeckGeometry.Bounds.center);
            Vector3 toMiddle = Vector3.ProjectOnPlane(middle - console, Vector3.up).normalized;
            Vector3 spot = ShipDeckBoarding.DeckEdgeSpot(Ship, console + toMiddle * CONSOLE_STAND_METERS, toMiddle);
            ShipDeckBoarding.PutAbove(SessionQueries.LocalPlayer.GetComponent<CharacterMovableBase>(), Ship, spot, DROP_HEIGHT);
            yield return Settle;
            Report(CoopFlightObserver.CaptureFlight(CoopFlightSteps.BY_CONSOLE));
        }

        private IEnumerator MarkStationAndFarRockCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_MARK);
            SonarConsoleInteractable console = FindConsole();
            SonarScopeModel scope = SessionQueries.Resolve<SonarScopeModel>();
            IReadOnlySonarConsoleModel synced = SessionQueries.Resolve<IReadOnlySonarConsoleModel>();
            SessionQueries.LocalPlayer.GetComponent<UseController>().RequestUse(console);
            yield return WaitForCoroutine(() => scope.IsOpen && Object.FindAnyObjectByType<SonarScopeView>() != null, "the sonar console window open");

            Object.FindAnyObjectByType<SonarScopeView>().DebugPingButton.onClick.Invoke();
            yield return WaitForCoroutine(() => HasBlip(scope, STATION_CONTACT_ID) && TryFindFarthestRock(scope, out _), "the ping's echoes of the station and a rock");
            yield return _pingSweep;
            TryFindFarthestRock(scope, out string rock);
            yield return ClickBlipCoroutine(STATION_CONTACT_ID);
            yield return ClickBlipCoroutine(rock);
            yield return WaitForCoroutine(() => HasMark(synced, STATION_CONTACT_ID) && HasMark(synced, rock), $"the marks of the station and {rock} in the synced model");
            yield return CaptureScreenCoroutine(SONAR_SCREENSHOT, path => _screenshot = path);

            Object.FindAnyObjectByType<SonarScopeView>().DebugLeaveButton.onClick.Invoke();
            yield return WaitForCoroutine(() => scope.IsOpen == false && string.IsNullOrEmpty(synced.NavigatorKey), "the console left and freed");
            yield return Settle;
            CoopObservation observation = CoopFlightObserver.CaptureFlight(CoopFlightSteps.SONAR_MARKED);
            observation.Message = rock;
            observation.Screenshot = _screenshot;
            Report(observation);
        }

        private IEnumerator ToggleHazeCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_HAZE);
            SpaceHazeModel haze = SessionQueries.Resolve<SpaceHazeModel>();
            yield return CaptureScreenCoroutine(HAZE_ON_SCREENSHOT, path => _screenshot = path);
            ReportWithScreenshot(CoopFlightSteps.HAZE_ON);

            yield return _debugWindow.ClickCoroutine(DebugSections.VIEW, HAZE_SHOWN_LABEL);
            yield return FailIfCoroutine(_debugWindow.Failure, $"Clicking \"{HAZE_SHOWN_LABEL}\"");
            yield return WaitForCoroutine(() => haze.IsEnabled == false, "the haze off on this peer");
            yield return CaptureScreenCoroutine(HAZE_OFF_SCREENSHOT, path => _screenshot = path);
            ReportWithScreenshot(CoopFlightSteps.HAZE_OFF);

            yield return _debugWindow.ClickCoroutine(DebugSections.VIEW, HAZE_HIDDEN_LABEL);
            yield return FailIfCoroutine(_debugWindow.Failure, $"Clicking \"{HAZE_HIDDEN_LABEL}\"");
            yield return WaitForCoroutine(() => haze.IsEnabled, "the haze back on on this peer");
            Report(CoopFlightObserver.CaptureFlight(CoopFlightSteps.HAZE_BACK_ON));
        }

        private IEnumerator ToggleStationMarkerCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_SHOW_STATION);
            SonarStationMarkerModel marker = SessionQueries.Resolve<SonarStationMarkerModel>();
            yield return _debugWindow.ClickCoroutine(DebugSections.SHIP, STATION_HIDDEN_LABEL);
            yield return FailIfCoroutine(_debugWindow.Failure, $"Clicking \"{STATION_HIDDEN_LABEL}\"");
            yield return WaitForCoroutine(() => marker.IsShown, "the own station marker on");
            yield return CaptureScreenCoroutine(STATION_MARKER_SCREENSHOT, path => _screenshot = path);
            ReportWithScreenshot(CoopFlightSteps.STATION_SHOWN);

            yield return _debugWindow.ClickCoroutine(DebugSections.SHIP, STATION_SHOWN_LABEL);
            yield return FailIfCoroutine(_debugWindow.Failure, $"Clicking \"{STATION_SHOWN_LABEL}\"");
            yield return WaitForCoroutine(() => marker.IsShown == false, "the own station marker off");
            Report(CoopFlightObserver.CaptureFlight(CoopFlightSteps.STATION_HIDDEN));
        }

        private void ReportWithScreenshot(string step) {
            CoopObservation observation = CoopFlightObserver.CaptureFlight(step);
            observation.Screenshot = _screenshot;
            Report(observation);
        }

        private IEnumerator ClickBlipCoroutine(string contactId) {
            foreach (SonarBlipButton blip in Object.FindObjectsByType<SonarBlipButton>(FindObjectsSortMode.None)) {
                if (blip.isActiveAndEnabled == false || blip.ContactId != contactId)
                    continue;

                blip.GetComponent<Button>().onClick.Invoke();
                yield return null;
                yield break;
            }

            yield return FailCoroutine($"No echo of '{contactId}' to click on the scope.");
        }

        // The rock echo (a single rock or a debris patch) farthest from the scope's middle: the longest range the radar
        // reaches.
        private static bool TryFindFarthestRock(SonarScopeModel scope, out string contactId) {
            contactId = null;
            float farthest = -1f;
            IReadOnlyList<SonarBlip> blips = scope.Blips;
            for (int i = 0; i < blips.Count; i++) {
                SonarBlip blip = blips[i];
                bool isRock = Array.IndexOf(_rockKindIds, blip.Kind.Id) >= 0;
                if (isRock == false || blip.ScopePosition.magnitude <= farthest)
                    continue;

                farthest = blip.ScopePosition.magnitude;
                contactId = blip.ContactId;
            }

            return contactId != null;
        }

        private static bool HasBlip(SonarScopeModel scope, string contactId) {
            foreach (SonarBlip blip in scope.Blips) {
                if (blip.ContactId == contactId)
                    return true;
            }

            return false;
        }

        private static bool HasMark(IReadOnlySonarConsoleModel synced, string contactId) {
            foreach (SonarMark mark in synced.Marks) {
                if (mark.ContactId == contactId)
                    return true;
            }

            return false;
        }

        private SonarConsoleInteractable FindConsole() =>
            Ship.GetComponentInChildren<SonarConsoleInteractable>();
    }
}
#endif
