#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using Features.CharacterMovableModule.Scripts;
using Features.CharacterMovableModule.Scripts.Models;
using Features.GrabModule.Scripts;
using Features.MvpModule;
using Features.ShipModule.Scripts;
using Features.SonarModule.Data;
using Features.SonarModule.Data.Generated;
using Features.SonarModule.Scripts.Network;
using Features.SonarModule.Scripts.UI;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace Tests.PlayMode.LoopSmoke {
    // The navigator's sonar console on the radar module: at the station the host stands by it; in cruise the host
    // takes the console through its use command (what E sends), the window opens and holds the player, the window's
    // Ping button finds the station, a click on its echo marks it for the crew (synced model and the HUD marker), the
    // Leave button frees the console and the mark runs out (LoopSmokeSonarConfigOverride keeps it short).
    public sealed class LoopSmokeSonarCheck {
        public const string STATION_CONTACT_ID = "station";

        private const string RADAR_MODULE_ID = "radar";
        private const float RADAR_RANGE_METERS = 1000f;
        private const float TIMEOUT_SECONDS = 4f;
        private const float BOARD_SETTLE_SECONDS = 0.6f;
        private const float DROP_HEIGHT = 0.5f;
        // Behind the radar console on the deck, facing its screen.
        private static readonly Vector3 _navigatorSpot = new Vector3(2.0f, 0.3f, 1.4f);

        private readonly LoopSmokeSession _session;

        private static NetworkIdentity Player => SessionQueries.LocalPlayer;

        public LoopSmokeSonarCheck(LoopSmokeSession session) =>
            _session = session;

        public LoopSmokeSonarCheck(LoopSmokeContext context) : this(context.Session) { }

        // A run that did not reinstall the radar by hand puts it in at the station.
        public void InstallRadarAtStation() {
            foreach (ShipSocket socket in _session.Ship.Sockets) {
                if (socket.AcceptedType == ShipModuleType.Radar)
                    Assert.IsTrue(socket.ServerTryInstall(RADAR_MODULE_ID), "Sonar: the radar was not installed at the station.");
            }
        }

        [LoopSmokeStep(LoopSmokePhase.BeforeTakeoff, 1, 1, 30)]
        public IEnumerator StandByConsoleCoroutine() {
            SonarConsoleInteractable console = FindConsole();
            Assert.IsFalse(console.IsConsoleActive, "Sonar: the console is active at the station, where E takes the radar off.");
            ShipDeckBoarding.PutAbove(Player.GetComponent<CharacterMovableBase>(), _session.Ship, _navigatorSpot, DROP_HEIGHT);
            yield return new WaitForSeconds(BOARD_SETTLE_SECONDS);
        }

        [LoopSmokeStep(LoopSmokePhase.InCruise, 1, 1, 10)]
        public IEnumerator UseConsoleInCruiseCoroutine() {
            yield return MarkStationInCruiseCoroutine();
            IReadOnlySonarConsoleModel synced = SessionQueries.Resolve<IReadOnlySonarConsoleModel>();
            float markTimeout = LoopSmokeSonarConfigOverride.STATION_MARK_SECONDS + TIMEOUT_SECONDS;
            yield return _session.WaitForCoroutine(() => synced.Marks.Count == 0, markTimeout, "Sonar: the station mark running out");
            Assert.IsTrue(Player.GetComponent<ShipRider>().IsRiding, "Sonar: the host does not ride the ship after the console.");
        }

        // Takes the console, pings, marks the station and leaves the console again; the mark stays.
        public IEnumerator MarkStationInCruiseCoroutine() {
            SonarConsoleInteractable console = FindConsole();
            SonarScopeModel scope = SessionQueries.Resolve<SonarScopeModel>();
            IReadOnlySonarConsoleModel synced = SessionQueries.Resolve<IReadOnlySonarConsoleModel>();
            Assert.IsTrue(console.IsConsoleActive, "Sonar: the radar console is not active in cruise.");

            Player.GetComponent<UseController>().RequestUse(console);
            yield return _session.WaitForCoroutine(() => scope.IsOpen && IsWindowOpen(), TIMEOUT_SECONDS, "Sonar: the console window open for the host");
            Assert.IsFalse(string.IsNullOrEmpty(synced.NavigatorKey), "Sonar: the synced console has no navigator.");
            Assert.IsTrue(SessionQueries.Resolve<PlayerControlBlockModel>().IsBlocked, "Sonar: walking and aim are not blocked at the console.");

            FindScopeView().DebugPingButton.onClick.Invoke();
            yield return _session.WaitForCoroutine(() => HasBlip(scope, STATION_CONTACT_ID), TIMEOUT_SECONDS, "Sonar: the ping's echo of the station");
            Assert.AreEqual(RADAR_RANGE_METERS, scope.Frame.RangeMeters, "Sonar: the range is not the radar module's SonarRange stat.");

            ClickBlip(STATION_CONTACT_ID);
            yield return _session.WaitForCoroutine(() => HasMark(synced, STATION_CONTACT_ID), TIMEOUT_SECONDS, "Sonar: the station mark in the synced model");
            yield return _session.WaitForCoroutine(() => SessionQueries.Resolve<SonarMarkerScreenModel>().Markers.Count == 1, TIMEOUT_SECONDS, "Sonar: the station marker on the HUD");

            FindScopeView().DebugLeaveButton.onClick.Invoke();
            yield return _session.WaitForCoroutine(() => scope.IsOpen == false && IsWindowOpen() == false && string.IsNullOrEmpty(synced.NavigatorKey), TIMEOUT_SECONDS, "Sonar: the console left and freed");
            Assert.IsFalse(SessionQueries.Resolve<PlayerControlBlockModel>().IsBlocked, "Sonar: the player is still blocked after leaving the console.");
        }

        private static bool IsWindowOpen() =>
            SessionQueries.Resolve<IWindowsService>().GetWindow(typeof(SonarWindow)).WindowStatus != WindowStatus.Closed;

        private static SonarScopeView FindScopeView() {
            SonarScopeView view = Object.FindAnyObjectByType<SonarScopeView>();
            Assert.IsNotNull(view, "Sonar: no scope view in the open window.");
            return view;
        }

        private static void ClickBlip(string contactId) {
            foreach (SonarBlipButton blip in Object.FindObjectsByType<SonarBlipButton>(FindObjectsSortMode.None)) {
                if (blip.isActiveAndEnabled && blip.ContactId == contactId) {
                    blip.GetComponent<Button>().onClick.Invoke();
                    return;
                }
            }

            Assert.Fail($"Sonar: no echo of '{contactId}' to click on the scope.");
        }

        private static bool HasBlip(SonarScopeModel scope, string contactId) {
            foreach (SonarBlip blip in scope.Blips) {
                if (blip.ContactId == contactId)
                    return true;
            }

            return false;
        }

        private static bool HasMark(IReadOnlySonarConsoleModel synced, string contactId) {
            IReadOnlyList<SonarMark> marks = synced.Marks;
            for (int i = 0; i < marks.Count; i++) {
                if (marks[i].ContactId == contactId)
                    return true;
            }

            return false;
        }

        private SonarConsoleInteractable FindConsole() {
            SonarConsoleInteractable console = _session.Ship.GetComponentInChildren<SonarConsoleInteractable>();
            Assert.IsNotNull(console, "Sonar: the ship has no sonar console.");
            return console;
        }
    }
}
#endif
