#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Client;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Debug;
using Features.FieldAgentModule.Scripts.Network.Generated;
using Features.FieldAgentModule.Scripts.Spawn;
using Features.FlightFieldModule.Contracts;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode.FieldAgent {
    // In the first cruise the host clicks "Spawn test agents (N)" in the debug window: 20 agents appear at a point no
    // player sees, fly to the ship through the field without ever coming into a rock sphere, and orbit it; the host
    // shows all of them. Then 30 more make 50 for the measurement of the server tick and the batch size.
    public sealed class FieldAgentFlockTest {
        private const int TEST_TIMEOUT_MS = 240000;
        private const string SPAWN_LABEL = "Spawn test agents (N)";
        private const int FLOCK_SIZE = 20;
        private const int MEASURED_AGENTS = 50;
        private const float ORBIT_TIMEOUT_SECONDS = 90f;
        private const float ORBIT_CHECK_SECONDS = 6f;
        private const float MEASURE_SECONDS = 4f;
        // Orbiting agents stay within this band around the orbit radius, measured flat from the ship.
        private const float ORBIT_BAND_METERS = 10f;
        private const string LOG_PREFIX = "[FieldAgents] ";
        // A slow cruise: the flock chases a moving ship, and the ship meets few rocks while the test waits.
        private const float CRUISE_THROTTLE = 0.3f;
        private const float PROGRESS_LOG_SECONDS = 5f;
        private const float STOP_SECONDS = 3f;
        private const float ROCK_SEARCH_METERS = 400f;
        // Small enough that the ship, its orbit radius away from the middle, stays clear of it.
        private const float MIN_ORBIT_ROCK_RADIUS = 3f;
        private const float MAX_ORBIT_ROCK_RADIUS = 8f;
        private const float CLOSE_METERS = 6f;
        private const int MIN_CLOSE_PASSES = 300;

        private FieldAgentTestFlight _flight;
        private IFieldAgentFlockDebug _flock;

        [SetUp]
        public void SetUp() =>
            _flight = new FieldAgentTestFlight();

        [TearDown]
        public void TearDown() =>
            _flight.Dispose();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenHostSpawnsTestAgentsInCruise_AndTheyFlyToTheShip_ThenTheyMissEveryRockAndOrbitIt() {
            LogAssert.ignoreFailingMessages = true;
            yield return _flight.FlyIntoEndlessCruiseCoroutine();
            _flight.SetThrottle(CRUISE_THROTTLE);
            _flock = SessionQueries.Resolve<IFieldAgentFlockDebug>();

            yield return _flight.ClickDebugActionCoroutine(SPAWN_LABEL);
            Assert.AreEqual(FLOCK_SIZE, _flock.FlockIds.Count, "The debug button did not spawn the default flock.");
            Assert.AreNotEqual(FieldAgentSpawnCover.None, _flock.LastSpawnCover);
            Debug.Log($"{LOG_PREFIX}flock hidden by {_flock.LastSpawnCover}, {DescribeFlockDistance()} from the ship");

            yield return WaitForOrbitCoroutine();
            yield return CheckOrbitCoroutine();
            yield return FieldAgentTestFlight.CaptureCoroutine("flock-orbiting-ship.png");
            yield return OrbitAcrossARockCoroutine();
            _flight.SetThrottle(CRUISE_THROTTLE);
            yield return MeasureFiftyCoroutine();
            yield return _flight.LandAndLeaveCoroutine();
        }

        private IEnumerator WaitForOrbitCoroutine() {
            float deadline = Time.time + ORBIT_TIMEOUT_SECONDS;
            bool isScreenshotTaken = false;
            float nextLog = Time.time;
            while (_flock.OrbitingCount < FLOCK_SIZE) {
                if (Time.time >= nextLog) {
                    nextLog = Time.time + PROGRESS_LOG_SECONDS;
                    Debug.Log($"{LOG_PREFIX}{_flock.OrbitingCount} orbiting, ship phase {_flight.Session.Model.Phase}, {_flock.FlockIds.Count} in the flock");
                    foreach (int id in _flock.FlockIds.Count > 0 ? new[] { _flock.FlockIds[0], _flock.FlockIds[_flock.FlockIds.Count - 1] } : new int[0])
                        Debug.Log(LOG_PREFIX + _flight.DescribeAgent(id));
                }

                Assert.Less(Time.time, deadline, $"Only {_flock.OrbitingCount} of {FLOCK_SIZE} agents reached the ship ({DescribeFlockDistance()}).");
                _flight.CheckRocks(_flock.FlockIds);
                Assert.AreEqual(0, _flight.RockHits, $"An agent flew into a rock: {_flight.FirstRockHit}.");
                if (isScreenshotTaken == false && _flock.OrbitingCount >= FLOCK_SIZE / 2) {
                    isScreenshotTaken = true;
                    yield return FieldAgentTestFlight.CaptureCoroutine("flock-arriving-through-field.png");
                }

                yield return null;
            }

            Debug.Log($"{LOG_PREFIX}flock orbiting after passing {_flight.RocksPassedNear} rocks within look-ahead, {_flight.RockHits} hits");
        }

        private IEnumerator CheckOrbitCoroutine() {
            IFieldAgentShownAgents shown = SessionQueries.Resolve<IFieldAgentShownAgents>();
            float end = Time.time + ORBIT_CHECK_SECONDS;
            while (Time.time < end) {
                _flight.CheckRocks(_flock.FlockIds);
                yield return null;
            }

            Assert.AreEqual(0, _flight.RockHits, $"An agent flew into a rock while orbiting: {_flight.FirstRockHit}.");
            Assert.AreEqual(FLOCK_SIZE, shown.ShownCount, "The host does not show every agent.");
            float radius = SessionQueries.Resolve<FieldAgentConfiguration>().DebugOrbitRadius;
            foreach (int id in _flock.FlockIds) {
                Assert.IsTrue(_flight.Control.TryGetWorldPosition(id, out Vector3 position));
                Vector3 flat = position - _flight.Ship.transform.position;
                flat.y = 0f;
                Assert.AreEqual(radius, flat.magnitude, ORBIT_BAND_METERS, $"Agent {id} is not on the orbit round the ship.");
                Assert.IsTrue(shown.TryGetShownWorldPosition(id, out Vector3 _), $"The host does not show agent {id}.");
            }
        }

        // The stopped ship is put beside a rock so the orbit circle runs through the rock's sphere: every agent has to
        // bend round it on every lap.
        private IEnumerator OrbitAcrossARockCoroutine() {
            _flight.SetThrottle(0f);
            yield return new WaitForSeconds(STOP_SECONDS);
            FieldRockDescription rock = FindRockForOrbit();
            FieldAgentConfiguration configuration = SessionQueries.Resolve<FieldAgentConfiguration>();
            Vector3 away = _flight.Ship.transform.position - rock.Center;
            away.y = 0f;
            away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward;
            Vector3 shipTarget = rock.Center + away * configuration.DebugOrbitRadius + Vector3.down * configuration.DebugOrbitHeight;
            _flight.Ship.FlightControlDebug.DebugMove(shipTarget - _flight.Ship.transform.position);
            yield return null;

            float deadline = Time.time + ORBIT_TIMEOUT_SECONDS;
            int closePasses = 0;
            while (closePasses < MIN_CLOSE_PASSES) {
                Assert.Less(Time.time, deadline, $"The flock never orbited close by rock {rock.Id} ({rock.Diameter:0.0} m): {closePasses} close passes.");
                _flight.CheckRocks(_flock.FlockIds);
                Assert.AreEqual(0, _flight.RockHits, $"An agent flew into the rock on the orbit: {_flight.FirstRockHit}.");
                closePasses += CountAgentsCloseTo(rock);
                yield return null;
            }

            yield return FieldAgentTestFlight.CaptureCoroutine("flock-orbit-round-rock.png");
            Assert.AreEqual(0, _flight.RockPushOuts, "The steering let agents into rock spheres: the server had to push them out.");
            Debug.Log($"{LOG_PREFIX}orbit through rock {rock.Id} ({rock.Diameter:0.0} m): {closePasses} agent frames within {CLOSE_METERS} m of its surface, {_flight.RockHits} hits, {_flight.RockPushOuts} push-outs");
        }

        private FieldRockDescription FindRockForOrbit() {
            List<FieldRockDescription> rocks = new();
            Vector3 ship = _flight.Ship.transform.position;
            _flight.Rocks.FindInSphere(ship, ROCK_SEARCH_METERS, rocks);
            FieldRockDescription best = default;
            float bestDistance = float.MaxValue;
            foreach (FieldRockDescription rock in rocks) {
                float distance = Vector3.Distance(rock.Center, ship);
                if (rock.Radius < MIN_ORBIT_ROCK_RADIUS || rock.Radius > MAX_ORBIT_ROCK_RADIUS || distance >= bestDistance)
                    continue;

                best = rock;
                bestDistance = distance;
            }

            Assert.Less(bestDistance, float.MaxValue, $"No rock of {MIN_ORBIT_ROCK_RADIUS}-{MAX_ORBIT_ROCK_RADIUS} m radius within {ROCK_SEARCH_METERS} m of the ship.");
            return best;
        }

        private int CountAgentsCloseTo(FieldRockDescription rock) {
            int close = 0;
            foreach (int id in _flock.FlockIds) {
                if (_flight.Control.TryGetWorldPosition(id, out Vector3 position) && Vector3.Distance(position, rock.Center) - rock.Radius < CLOSE_METERS)
                    close++;
            }

            return close;
        }

        private IEnumerator MeasureFiftyCoroutine() {
            _flock.DebugSpawnTestFlock(MEASURED_AGENTS - FLOCK_SIZE);
            Assert.AreEqual(MEASURED_AGENTS, _flight.Control.Count);
            yield return new WaitForSeconds(1f);
            FieldAgentDiagnosticsModel diagnostics = SessionQueries.Resolve<FieldAgentDiagnosticsModel>();
            diagnostics.ResetMeasurements();
            float start = Time.time;
            List<int> all = new();
            while (Time.time - start < MEASURE_SECONDS) {
                all.Clear();
                all.AddRange(_flock.FlockIds);
                _flight.CheckRocks(all);
                yield return null;
            }

            Assert.AreEqual(0, _flight.RockHits, $"An agent flew into a rock with 50 agents: {_flight.FirstRockHit}.");
            Assert.AreEqual(0, _flight.RockPushOuts, "The steering let agents into rock spheres: the server had to push them out.");
            int batchBytes = MeasureBatchBytes();
            float batchesPerSecond = diagnostics.BatchesSent / (Time.time - start);
            Debug.Log($"{LOG_PREFIX}{MEASURED_AGENTS} agents: server tick avg {diagnostics.AverageTickMilliseconds:0.000} ms, max {diagnostics.MaxTickMilliseconds:0.000} ms; batch {batchBytes} B x {batchesPerSecond:0.0}/s = {batchBytes * batchesPerSecond / 1024f:0.0} KiB/s per client (payload, before Mirror's message header)");
        }

        private static int MeasureBatchBytes() {
            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            writer.Write(SessionQueries.Resolve<IReadOnlyFieldAgentsModel>().Frame);
            return writer.Position;
        }

        private string DescribeFlockDistance() {
            float nearest = float.MaxValue;
            float farthest = 0f;
            foreach (int id in _flock.FlockIds) {
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
