#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using Features.DebugModule.Contracts;
using Features.FieldAgentModule.Scripts.Debug;
using Features.ShipModule.Scripts.Debug;
using NUnit.Framework;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Coop.Host {
    // The host spawns the debug flock of field agents in the cruise; once it orbits the ship, the real client process
    // shows the same agents (same ids) at the same places as the host at the same network time, within a tolerance.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopClientFieldAgentsTest {
        private const int TEST_TIMEOUT_MS = 240000;
        private const int LOOP = 1;
        private const string SPAWN_LABEL = "Spawn test agents (N)";
        // A slow cruise: the stopped flock still chases a moving ship, and the ship meets few rocks meanwhile.
        private const float CRUISE_THROTTLE = 0.3f;
        // The two clocks differ by a few milliseconds; an orbiting agent moves up to about 25 m/s.
        private const float POSITION_TOLERANCE_METERS = 2f;
        // The host keeps its own views this long to find the one at the client's network time.
        private const double TRACK_SECONDS = 10d;
        private const double MAX_TIME_GAP_SECONDS = 0.05d;

        private readonly List<CoopFieldAgentView> _hostTrack = new();
        private CoopFlightHostHarness _host;

        [SetUp]
        public void SetUp() =>
            _host = new CoopFlightHostHarness();

        [TearDown]
        public void TearDown() =>
            _host?.Dispose();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenHostSpawnsAFlockInCruise_AndItOrbitsTheShip_ThenTheClientShowsTheSameAgentsAtTheSamePlaces() {
            // LoopSmokeErrorLog fails the test with the full list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return _host.HostAndLetClientInCoroutine(CoopFlightSteps.SCENARIO_FIELD_AGENTS);
            yield return _host.StartAndBoardCoroutine();
            _host.Flight.PullLaunchLever(LOOP);
            yield return _host.WaitForCruiseCoroutine(LOOP, _host.FaceAwayFromStation);
            yield return _host.WaitForTurnDoneCoroutine();
            SessionQueries.Resolve<IShipThrottleDebug>().DebugSetThrottle(CRUISE_THROTTLE);
            yield return SpawnFlockAndWaitForOrbitCoroutine();
            yield return CompareClientViewCoroutine();
            _host.Flight.FaceDestination();
            SessionQueries.Resolve<IShipThrottleDebug>().DebugSetThrottle(1f);
            yield return _host.WaitForLandingCoroutine(LOOP);
            yield return _host.FinishCoroutine();
        }

        private IEnumerator SpawnFlockAndWaitForOrbitCoroutine() {
            _host.BeginStep("host spawns the test flock through its debug window");
            DebugWindowClicking clicking = new();
            yield return clicking.ClickCoroutine(DebugSections.AGENTS, SPAWN_LABEL);
            Assert.IsNull(clicking.Failure, $"Debug window \"{SPAWN_LABEL}\": {clicking.Failure}.");
            FieldAgentFlockDebugModel flock = SessionQueries.Resolve<FieldAgentFlockDebugModel>();
            Assert.AreEqual(CoopFlightSteps.FIELD_AGENT_COUNT, flock.FlockIds.Count, "The host's debug button did not spawn the flock.");
            yield return _host.WaitForCoroutine(() => flock.OrbitingCount == CoopFlightSteps.FIELD_AGENT_COUNT, "the flock orbiting the ship");
        }

        private IEnumerator CompareClientViewCoroutine() {
            _host.BeginStep("client reports the agents it shows");
            _host.Command(CoopFlightSteps.COMMAND_REPORT_AGENTS);
            yield return _host.WaitForReportCoroutine(CoopFlightSteps.AGENTS_SEEN, RecordHostView);
            CoopFieldAgentView client = CoopFieldAgentView.Parse(_host.LastReport.Message);
            CoopFieldAgentView host = FindHostViewAt(client.NetworkTime);
            Assert.AreEqual(host.Positions.Count, client.Positions.Count, "The client shows another number of agents than the host.");
            float worst = 0f;
            foreach (KeyValuePair<int, Vector3> agent in client.Positions) {
                Assert.IsTrue(host.Positions.TryGetValue(agent.Key, out Vector3 hostPosition), $"The client shows agent {agent.Key}, the host does not.");
                worst = Mathf.Max(worst, Vector3.Distance(hostPosition, agent.Value));
            }

            Assert.Less(worst, POSITION_TOLERANCE_METERS, $"An agent is shown {worst:0.00} m apart on the host and the client.");
            Debug.Log($"[CoopTest] the client shows the {client.Positions.Count} agents of the host, at most {worst:0.00} m apart (network times {host.NetworkTime:0.000} / {client.NetworkTime:0.000}); client screenshot {_host.LastReport.Screenshot}");
        }

        private void RecordHostView() {
            _hostTrack.Add(CoopFieldAgentView.Capture());
            while (_hostTrack.Count > 0 && _hostTrack[^1].NetworkTime - _hostTrack[0].NetworkTime > TRACK_SECONDS)
                _hostTrack.RemoveAt(0);
        }

        private CoopFieldAgentView FindHostViewAt(double networkTime) {
            CoopFieldAgentView closest = null;
            double gap = double.MaxValue;
            foreach (CoopFieldAgentView view in _hostTrack) {
                double viewGap = System.Math.Abs(view.NetworkTime - networkTime);
                if (viewGap >= gap)
                    continue;

                gap = viewGap;
                closest = view;
            }

            Assert.IsNotNull(closest, "The host recorded no view of its own agents.");
            Assert.Less(gap, MAX_TIME_GAP_SECONDS, $"The host has no view near the client's network time {networkTime:0.000}.");
            return closest;
        }
    }
}
#endif
