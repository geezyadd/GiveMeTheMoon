#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.CharacterMovableModule.Scripts;
using Features.PlayerLifeModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Mirror;
using NUnit.Framework;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;

namespace Tests.PlayMode.ShipRide {
    // A host player standing anywhere on the deck when the lever is pulled, or in the air from a jump, flies away with
    // the ship, and a jump in cruise lands back on the deck still riding. One flight per spot.
    public sealed class ShipRideTest {
        private const int TEST_TIMEOUT_MS = 240000;
        private const float FLIGHT_TIMEOUT_SECONDS = 30f;
        private const float SETTLE_SECONDS = 1.5f;
        private const float RIDE_CHECK_SECONDS = 1f;
        private const float CRUISE_JUMP_SECONDS = 0.3f;
        private const float CRUISE_LANDING_SECONDS = 1.5f;
        private const float JUMP_APEX_TIMEOUT_SECONDS = 2f;
        private const float JUMP_STARTED_SPEED = 1f;
        // The jump at the lever must lift the player clear of the deck's ride volume (2.55 m above the deck surface).
        private const float MIN_JUMP_HEIGHT_AT_LEVER = 2.6f;
        // A short sideways hop in cruise: from the deck centre it lands on the deck somewhere else.
        private static readonly Vector2 _cruiseHopMove = new Vector2(0.3f, 0.3f);

        private LoopSmokeErrorLog _errorLog;
        private LoopSmokeSteamLobbySpy _steamLobby;
        private LoopSmokeShipRunConfigOverride _configOverride;
        private LoopSmokeFieldCatalogOverride _fieldCatalogOverride;
        private LoopSmokeSession _session;
        private LoopSmokeFlight _flight;
        private ShipRideDeckSpots _spots;
        private CharacterInputBuffer _input;

        private static NetworkIdentity LocalPlayer =>
            SessionQueries.LocalPlayer;

        private IReadOnlyShipRunModel Model =>
            _session.Model;

        [SetUp]
        public void SetUp() {
            _configOverride = new LoopSmokeShipRunConfigOverride(LoopSmokeShipRunConfigOverride.FAST_TIMINGS_JSON);
            _fieldCatalogOverride = new LoopSmokeFieldCatalogOverride();
            _errorLog = new LoopSmokeErrorLog();
            _steamLobby = new LoopSmokeSteamLobbySpy();
            _steamLobby.BindIntoSceneContexts();
            _session = new LoopSmokeSession(_errorLog);
        }

