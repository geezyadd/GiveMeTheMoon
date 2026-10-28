#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.FieldAgentModule.Scripts.Client;
using Features.FieldAgentModule.Scripts.Configuration;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode.FieldAgent {
    // In the first cruise the host clicks "Spawn test agents (N)" in the debug window: 20 agents appear at a point no
    // player sees, fly to the ship through the field without ever coming into a rock sphere, and orbit it; the host
    // shows all of them.
    public sealed class FieldAgentFlockTest {
        private const int TEST_TIMEOUT_MS = 240000;
        private const float ORBIT_CHECK_SECONDS = 6f;
        // Orbiting agents stay within this band around the orbit radius, measured flat from the ship.
        private const float ORBIT_BAND_METERS = 10f;

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
        public IEnumerator WhenHostSpawnsTestAgentsInCruise_AndTheyFlyToTheShip_ThenTheyMissEveryRockAndOrbitIt() {
            LogAssert.ignoreFailingMessages = true;
            yield return _steps.SpawnFlockInSlowCruiseCoroutine();
            yield return _steps.WaitForOrbitCoroutine("flock-arriving-through-field.png");
            yield return CheckOrbitCoroutine();
            yield return FieldAgentTestFlight.CaptureCoroutine("flock-orbiting-ship.png");
            Assert.AreEqual(0, _flight.RockPushOuts, "The steering let agents into rock spheres: the server had to push them out.");
            yield return _flight.LandAndLeaveCoroutine();
        }

        private IEnumerator CheckOrbitCoroutine() {
            float end = Time.time + ORBIT_CHECK_SECONDS;
            while (Time.time < end) {
                _flight.CheckRocks(_steps.Flock.FlockIds);
                yield return null;
            }

            Assert.AreEqual(0, _flight.RockHits, $"An agent flew into a rock while orbiting: {_flight.FirstRockHit}.");
            IFieldAgentShownAgents shown = SessionQueries.Resolve<IFieldAgentShownAgents>();
            Assert.AreEqual(FieldAgentFlockSteps.FLOCK_SIZE, shown.ShownCount, "The host does not show every agent.");
            float radius = SessionQueries.Resolve<FieldAgentConfiguration>().DebugOrbitRadius;
            foreach (int id in _steps.Flock.FlockIds) {
                Assert.IsTrue(_flight.Control.TryGetWorldPosition(id, out Vector3 position));
                Vector3 flat = position - _flight.Ship.transform.position;
                flat.y = 0f;
                Assert.AreEqual(radius, flat.magnitude, ORBIT_BAND_METERS, $"Agent {id} is not on the orbit round the ship.");
                Assert.IsTrue(shown.TryGetShownWorldPosition(id, out Vector3 _), $"The host does not show agent {id}.");
            }
        }
    }
}
#endif
