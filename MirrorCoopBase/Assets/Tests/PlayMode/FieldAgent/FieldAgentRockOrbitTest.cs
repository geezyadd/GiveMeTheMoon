#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FlightFieldModule.Contracts;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode.FieldAgent {
    // Once the debug flock orbits the ship, the stopped ship is put beside a rock so the orbit circle runs through the
    // rock's sphere: every agent bends round it lap after lap, never comes into it and never needs the server's
    // last-resort push.
    public sealed class FieldAgentRockOrbitTest {
        private const int TEST_TIMEOUT_MS = 240000;
        private const float STOP_SECONDS = 3f;
        private const float ROCK_TIMEOUT_SECONDS = 90f;
        private const float ROCK_SEARCH_METERS = 400f;
        // Small enough that the ship, its orbit radius away from the middle, stays clear of it.
        private const float MIN_ORBIT_ROCK_RADIUS = 3f;
        private const float MAX_ORBIT_ROCK_RADIUS = 8f;
        private const float CLOSE_METERS = 6f;
        private const int MIN_CLOSE_PASSES = 300;
        private const float MIN_AWAY_SQR = 0.01f;

        private FieldAgentTestFlight _flight;
        private FieldAgentFlockSteps _steps;

        [SetUp]
        public void SetUp() {
            _flight = new FieldAgentTestFlight();
            _steps = new FieldAgentFlockSteps(_flight);
        }

        [TearDown]
        public void TearDown() =>
            _flight.Dispose();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenTheOrbitRunsThroughARock_AndTheFlockCirclesTheShip_ThenEveryAgentBendsRoundTheRock() {
            LogAssert.ignoreFailingMessages = true;
            yield return _steps.SpawnFlockInSlowCruiseCoroutine();
            yield return _steps.WaitForOrbitCoroutine(null);
            _flight.SetThrottle(0f);
            yield return new WaitForSeconds(STOP_SECONDS);
            FieldRockDescription rock = FindRockForOrbit();
            PutShipBesideRock(rock);
            yield return null;
            yield return CountCloseOrbitsCoroutine(rock);
            yield return _flight.LandAndLeaveCoroutine();
        }

        private void PutShipBesideRock(FieldRockDescription rock) {
            FieldAgentConfiguration configuration = SessionQueries.Resolve<FieldAgentConfiguration>();
            Vector3 away = _flight.Ship.transform.position - rock.Center;
            away.y = 0f;
            away = away.sqrMagnitude > MIN_AWAY_SQR ? away.normalized : Vector3.forward;
            Vector3 shipTarget = rock.Center + away * configuration.DebugOrbitRadius + Vector3.down * configuration.DebugOrbitHeight;
            _flight.Ship.FlightControlDebug.DebugMove(shipTarget - _flight.Ship.transform.position);
        }

        private IEnumerator CountCloseOrbitsCoroutine(FieldRockDescription rock) {
            float deadline = Time.time + ROCK_TIMEOUT_SECONDS;
            int closePasses = 0;
            while (closePasses < MIN_CLOSE_PASSES) {
                Assert.Less(Time.time, deadline, $"The flock never orbited close by rock {rock.Id} ({rock.Diameter:0.0} m): {closePasses} close passes.");
                _flight.CheckRocks(_steps.Flock.FlockIds);
                Assert.AreEqual(0, _flight.RockHits, $"An agent flew into the rock on the orbit: {_flight.FirstRockHit}.");
                closePasses += CountAgentsCloseTo(rock);
                yield return null;
            }

            yield return FieldAgentTestFlight.CaptureCoroutine("flock-orbit-round-rock.png");
            Assert.AreEqual(0, _flight.RockPushOuts, "The steering let agents into rock spheres: the server had to push them out.");
            Debug.Log($"{FieldAgentFlockSteps.LOG_PREFIX}orbit through rock {rock.Id} ({rock.Diameter:0.0} m): {closePasses} agent frames within {CLOSE_METERS} m of its surface, {_flight.RockHits} hits, {_flight.RockPushOuts} push-outs");
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
            foreach (int id in _steps.Flock.FlockIds) {
                if (_flight.Control.TryGetWorldPosition(id, out Vector3 position) && Vector3.Distance(position, rock.Center) - rock.Radius < CLOSE_METERS)
                    close++;
            }

            return close;
        }
    }
}
#endif