        [TearDown]
        public void TearDown() {
            _input?.EndScripted();
            _configOverride.Dispose();
            _fieldCatalogOverride.Dispose();
            _errorLog.Dispose();
            _steamLobby.Dispose();
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenHostStandsAnywhereOnDeck_AndPullsLever_ThenRidesEveryFlight() {
            // LoopSmokeErrorLog fails the test with the full list and its allow-list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return _session.EnterLobbyAsHostCoroutine();
            yield return _session.StartGameCoroutine();
            _configOverride.AssertBound();
            _flight = new LoopSmokeFlight(_session, new LoopSmokeFieldCheck(ProjectContext.Instance.Container));
            _spots = new ShipRideDeckSpots(_session.Ship);
            _input = SessionQueries.Resolve<CharacterInputBuffer>();
            _flight.InstallRequiredEngines();

            yield return FlyFromCoroutine(1, "nose", _spots.Nose, false, Vector2.zero);
            yield return FlyFromCoroutine(2, "stern", _spots.Stern, false, Vector2.zero);
            yield return FlyFromCoroutine(3, "port side edge", _spots.PortSide, false, Vector2.zero);
            yield return FlyFromCoroutine(4, "starboard side edge", _spots.StarboardSide, false, Vector2.zero);
            yield return FlyFromCoroutine(5, "centre, in a jump at the lever", _spots.Centre, true, _cruiseHopMove);
            yield return FlyFromCoroutine(6, "stern, in a jump at the lever", _spots.Stern, true, Vector2.zero);

            yield return _session.LeaveToMenuCoroutine();
            Assert.AreEqual(0, _errorLog.Count, $"Errors were logged during the flights:\n{_errorLog.Describe()}");
        }

        private IEnumerator FlyFromCoroutine(int loop, string spotName, Vector3 spot, bool jumpAtLever, Vector2 cruiseJumpMove) {
            string flight = $"Loop {loop} from the {spotName}";
            ShipLandingPad launchPad = _flight.FindPadUnderShip();
            _spots.PutPlayerAt(LocalPlayer, spot);
            yield return new WaitForSeconds(SETTLE_SECONDS);
            Assert.IsTrue(_spots.IsOnDeck(LocalPlayer.transform.position), $"{flight}: the player is not on the deck before launch: {_spots.Describe(LocalPlayer.transform.position)}.");

            if (jumpAtLever)
                yield return JumpToApexCoroutine(flight);

            _session.StartPhaseRecord();
            _flight.PullLaunchLever(loop);
            _input.EndScripted();
            yield return new WaitForSeconds(RIDE_CHECK_SECONDS);
            AssertRiding(flight, "after takeoff");

            yield return _session.WaitForCoroutine(IsCruising, FLIGHT_TIMEOUT_SECONDS, $"{flight}: cruise");
            AssertRiding(flight, "in cruise");
            _flight.FaceDestination();
            yield return JumpCoroutine(cruiseJumpMove, CRUISE_JUMP_SECONDS);
            _input.EndScripted();
            yield return new WaitForSeconds(CRUISE_LANDING_SECONDS);
            AssertRiding(flight, "after a jump in cruise");

            yield return _session.WaitForCoroutine(IsBackInBuild, FLIGHT_TIMEOUT_SECONDS, $"{flight}: Build after landing");
            yield return _flight.AssertLandedSafelyCoroutine(loop, launchPad, _flight.FindPadUnderShip());
        }

        // The lever is pulled at the top of a jump, when the player is highest above the deck.
        private IEnumerator JumpToApexCoroutine(string flight) {
            Rigidbody body = LocalPlayer.GetComponent<CharacterMovableBase>().Body;
            _input.BeginScripted();
            float deadline = Time.realtimeSinceStartup + JUMP_APEX_TIMEOUT_SECONDS;
            bool isJumpHeld = false;
            // A press is taken only on the ground: press again every other step until the jump starts.
            while (body.linearVelocity.y < JUMP_STARTED_SPEED) {
                Assert.Less(Time.realtimeSinceStartup, deadline, $"{flight}: the jump did not start.");
                isJumpHeld = isJumpHeld == false;
                _input.SetScripted(Vector2.zero, isJumpHeld, false);
                yield return new WaitForFixedUpdate();
            }

            _input.SetScripted(Vector2.zero, true, false);
            yield return _session.WaitForCoroutine(() => body.linearVelocity.y <= 0f, JUMP_APEX_TIMEOUT_SECONDS, $"{flight}: the top of the jump");
            Assert.GreaterOrEqual(_spots.HeightAboveDeck(body.position), MIN_JUMP_HEIGHT_AT_LEVER, $"{flight}: the jump is too low: {_spots.Describe(body.position)}.");
        }

        private IEnumerator JumpCoroutine(Vector2 move, float seconds) {
            _input.BeginScripted();
            _input.SetScripted(move, true, false);
            yield return new WaitForSeconds(seconds);
            _input.SetScripted(Vector2.zero, false, false);
        }

        private void AssertRiding(string flight, string moment) {
            Vector3 player = LocalPlayer.transform.position;
            Assert.IsFalse(LocalPlayer.GetComponent<IDamageable>().IsDead, $"{flight}: the player died {moment}.");
            Assert.IsTrue(LocalPlayer.GetComponent<ShipRider>().IsRiding, $"{flight}: the player is not riding {moment}: {_spots.Describe(player)}.");
            Assert.IsTrue(_spots.IsOnDeck(player), $"{flight}: the player is off the deck {moment}: {_spots.Describe(player)}.");
        }

        private bool IsCruising() =>
            Model.Phase == ShipRunPhase.Cruise;

        private bool IsBackInBuild() =>
            _session.Phases.Count > 1 && Model.Phase == ShipRunPhase.Build;
    }
}
#endif
