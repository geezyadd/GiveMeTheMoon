#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.ShipModule.Scripts;
using Mirror;
using NUnit.Framework;
using Tests.Coop.Shared;
using UnityEngine.TestTools;

namespace Tests.Coop.Host {
    // The hull is lost in flight with the client process on the deck or seated at the turret: back in the lobby the
    // client's own player is free of the ship (not riding, not seated, its body under physics, hover on, controls free),
    // stands on the lobby's ground and walks; on the server it is not riding or seated either; in the next run it walks
    // on the deck. PR #60 fixed this for the host only.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopHullLossTest {
        private const int TEST_TIMEOUT_MS = 300000;
        private const float MIN_WALK_METERS = 1f;
        private const string LOBBY_SCREENSHOT_SUFFIX = "-lobby-after-hull-loss";

        private CoopHostHarness _host;
        private CoopClientBot _client;

        [SetUp]
        public void SetUp() =>
            _host = new CoopHostHarness(TestContext.CurrentContext.Test.Name);

        [UnityTearDown]
        public IEnumerator TearDown() =>
            _host.TearDownCoroutine();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenHullIsLostWithTheClientSeatedAtTheTurret_ThenTheClientIsFreeInTheLobby_AndWalksInTheNextRun() {
            yield return StartRunWithClientCoroutine();
            Assert.IsTrue(CoopTurret.FindSocket(_host.Ship).ServerTryInstall(CoopTurret.MINIGUN_MODULE_ID), "The minigun was not installed in the turret.");
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.SIT_AT_TURRET);
            yield return LoseHullInCruiseCoroutine("turret");
            yield return WalkInNextRunCoroutine();
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenHullIsLostWithTheClientOnTheDeck_ThenTheClientIsFreeInTheLobby_AndWalksInTheNextRun() {
            yield return StartRunWithClientCoroutine();
            yield return LoseHullInCruiseCoroutine("deck");
            yield return WalkInNextRunCoroutine();
        }

        private IEnumerator StartRunWithClientCoroutine() {
            yield return _host.HostLobbyCoroutine();
            yield return _host.StartClientAndJoinCoroutine(CoopHostHarness.CLIENT_NAME, bot => _client = bot);
            yield return _host.StartRunCoroutine();
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.WAIT_STATION);
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.BOARD);
        }

        private IEnumerator LoseHullInCruiseCoroutine(string where) {
            bool isSeated = _client.LastReport.RunState.IsSeated;
            yield return _host.LaunchCoroutine(1);
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.WAIT_PHASE, nameof(ShipRunPhase.Cruise));
            Assert.IsTrue(_client.LastReport.RunState.IsRiding, "The client does not ride the ship in cruise before the hull is lost.");
            Assert.AreEqual(isSeated, _client.LastReport.RunState.IsSeated, "The client stood up or sat down on takeoff.");

            _host.BeginStep($"lose the hull with the client on the {where}");
            yield return _host.Session.ReturnToLobbyByHullLossCoroutine();
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.WAIT_LOBBY);
            CoopRunState lobby = _client.LastReport.RunState;
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.SCREENSHOT, where + LOBBY_SCREENSHOT_SUFFIX);
            AssertFree(lobby, "in the lobby");
            Assert.IsTrue(lobby.StandsOnGround, $"The client does not stand on the lobby's ground after the hull loss: {lobby.Describe()}.");
            AssertFreeOnServer();
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.WALK);
            Assert.GreaterOrEqual(_client.LastReport.RunState.WalkedMeters, MIN_WALK_METERS, "The client does not walk in the lobby after the hull loss.");
        }

        private IEnumerator WalkInNextRunCoroutine() {
            yield return _host.StartRunCoroutine();
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.WAIT_STATION);
            AssertFree(_client.LastReport.RunState, "at the next run's first station");
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.BOARD);
            _host.AssertSameView(_client, false);
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.WALK);
            Assert.GreaterOrEqual(_client.LastReport.RunState.WalkedMeters, MIN_WALK_METERS, "The client does not walk on the deck in the next run.");
            Assert.IsTrue(_client.LastReport.IsOnDeck, "The client walked off the deck in the next run.");
            yield return _host.QuitClientCoroutine(_client);
            _host.AssertNoErrors(_client);
        }

        private static void AssertFree(CoopRunState state, string where) {
            Assert.IsFalse(state.IsRiding, $"The client still rides a ship {where}.");
            Assert.IsFalse(state.IsSeated, $"The client is still seated {where}.");
            Assert.IsFalse(state.IsTurretGunner, $"The client is still the turret's gunner {where}.");
            Assert.IsFalse(state.IsKinematic, $"The client's body is kinematic {where}.");
            Assert.IsTrue(state.DetectsCollisions, $"The client's body has its collisions off {where}.");
            Assert.IsTrue(state.IsHovering, $"The client's hover is off {where}.");
            Assert.IsFalse(state.IsControlBlocked, $"The client's controls are blocked {where}.");
        }

        private void AssertFreeOnServer() {
            Assert.IsTrue(_client.TryGetPlayer(out NetworkIdentity player), "The client has no player on the server in the lobby.");
            ShipRider rider = player.GetComponent<ShipRider>();
            Assert.IsFalse(rider.IsRiding, "The server still has the client riding a ship in the lobby.");
            Assert.IsFalse(rider.IsSeated, "The server still has the client seated in the lobby.");
        }
    }
}
#endif
