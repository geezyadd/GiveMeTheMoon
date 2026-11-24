#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.PlayerReplicaModule;
using Features.PlayerReplicaModule.Receiving;
using Features.ShipModule.Scripts;
using NUnit.Framework;
using Tests.Coop.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Coop.Host {
    // Smoothness of a client's copy on the flying deck with a bad link, a real second process: in the cruise the client
    // sends with the latency profile "150 ms, jitter 20, loss 5%" and walks short legs on the deck; the host's copy of
    // the client never snaps and stays close to the owner's packets (BodyStateReceiveStats of the server's copy).
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopClientDeckWalkLagTest {
        private const int TEST_TIMEOUT_MS = 240000;
        private const int LOOP = 1;
        // The copy's distance to the owner's packet (at the copy's delay) over the walk, metres on average.
        private const double MAX_AVERAGE_ERROR_METERS = 0.3d;
        // The legs are 0.8 s each at walking speed: a client that walked much less did not really walk.
        private const float MIN_WALK_METERS = 4f;

        private CoopFlightHostHarness _host;

        private ShipBase Ship => _host.Ship;

        [SetUp]
        public void SetUp() =>
            _host = new CoopFlightHostHarness();

        [TearDown]
        public void TearDown() =>
            _host?.Dispose();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenClientWalksOnTheFlyingDeckWithLag_ThenItsCopyOnTheHostNeverSnapsAndStaysClose() {
            // LoopSmokeErrorLog fails the test with the full list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return _host.HostAndLetClientInCoroutine(CoopFlightSteps.SCENARIO_DECK_WALK_LAG);
            yield return _host.StartAndBoardCoroutine();
            _host.Flight.PullLaunchLever(LOOP);
            yield return _host.WaitForCruiseCoroutine(LOOP, _host.FaceAwayFromStation);
            yield return WalkWithLagCoroutine();
            yield return _host.FinishCoroutine();
        }

        private IEnumerator WalkWithLagCoroutine() {
            _host.BeginStep($"client walks on the flying deck with '{CoopFlightSteps.DECK_WALK_LAG_PROFILE}'");
            BodyStateReceiveStats stats = _host.ClientRider().GetComponent<BodyStateChannel>().ReceiveStats;
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_WALK_WITH_LAG, CoopFlightSteps.WALKING_WITH_LAG);
            // The client's landing on the deck and the start of the lag are before this report: the walk starts here.
            stats.TakeReport(Time.unscaledTimeAsDouble);
            yield return _host.WaitForReportCoroutine(CoopFlightSteps.WALKED_WITH_LAG);
            BodyStateReceiveReport walk = stats.TakeReport(Time.unscaledTimeAsDouble);
            CoopObservation client = _host.LastReport;
            Debug.Log($"[CoopTest] the client's copy while it walked {client.WalkedMeters:0.0} m with lag: {walk.Snaps} snaps, "
                      + $"error {walk.AverageErrorMeters:0.000} m on average, {walk.MaxErrorMeters:0.000} m at most, "
                      + $"{walk.Accepted} packets ({walk.PacketsPerSecond:0.0}/s), {walk.Dropped} dropped as old, "
                      + $"age {walk.AverageAgeSeconds * 1000d:0} ms on average");

            Assert.GreaterOrEqual(client.WalkedMeters, MIN_WALK_METERS, "The client hardly walked on the deck.");
            Assert.IsTrue(client.IsAboard, "The client is not aboard after the walk with lag.");
            Assert.IsTrue(Ship.Riders.IsAboard(_host.ClientRider()), "The client's copy is not aboard the ship on the host after the walk.");
            Assert.IsTrue(_host.IsClientOnDeckOnServer(), $"The client's copy is not on the deck on the host after the walk: {_host.ClientServerPosition()}.");
            Assert.Greater(walk.Accepted, 0, "The host got no state packets of the client during the walk.");
            Assert.AreEqual(0, walk.Snaps, "The client's copy on the host snapped during the walk with lag.");
            Assert.Less(walk.AverageErrorMeters, MAX_AVERAGE_ERROR_METERS, "The client's copy on the host was too far from its owner on average.");
        }
    }
}
#endif
