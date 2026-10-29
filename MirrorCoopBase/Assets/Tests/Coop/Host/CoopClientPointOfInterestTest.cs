#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Globalization;
using Features.CutsceneModule.Scripts.Models;
using Features.DebugModule.Contracts;
using Features.PointOfInterestModule.Scripts.Network;
using Features.ShipModule.Scripts.Debug;
using Features.ShopModule.Scripts.Generated;
using Mirror;
using NUnit.Framework;
using Tests.Coop.Shared;
using Tests.PlayMode.PointOfInterest;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Coop.Host {
    // A bug nest with a real second client process: in the first cruise the client has the field's points where the
    // host has them, shows their looks and has them on its sonar; the host's debug window puts the ship before the
    // nearest nest and the cruise flies it in; the client plays the entry cutscene at the host's network time (controls
    // blocked, HUD hidden) and, after the host rams the reward container, sees it gone and the host's balance.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopClientPointOfInterestTest {
        private const int TEST_TIMEOUT_MS = 300000;
        private const int LOOP = 1;
        private const string REMOVE_LABEL = "Remove all field agents";
        private const float ENTRY_THROTTLE = 0.6f;
        private const float FULL_THROTTLE = 1f;
        private const float STOPPED = 0f;
        private const float POSITION_TOLERANCE_METERS = 0.05f;
        // Both peers start at the playback's network time; a client's network time estimate is a few frames off.
        private const double START_TOLERANCE_SECONDS = 0.25;
        private const char POINT_SEPARATOR = ';';
        private const char ID_SEPARATOR = ':';
        private const char FIELD_SEPARATOR = ',';
        private const char MESSAGE_SEPARATOR = ' ';

        private CoopFlightHostHarness _host;
        private PointOfInterestTestSteps _steps;
        private CutsceneLocalModel _cutscene;
        private double _hostCutsceneStart;

        [SetUp]
        public void SetUp() =>
            _host = new CoopFlightHostHarness();

        [TearDown]
        public void TearDown() {
            if (_cutscene != null)
                _cutscene.OnPlayingChanged -= OnHostCutsceneChanged;

            _host?.Dispose();
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenTheHostFliesIntoANest_ThenTheClientSeesThePointPlaysTheCutsceneAtTheSameTimeAndSeesThePayout() {
            // LoopSmokeErrorLog fails the test with the full list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return _host.HostAndLetClientInCoroutine(CoopFlightSteps.SCENARIO_POINT_OF_INTEREST);
            yield return _host.StartAndBoardCoroutine();
            _host.Flight.PullLaunchLever(LOOP);
            yield return _host.WaitForCruiseCoroutine(LOOP, _host.FaceAwayFromStation);
            yield return _host.WaitForTurnDoneCoroutine();
            SetThrottle(STOPPED);
            _steps = new PointOfInterestTestSteps(_host.Session, SetThrottle);
            yield return _steps.WaitForPointsCoroutine();
            yield return ClientSeesPointsCoroutine();

            Vector3 start = _host.Ship.transform.position;
            yield return PlayCutsceneTogetherCoroutine();
            yield return ClientSeesPayoutCoroutine();

            yield return _steps.ClickCoroutine(DebugSections.AGENTS, REMOVE_LABEL);
            SetThrottle(FULL_THROTTLE);
            yield return _host.Flight.PutShipBackCoroutine(start);
            yield return _host.WaitForLandingCoroutine(LOOP);
            yield return _host.FinishCoroutine();
        }

        private IEnumerator ClientSeesPointsCoroutine() {
            _host.BeginStep("client sees the points of interest");
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_REPORT_POINTS, CoopFlightSteps.POINTS_SEEN);
            string[] described = _host.LastReport.Message.Split(POINT_SEPARATOR);
            Assert.AreEqual(_steps.Points.Count, described.Length, $"The client has other points than the host: {_host.LastReport.Message}.");
            foreach (string point in described)
                AssertSamePoint(point);

            Debug.Log($"[CoopTest] the client has the host's {described.Length} point(s), shown and on its sonar; client screenshot {_host.LastReport.Screenshot}");
        }

        private void AssertSamePoint(string described) {
            string[] idAndFields = described.Split(ID_SEPARATOR);
            int id = int.Parse(idAndFields[0], CultureInfo.InvariantCulture);
            string[] fields = idAndFields[1].Split(FIELD_SEPARATOR);
            Vector3 clientPosition = new(Parse(fields[0]), Parse(fields[1]), Parse(fields[2]));
            float gap = Vector3.Distance(clientPosition, _steps.PositionOf(id));
            Assert.Less(gap, POSITION_TOLERANCE_METERS, $"Point {id} stands {gap:0.00} m from the host's on the client.");
            Assert.IsTrue(bool.Parse(fields[3]), $"The client does not show point {id}.");
            Assert.IsTrue(bool.Parse(fields[4]), $"Point {id} is not on the client's sonar.");
        }

        private IEnumerator PlayCutsceneTogetherCoroutine() {
            _host.BeginStep("the entry cutscene on both peers");
            int pointId = 0;
            yield return _steps.MoveToNearestPointCoroutine(id => pointId = id);
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_WATCH_CUTSCENE, CoopFlightSteps.WATCHING);
            _cutscene = SessionQueries.Resolve<CutsceneLocalModel>();
            _cutscene.OnPlayingChanged += OnHostCutsceneChanged;
            yield return _steps.EnterZoneCoroutine(pointId, ENTRY_THROTTLE);
            yield return _steps.WatchEntryCutsceneCoroutine(false, "coop-host-cutscene-nest.png", "coop-host-cutscene-flock.png");
            yield return _host.WaitForReportCoroutine(CoopFlightSteps.CUTSCENE_WATCHED);
            string[] parts = _host.LastReport.Message.Split(MESSAGE_SEPARATOR);
            double clientStart = double.Parse(parts[0], CultureInfo.InvariantCulture);
            Assert.AreEqual(_hostCutsceneStart, clientStart, START_TOLERANCE_SECONDS, "The client did not start the cutscene with the host.");
            Assert.IsTrue(bool.Parse(parts[2]), "The client's controls were not blocked in the cutscene.");
            Assert.IsTrue(bool.Parse(parts[3]), "The client's HUD was not hidden in the cutscene.");
            Debug.Log($"[CoopTest] cutscene: host start {_hostCutsceneStart:0.000}, client {clientStart:0.000} - {parts[1]}; client screenshot {_host.LastReport.Screenshot}");
            _steps.AssertBugsCameOut();
            yield return _steps.RamContainerCoroutine(pointId);
        }

        private IEnumerator ClientSeesPayoutCoroutine() {
            _host.BeginStep("client sees the payout");
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_REPORT_PAYOUT, CoopFlightSteps.PAYOUT_SEEN);
            long hostBalance = SessionQueries.Resolve<IReadOnlyWalletModel>().Balance;
            Assert.AreEqual(hostBalance, _host.LastReport.Balance, "The client's crew balance is not the host's after the payout.");
        }

        private void OnHostCutsceneChanged() {
            if (_cutscene.IsPlaying)
                _hostCutsceneStart = NetworkTime.time;
        }

        private static void SetThrottle(float position) =>
            SessionQueries.Resolve<IShipThrottleDebug>().DebugSetThrottle(position);

        private static float Parse(string value) =>
            float.Parse(value, CultureInfo.InvariantCulture);
    }
}
#endif
