#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using Features.ShipModule.Scripts.Generated;
using Features.SonarModule.Contracts;
using Features.SonarModule.Data.Generated;
using Features.SonarModule.Scripts.Core;
using NUnit.Framework;
using Tests.Coop.Shared;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Coop.Host {
    // The client navigates with a real second process (PR #64): at the radar's sonar console it pings and marks the
    // station and the farthest rock; the host has the same marks at the same places, over the real contacts, and shows
    // them on its HUD. The client's haze (the same clear and hidden distances as the host's) and its own station marker
    // are switched through its debug window for the client only: the host's view keeps its own.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopClientNavigationViewTest {
        private const int TEST_TIMEOUT_MS = 240000;
        private const int LOOP = 1;
        private const string STATION_CONTACT_ID = "station";
        // Synced mark positions are the same numbers on both peers; the contact may drift with the world a little (m).
        private const float MARK_POSITION_TOLERANCE = 0.05f;
        private const float CONTACT_POSITION_TOLERANCE = 1f;
        // The rock the client marked lies at least this far from the ship: the sonar's long range, past the haze (m).
        private const float LONG_RANGE_METERS = 300f;
        private const float HAZE_DISTANCE_TOLERANCE = 0.01f;

        private CoopFlightHostHarness _host;

        [SetUp]
        public void SetUp() =>
            _host = new CoopFlightHostHarness();

        [TearDown]
        public void TearDown() =>
            _host?.Dispose();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenClientMarksBySonar_AndSwitchesItsHazeAndStationMarker_ThenHostSeesTheMarksAndKeepsItsOwnView() {
            // LoopSmokeErrorLog fails the test with the full list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return _host.HostAndLetClientInCoroutine(CoopFlightSteps.SCENARIO_NAVIGATION_VIEW);
            yield return _host.StartAndBoardCoroutine();
            new LoopSmokeSonarCheck(_host.Session).InstallRadarAtStation();
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_STAND_BY_CONSOLE, CoopFlightSteps.BY_CONSOLE);
            _host.Flight.PullLaunchLever(LOOP);
            yield return _host.WaitForCruiseCoroutine(LOOP, _host.FaceAwayFromStation);
            yield return WatchClientMarkCoroutine();
            yield return WatchClientSwitchHazeCoroutine();
            yield return WatchClientShowStationCoroutine();
            _host.Flight.FaceDestination();
            yield return _host.WaitForLandingCoroutine(LOOP);
            yield return _host.FinishCoroutine();
        }

        private IEnumerator WatchClientMarkCoroutine() {
            _host.BeginStep("client navigator marks the station and the farthest rock");
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_MARK, CoopFlightSteps.SONAR_MARKED);
            CoopObservation client = _host.LastReport;
            CoopObservation host = CoopFlightObserver.CaptureFlight(CoopFlightSteps.SONAR_MARKED);
            string rock = client.Message;
            CollectionAssert.Contains(client.SonarMarks, STATION_CONTACT_ID, $"The client's station mark is not in its synced model: {client.DescribeFlight()}");
            CollectionAssert.Contains(client.SonarMarks, rock, $"The client's mark of {rock} is not in its synced model: {client.DescribeFlight()}");
            CollectionAssert.AreEqual(host.SonarMarks, client.SonarMarks, "Host and client have different sonar marks.");
            Assert.IsTrue(string.IsNullOrEmpty(SessionQueries.Resolve<IReadOnlySonarConsoleModel>().NavigatorKey), "The console is still taken on the host after the client left it.");
            Assert.AreEqual(host.SonarMarks.Length, host.SonarMarkerCount, "The host's HUD does not show a marker for every crew mark.");
            Assert.AreEqual(client.SonarMarks.Length, client.SonarMarkerCount, "The client's HUD does not show a marker for every crew mark.");
            for (int i = 0; i < host.SonarMarks.Length; i++)
                AssertMarkOverContact(host, client, i);

            float rockRange = RangeFromShip(host, Array.IndexOf(host.SonarMarks, rock));
            Assert.Greater(rockRange, LONG_RANGE_METERS, $"The client's farthest rock mark {rock} lies only {rockRange:0} m from the ship.");
            Debug.Log($"[CoopTest] the client marked the station and {rock} at {rockRange:0} m; the host shows {host.SonarMarkerCount} markers");
        }

        // The same synced mark on both peers, over the contact the host's sonar knows by that id.
        private static void AssertMarkOverContact(CoopObservation host, CoopObservation client, int index) {
            string id = host.SonarMarks[index];
            float apart = Vector3.Distance(host.SonarMarkFromDestination[index], client.SonarMarkFromDestination[index]);
            Assert.Less(apart, MARK_POSITION_TOLERANCE, $"The mark of {id} lies {apart:0.00} m apart on the host and the client.");
            Assert.IsTrue(SessionQueries.Resolve<ISonarContactService>().TryGetContact(id, out SonarContact contact), $"The host's sonar knows no contact {id} the client marked.");
            Vector3 destination = SessionQueries.Resolve<IReadOnlyShipRunModel>().TransitDestination;
            Vector3 contactTop = contact.Position + Vector3.up * contact.Radius - destination;
            float offContact = Vector3.Distance(contactTop, host.SonarMarkFromDestination[index]);
            Assert.Less(offContact, CONTACT_POSITION_TOLERANCE, $"The mark of {id} lies {offContact:0.0} m off its contact on the host.");
        }

        private static float RangeFromShip(CoopObservation host, int markIndex) {
            Vector3 fromShip = host.SonarMarkFromDestination[markIndex] - host.ShipFromDestination;
            fromShip.y = 0f;
            return fromShip.magnitude;
        }

        private IEnumerator WatchClientSwitchHazeCoroutine() {
            _host.BeginStep("client switches its haze off and on");
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_HAZE, CoopFlightSteps.HAZE_ON);
            CoopObservation host = CoopFlightObserver.CaptureFlight(CoopFlightSteps.HAZE_ON);
            Assert.IsTrue(host.IsHazeEnabled && _host.LastReport.IsHazeEnabled, "The haze is not on on both peers at the start.");
            Assert.AreEqual(host.HazeClearMeters, _host.LastReport.HazeClearMeters, HAZE_DISTANCE_TOLERANCE, "The client sees clear to another distance than the host.");
            Assert.AreEqual(host.HazeHiddenMeters, _host.LastReport.HazeHiddenMeters, HAZE_DISTANCE_TOLERANCE, "The client's haze hides things at another distance than the host's.");
            Debug.Log($"[CoopTest] haze on both peers: clear to {host.HazeClearMeters:0} m, hidden from {host.HazeHiddenMeters:0} m; client screenshot {_host.LastReport.Screenshot}");

            yield return _host.WaitForReportCoroutine(CoopFlightSteps.HAZE_OFF);
            Assert.IsFalse(_host.LastReport.IsHazeEnabled, "The client's debug toggle did not turn its haze off.");
            Assert.IsTrue(CoopFlightObserver.CaptureFlight(CoopFlightSteps.HAZE_OFF).IsHazeEnabled, "The client's haze toggle turned the host's haze off.");

            yield return _host.WaitForReportCoroutine(CoopFlightSteps.HAZE_BACK_ON);
            Assert.IsTrue(_host.LastReport.IsHazeEnabled, "The client's debug toggle did not turn its haze back on.");
            Assert.IsTrue(CoopFlightObserver.CaptureFlight(CoopFlightSteps.HAZE_BACK_ON).IsHazeEnabled, "The host's haze is off after the client's toggles.");
        }

        private IEnumerator WatchClientShowStationCoroutine() {
            _host.BeginStep("client shows and hides its own station marker");
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_SHOW_STATION, CoopFlightSteps.STATION_SHOWN);
            Assert.IsTrue(_host.LastReport.IsStationMarkerShown, "The client's \"Show station\" did not turn its station marker on.");
            Assert.IsFalse(CoopFlightObserver.CaptureFlight(CoopFlightSteps.STATION_SHOWN).IsStationMarkerShown, "The client's \"Show station\" turned the host's station marker on.");

            yield return _host.WaitForReportCoroutine(CoopFlightSteps.STATION_HIDDEN);
            Assert.IsFalse(_host.LastReport.IsStationMarkerShown, "The client's second click did not turn its station marker off.");
            Assert.IsFalse(CoopFlightObserver.CaptureFlight(CoopFlightSteps.STATION_HIDDEN).IsStationMarkerShown, "The host's station marker is on after the client's toggles.");
        }
    }
}
#endif
