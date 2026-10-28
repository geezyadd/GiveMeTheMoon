#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.PlayerLifeModule.Scripts;
using Features.ShipModule.Scripts.Overboard;
using NUnit.Framework;
using Tests.PlayMode.FieldAgent;
using Tests.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.PlayMode.Overboard {
    // The host is thrown overboard in a cruise and drifts; the ship is moved past the drift limit (test setup, the
    // debug move): the void takes the host, a normal death, and with nobody else alive the crew goes to the lobby.
    public sealed class OverboardLimitTest {
        private const int TEST_TIMEOUT_MS = 240000;
        private const string GAME_SCENE = "GameScene";
        private const float BEYOND_LIMIT_METERS = 100f;
        private const float DEATH_TIMEOUT_SECONDS = 3f;
        private const float LOBBY_TIMEOUT_SECONDS = 20f;

        private FieldAgentTestFlight _flight;
        private OverboardTestSteps _steps;

        [SetUp]
        public void SetUp() {
            _flight = new FieldAgentTestFlight();
            _steps = new OverboardTestSteps(_flight);
        }

        [TearDown]
        public void TearDown() =>
            _flight.Dispose();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenHostDriftsOverboard_AndShipGoesPastTheLimit_ThenTheHostDies() {
            yield return _steps.ThrowOverboardAndWatchDriftCoroutine();

            float limit = SessionQueries.Resolve<ShipOverboardConfiguration>().MaxDistanceMeters;
            Vector3 away = _flight.Ship.transform.position - OverboardTestSteps.LocalOverboard.Position;
            _flight.Ship.FlightControlDebug.DebugMove(away.normalized * (limit + BEYOND_LIMIT_METERS));
            IPlayerLifeQuery life = SessionQueries.Resolve<IPlayerLifeQuery>();
            yield return _flight.Session.WaitForCoroutine(() => life.LocalState == PlayerLifeState.Dead, DEATH_TIMEOUT_SECONDS, "the host dead past the drift limit");
            Assert.IsFalse(OverboardTestSteps.LocalOverboard.IsOverboard, "The dead host still drifts overboard.");
            Assert.IsFalse(OverboardTestSteps.LocalDrift.IsDrifting, "The dead host's body still drifts.");
            yield return _flight.Session.WaitForCoroutine(IsGameSceneGone, LOBBY_TIMEOUT_SECONDS, "the lobby after the only player died");
            yield return _flight.Session.LeaveToMenuCoroutine();
        }

        private static bool IsGameSceneGone() =>
            SceneManager.GetSceneByName(GAME_SCENE).isLoaded == false;
    }
}
#endif
