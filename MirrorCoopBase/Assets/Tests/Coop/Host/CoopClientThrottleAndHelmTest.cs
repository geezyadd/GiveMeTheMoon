#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using NUnit.Framework;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Coop.Host {
    // The client flies the ship with a real second process: it takes off by pushing the throttle handle, pulls it to
    // Stop in the cruise (the ship stops and stays still, at the same pose on both peers) and pushes it to Full again;
    // at the next station it sits at the helm and steers the next cruise right and left (the ship turns both ways and
    // shows the same pose on the client), cannot take the throttle from the seat, and stands up again.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopClientThrottleAndHelmTest {
        private const int TEST_TIMEOUT_MS = 300000;
        private const int THROTTLE_LOOP = 1;
        private const int HELM_LOOP = 2;
        private const float FULL_THROTTLE = 1f;
        private const float THROTTLE_TOLERANCE = 0.02f;
        private const float MIN_CRUISE_SPEED = 5f;
        private const float SPEED_SAMPLE_SECONDS = 0.25f;
        private const float STOP_TIMEOUT_SECONDS = 6f;
        private const float STAY_STOPPED_SECONDS = 2f;
        private const float STOPPED_SPEED = 0.2f;
        // Both peers show a ship at rest at the same pose (m, degrees).
        private const float REST_POSE_TOLERANCE = 0.25f;
        private const float REST_YAW_TOLERANCE = 0.5f;
        // A flying ship's pose on the client lies on the host's track within this (m, degrees).
        private const float TRACK_POSE_TOLERANCE = 1.5f;
        private const float TRACK_YAW_TOLERANCE = 1f;
        private const float SIT_POINT_TOLERANCE = 0.3f;
        // The pilot's D turns the ship at least this far right, its longer A at least this far back left (degrees).
        private const float MIN_TURN_DEGREES = 3f;

        private readonly WaitForSeconds _speedSample = new WaitForSeconds(SPEED_SAMPLE_SECONDS);
        private readonly WaitForSeconds _stayStopped = new WaitForSeconds(STAY_STOPPED_SECONDS);
        private readonly CoopShipPoseTrack _poseTrack = new CoopShipPoseTrack();

        private CoopHostHarness _host;
        private float _cruiseStartYaw;
        private float _mostRightDegrees;
        private bool _hasClientHeldThrottle;
        private Vector3 _speedSampleStart;
        private float _speedSampleStartTime;

        private ShipBase Ship => _host.Ship;
        private IReadOnlyShipRunModel Model => _host.Model;
        private static IReadOnlyShipThrottleModel Throttle => SessionQueries.Resolve<IReadOnlyShipThrottleModel>();

        [SetUp]
        public void SetUp() =>
            _host = new CoopHostHarness();

        [TearDown]
        public void TearDown() =>
            _host?.Dispose();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenClientFliesByThrottleAndHelm_AndStopsAndTurnsInCruise_ThenHostSeesTheSameShip() {
            // LoopSmokeErrorLog fails the test with the full list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return _host.HostAndLetClientInCoroutine(CoopFlightSteps.SCENARIO_THROTTLE_AND_HELM);
            yield return _host.StartAndBoardCoroutine();
            yield return WatchClientTakeOffCoroutine();
            yield return _host.WaitForCruiseCoroutine(THROTTLE_LOOP, _host.FaceAwayFromStation);
            yield return WatchClientStopAndGoCoroutine();
            yield return _host.WaitForLandingCoroutine(THROTTLE_LOOP);
            yield return WatchClientTakeHelmCoroutine();
            _host.Flight.PullLaunchLever(HELM_LOOP);
            yield return _host.WaitForCruiseCoroutine(HELM_LOOP, _host.FaceAwayFromStation);
            yield return WatchClientSteerCoroutine();
            yield return WatchClientTryThrottleFromSeatCoroutine();
            yield return CompareFlyingPoseCoroutine();
            yield return WatchClientStandUpCoroutine();
            _host.Flight.FaceDestination();
            yield return _host.WaitForLandingCoroutine(HELM_LOOP);
            yield return _host.FinishCoroutine();
        }

        // The client takes the handle by its player key on the server and pushes it past the launch mark to Full.
        private IEnumerator WatchClientTakeOffCoroutine() {
            _host.BeginStep("client pushes the throttle handle to take off");
            _host.Command(CoopTestSteps.COMMAND_PUSH_THROTTLE);
            yield return _host.WaitForCoroutine(IsThrottleHeldByClient, "the throttle handle held by the client's key on the server");
            yield return _host.WaitForReportCoroutine(CoopTestSteps.THROTTLE_PUSHED);
            Assert.AreNotEqual(ShipRunPhase.Build, Model.Phase, "The client's push on the throttle handle did not take off on the host.");
            AssertThrottleOnBoth(FULL_THROTTLE, "after the client's push to take off");
            Assert.IsTrue(string.IsNullOrEmpty(Throttle.HolderKey), "The handle is still held on the server after the client let go.");
        }

        private IEnumerator WatchClientStopAndGoCoroutine() {
            StartSpeedSample();
            yield return _host.WaitForCoroutine(IsShipCruising, "the ship flying at the client's throttle");
            yield return _host.WaitForTurnDoneCoroutine();

            _host.BeginStep("client pulls the throttle handle to Stop in cruise");
            yield return _host.CommandAndWaitCoroutine(CoopTestSteps.COMMAND_PULL_THROTTLE, CoopTestSteps.THROTTLE_PULLED);
            AssertThrottleOnBoth(0f, "after the client's pull to Stop in cruise");
            yield return WaitForShipStoppedCoroutine();
            Vector3 stoppedAt = Ship.transform.position;
            yield return _stayStopped;
            Assert.Less(HorizontalDistance(stoppedAt, Ship.transform.position) / STAY_STOPPED_SECONDS, STOPPED_SPEED, "The ship did not stay still at the client's Stop.");
            Assert.AreEqual(ShipRunPhase.Cruise, Model.Phase, "The stopped ship left the cruise.");

            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_REPORT_STOPPED, CoopFlightSteps.SHIP_STOPPED);
            CoopObservation host = CoopFlightObserver.CaptureFlight(CoopFlightSteps.SHIP_STOPPED);
            float distance = Vector3.Distance(host.ShipFromDestination, _host.LastReport.ShipFromDestination);
            Assert.Less(distance, REST_POSE_TOLERANCE, $"The stopped ship stands {distance:0.00} m apart on the host and the client.\nhost: {host.DescribeFlight()}\nclient: {_host.LastReport.DescribeFlight()}");
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(host.ShipYaw, _host.LastReport.ShipYaw)), REST_YAW_TOLERANCE, $"The stopped ship faces another way on the client: host {host.ShipYaw:0.0}, client {_host.LastReport.ShipYaw:0.0}.");

            _host.BeginStep("client pushes the throttle handle to Full in cruise");
            yield return _host.CommandAndWaitCoroutine(CoopTestSteps.COMMAND_PUSH_THROTTLE_IN_CRUISE, CoopTestSteps.THROTTLE_PUSHED_IN_CRUISE);
            AssertThrottleOnBoth(FULL_THROTTLE, "after the client's push to Full in cruise");
            StartSpeedSample();
            yield return _host.WaitForCoroutine(IsShipCruising, "the ship flying again at the client's Full");
            _host.Flight.FaceDestination();
        }

        // The seat is keyed by the client's player key on the server, and the host sees the client's body in it.
        private IEnumerator WatchClientTakeHelmCoroutine() {
            _host.BeginStep("client takes the helm at the station");
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_TAKE_HELM, CoopTestSteps.SEATED_AT_HELM);
            Assert.IsTrue(_host.LastReport.IsHelmPilot, "The client does not steer from the helm on its own peer.");
            ShipSocket helm = CoopHelm.FindSocket(Ship);
            ShipRider client = _host.ClientRider();
            yield return _host.WaitForCoroutine(() => helm.IsHeldBy(client.Key), "the helm held by the client's key on the server");
            Assert.IsTrue(client.IsSeated, "The client's rider is not seated on the server.");
            Assert.IsTrue(CoopHelm.FindStation(Ship).HasPilot, "The host's helm station sees no pilot.");
            Assert.IsFalse(CoopHelm.FindStation(Ship).IsSteeredByLocalPlayer, "The host's helm station takes the client for the host.");
            float sitDistance = Vector3.Distance(client.transform.position, helm.Seat.SitPoint.position);
            Assert.Less(sitDistance, SIT_POINT_TOLERANCE, $"The host does not see the client at the helm's seat: {sitDistance:0.00} m away.");
        }

        private IEnumerator WatchClientSteerCoroutine() {
            _host.BeginStep("client steers right and left from the helm");
            _cruiseStartYaw = Ship.transform.eulerAngles.y;
            _mostRightDegrees = 0f;
            _host.Command(CoopFlightSteps.COMMAND_STEER);
            yield return _host.WaitForReportCoroutine(CoopFlightSteps.STEERED, WatchTurn);
            float endDegrees = TurnedDegrees();
            Assert.Greater(_mostRightDegrees, MIN_TURN_DEGREES, $"The ship did not turn right under the client pilot's D: at most {_mostRightDegrees:0.0} degrees.");
            Assert.Less(endDegrees, _mostRightDegrees - MIN_TURN_DEGREES, $"The ship did not turn back left under the client pilot's A: {_mostRightDegrees:0.0} degrees right at most, {endDegrees:0.0} at the end.");
            Debug.Log($"[CoopTest] the client's helm: {_mostRightDegrees:0.0} degrees right, then {endDegrees:0.0} degrees from the start heading");
        }

        private IEnumerator WatchClientTryThrottleFromSeatCoroutine() {
            _host.BeginStep("seated client tries the throttle handle");
            _hasClientHeldThrottle = false;
            _host.Command(CoopFlightSteps.COMMAND_TRY_THROTTLE_SEATED);
            yield return _host.WaitForReportCoroutine(CoopFlightSteps.THROTTLE_REFUSED, WatchThrottleHolder);
            Assert.IsFalse(_hasClientHeldThrottle, "The server gave the throttle handle to the seated client.");
            Assert.IsTrue(string.IsNullOrEmpty(Throttle.HolderKey), $"The throttle handle is held by '{Throttle.HolderKey}' after the seated client's try.");
            AssertThrottleOnBoth(FULL_THROTTLE, "after the seated client's try");
            Assert.IsTrue(CoopHelm.FindStation(Ship).HasPilot, "The client left the helm while trying the throttle.");
        }

        private IEnumerator CompareFlyingPoseCoroutine() {
            _host.BeginStep("host and client compare the flying ship's pose");
            _host.Command(CoopFlightSteps.COMMAND_REPORT_POSE);
            yield return _host.WaitForReportCoroutine(CoopFlightSteps.POSE, RecordPose);
            CoopObservation client = _host.LastReport;
            Assert.IsTrue(_poseTrack.TryFindNearest(client.ShipFromDestination, client.ShipYaw, out float distance, out float yawDifference), "The host recorded no ship pose.");
            Assert.Less(distance, TRACK_POSE_TOLERANCE, $"The client shows the flying ship {distance:0.00} m off the host's track: {client.DescribeFlight()}");
            Assert.Less(yawDifference, TRACK_YAW_TOLERANCE, $"The client shows the flying ship turned {yawDifference:0.0} degrees off the host's heading there.");
            Debug.Log($"[CoopTest] the client's flying ship: {distance:0.00} m and {yawDifference:0.00} degrees from the host's track");
        }

        private IEnumerator WatchClientStandUpCoroutine() {
            _host.BeginStep("client stands up from the helm");
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_STAND_UP, CoopFlightSteps.STOOD_UP);
            Assert.IsFalse(_host.LastReport.IsSeated, "The client still sees itself seated after its jump.");
            Assert.IsTrue(_host.LastReport.IsRiding, "The client does not ride the ship after standing up.");
            Assert.IsFalse(_host.ClientRider().IsSeated, "The client's rider is still seated on the server.");
            Assert.IsFalse(CoopHelm.FindSocket(Ship).HasOccupant, "The helm is still taken after the client stood up.");
            Assert.IsFalse(CoopHelm.FindStation(Ship).HasPilot, "The host's helm station still has a pilot after the client stood up.");
            Assert.IsTrue(_host.IsClientOnDeckOnServer(), $"The client is not on the deck after standing up: {_host.ClientServerPosition()}.");
        }

        private void WatchTurn() {
            RecordPose();
            _mostRightDegrees = Mathf.Max(_mostRightDegrees, TurnedDegrees());
        }

        private void WatchThrottleHolder() {
            RecordPose();
            _hasClientHeldThrottle |= IsThrottleHeldByClient();
        }

        private void RecordPose() =>
            _poseTrack.Record(Ship, Model);

        private float TurnedDegrees() =>
            Mathf.DeltaAngle(_cruiseStartYaw, Ship.transform.eulerAngles.y);

        // False while the client has no player on the server: the wait then fails on the client's own report.
        private bool IsThrottleHeldByClient() =>
            _host.IsClientPlayerSpawned() && Throttle.HolderKey == _host.ClientRider().Key.Id;

        private void AssertThrottleOnBoth(float expected, string when) {
            Assert.AreEqual(expected, Throttle.Position, THROTTLE_TOLERANCE, $"The host sees the throttle at {Throttle.Position:0.000} {when}.");
            Assert.AreEqual(Throttle.Position, _host.LastReport.ThrottlePosition, THROTTLE_TOLERANCE, $"Host and client see the throttle at different positions {when}.");
        }

        private IEnumerator WaitForShipStoppedCoroutine() {
            float deadline = Time.time + STOP_TIMEOUT_SECONDS;
            while (true) {
                Vector3 start = Ship.transform.position;
                float startTime = Time.time;
                yield return _speedSample;
                float speed = HorizontalDistance(start, Ship.transform.position) / (Time.time - startTime);
                if (speed < STOPPED_SPEED)
                    yield break;

                if (Time.time > deadline)
                    Assert.Fail($"The ship did not stop within {STOP_TIMEOUT_SECONDS} s of the client's pull to Stop: it moves at {speed:0.00} m/s, the server's throttle at {Throttle.Position:0.000}.");
            }
        }

        private void StartSpeedSample() {
            _speedSampleStart = Ship.transform.position;
            _speedSampleStartTime = Time.time;
        }

        // The ship's horizontal speed over the last sample on the host, restarted every sample.
        private bool IsShipCruising() {
            float elapsed = Time.time - _speedSampleStartTime;
            if (elapsed < SPEED_SAMPLE_SECONDS)
                return false;

            float speed = HorizontalDistance(_speedSampleStart, Ship.transform.position) / elapsed;
            StartSpeedSample();
            return speed >= MIN_CRUISE_SPEED;
        }

        private static float HorizontalDistance(Vector3 from, Vector3 to) {
            Vector3 moved = to - from;
            moved.y = 0f;
            return moved.magnitude;
        }
    }
}
#endif
