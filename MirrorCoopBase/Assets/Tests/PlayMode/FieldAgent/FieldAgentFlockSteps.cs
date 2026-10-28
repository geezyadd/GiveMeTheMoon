#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.FieldAgentModule.Scripts.Debug;
using Features.FieldAgentModule.Scripts.Spawn;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.FieldAgent {
    // The flock steps the field agent tests share: the debug window's spawn button clicked in a slow cruise, and the
    // wait until every agent of the flock orbits the ship, checking the rocks all the way.
    public sealed class FieldAgentFlockSteps {
        public const int FLOCK_SIZE = 20;
        public const string LOG_PREFIX = "[FieldAgents] ";

        private const string SPAWN_LABEL = "Spawn test agents (N)";
        private const float ORBIT_TIMEOUT_SECONDS = 90f;
        private const float PROGRESS_LOG_SECONDS = 5f;
        // A slow cruise: the flock chases a moving ship, and the ship meets few rocks while the test waits.
        private const float CRUISE_THROTTLE = 0.3f;

        private readonly FieldAgentTestFlight _flight;

        public FieldAgentFlockDebugModel Flock { get; private set; }

        public FieldAgentFlockSteps(FieldAgentTestFlight flight) =>
            _flight = flight;

        public IEnumerator SpawnFlockInSlowCruiseCoroutine() {
            yield return _flight.FlyIntoEndlessCruiseCoroutine();
            _flight.SetThrottle(CRUISE_THROTTLE);
            Flock = SessionQueries.Resolve<FieldAgentFlockDebugModel>();
            yield return _flight.ClickDebugActionCoroutine(SPAWN_LABEL);
            Assert.AreEqual(FLOCK_SIZE, Flock.FlockIds.Count, "The debug button did not spawn the default flock.");
            Assert.AreNotEqual(FieldAgentSpawnCover.None, Flock.LastSpawnCover);
            Debug.Log($"{LOG_PREFIX}flock hidden by {Flock.LastSpawnCover}, {DescribeFlockDistance()} from the ship");
        }

        // screenshot: taken once half the flock orbits; null takes none.
        public IEnumerator WaitForOrbitCoroutine(string screenshot) {
            float deadline = Time.time + ORBIT_TIMEOUT_SECONDS;
            float nextLog = Time.time;
            while (Flock.OrbitingCount < FLOCK_SIZE) {
                if (Time.time >= nextLog) {
                    nextLog = Time.time + PROGRESS_LOG_SECONDS;
                    Debug.Log($"{LOG_PREFIX}{Flock.OrbitingCount} of {Flock.FlockIds.Count} orbiting, {DescribeFlockDistance()} from the ship");
                }

                Assert.Less(Time.time, deadline, $"Only {Flock.OrbitingCount} of {FLOCK_SIZE} agents reached the ship ({DescribeFlockDistance()}).");
                _flight.CheckRocks(Flock.FlockIds);
                Assert.AreEqual(0, _flight.RockHits, $"An agent flew into a rock: {_flight.FirstRockHit}.");
                if (screenshot != null && Flock.OrbitingCount >= FLOCK_SIZE / 2) {
                    yield return FieldAgentTestFlight.CaptureCoroutine(screenshot);
                    screenshot = null;
                }

                yield return null;
            }

            Debug.Log($"{LOG_PREFIX}flock orbiting after passing {_flight.RocksPassedNear} rocks within look-ahead, {_flight.RockHits} hits");
        }

        public string DescribeFlockDistance() {
            float nearest = float.MaxValue;
            float farthest = 0f;
            foreach (int id in Flock.FlockIds) {
                if (_flight.Control.TryGetWorldPosition(id, out Vector3 position) == false)
                    continue;

                float distance = Vector3.Distance(position, _flight.Ship.transform.position);
                nearest = Mathf.Min(nearest, distance);
                farthest = Mathf.Max(farthest, distance);
            }

            return $"{nearest:0} - {farthest:0} m";
        }
    }
}
#endif
