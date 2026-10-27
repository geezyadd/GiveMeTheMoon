#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.GrabModule.Scripts;
using Features.PlayerLifeModule.Scripts;
using Features.NetworkModelModule.Scripts;
using Features.ShipModule.Scripts;
using Game.Connection;
using Mirror;
using NUnit.Framework;
using Tests.Coop.Shared;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Coop.Host {
    // The client process leaves through the HUD and joins again: what it had as a player comes back by its player key
    // as designed. A seat is freed when it leaves; an item in its hand drops where it was; a dead player comes back
    // dead; money is the crew's and stays as it was.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopLeaveRejoinTest {
        private const int TEST_TIMEOUT_MS = 300000;
        // A dropped item falls onto the deck and comes to rest there within this long.
        private const float ITEM_REST_TIMEOUT_SECONDS = 5f;

        private CoopHostHarness _host;
        private CoopClientBot _client;

        [SetUp]
        public void SetUp() =>
            _host = new CoopHostHarness(TestContext.CurrentContext.Test.Name);

        [UnityTearDown]
        public IEnumerator TearDown() =>
            _host.TearDownCoroutine();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenClientLeavesAtStationSeatedAtTheTurret_AndRejoins_ThenTheSeatIsFreeAndItStandsOnTheDeck() {
            yield return StartRunWithClientCoroutine();
            Assert.IsTrue(CoopTurret.FindSocket(_host.Ship).ServerTryInstall(CoopTurret.MINIGUN_MODULE_ID), "The minigun was not installed in the turret.");
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.SIT_AT_TURRET);
            ShipSocket turret = CoopTurret.FindSocket(_host.Ship);
            PlayerKey key = ClientRider().Key;
            yield return _host.WaitForCoroutine(() => turret.IsHeldBy(key), "the turret held by the client's key on the server");
            long balance = _client.LastReport.Balance;

            yield return LeaveCoroutine();
            Assert.IsFalse(turret.HasOccupant, "The client left, but the turret seat is still taken.");

            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.JOIN, CoopHostHarness.CLIENT_NAME);
            AssertBackOnDeck(key);
            Assert.IsFalse(_client.LastReport.RunState.IsTurretGunner, "The rejoined client is back at the turret.");
            Assert.AreEqual(balance, _client.LastReport.Balance, "The crew balance changed across the client's leave and rejoin.");
            yield return FinishCoroutine();
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenClientLeavesInFlightHoldingAnItem_AndRejoinsAtNextStation_ThenTheItemLiesOnTheDeckAndTheHandIsEmpty() {
            yield return StartRunWithClientCoroutine();
            Grabbable item = GiveClientAnItem();
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.REPORT);
            Assert.AreEqual(item.netId, _client.LastReport.RunState.HeldItemNetId, "The client does not see the given item in its hand.");
            PlayerKey key = ClientRider().Key;

            yield return _host.LaunchCoroutine(1);
            _host.FaceAwayFromStation();
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.WAIT_PHASE, nameof(ShipRunPhase.Cruise));
            yield return LeaveCoroutine();
            Assert.IsTrue(item.CanBeGrabbed, $"The {item.name} the client held is not free after it left.");
            yield return _host.WaitForCoroutine(() => LiesOnDeck(item), $"the {item.name} the client held lying on the deck", null, null, ITEM_REST_TIMEOUT_SECONDS);

            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.JOIN_EXPECT_REJECT, CoopHostHarness.CLIENT_NAME);
            Assert.AreEqual(nameof(ConnectionRejectReason.InFlight), _client.LastReport.RejectReason, "A join in flight was turned away for another reason.");
            Assert.IsTrue(_client.LastReport.IsRejectTextShown, "The client's menu does not show the in-flight reason.");
            Assert.IsTrue(_client.IsGone(), "The server kept a player who was turned away in flight.");
            _host.Flight.FaceDestination();

            yield return _host.LandCoroutine(1);
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.JOIN, CoopHostHarness.CLIENT_NAME);
            AssertBackOnDeck(key);
            Assert.AreEqual(0u, _client.LastReport.RunState.HeldItemNetId, "The rejoined client holds an item.");
            yield return FinishCoroutine();
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenDeadClientLeavesAtStation_AndRejoins_ThenItIsStillDeadAndSpectating() {
            yield return StartRunWithClientCoroutine();
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.DIE);
            yield return _host.WaitForCoroutine(() => ClientLifeState() == PlayerLifeState.Dead, "the client dead on the server");
            yield return LeaveCoroutine();

            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.JOIN, CoopHostHarness.CLIENT_NAME);
            Assert.AreEqual(nameof(PlayerLifeState.Dead), _client.LastReport.LifeState, "The client came back alive after it left dead.");
            Assert.IsTrue(_client.LastReport.IsSpectating, "The client came back dead but does not spectate.");
            Assert.AreEqual(PlayerLifeState.Dead, ClientLifeState(), "The server brought the dead client back alive.");
            yield return FinishCoroutine();
        }

        // Joins in the lobby, both start the run: the client stands at the station's player spawn beside the ship, walks
        // aboard (boarding stands in for it) and sees the same ship and field.
        private IEnumerator StartRunWithClientCoroutine() {
            yield return _host.HostLobbyCoroutine();
            yield return _host.StartClientAndJoinCoroutine(CoopHostHarness.CLIENT_NAME, bot => _client = bot);
            yield return _host.StartRunCoroutine();
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.WAIT_STATION);
            Assert.IsTrue(_client.LastReport.RunState.StandsOnGround, $"The client does not stand on the ground at the run's first station: {_client.LastReport.Position}.");
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.BOARD);
            _host.AssertSameView(_client, false);
        }

        private IEnumerator LeaveCoroutine() {
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.LEAVE);
            yield return _host.WaitForCoroutine(_client.IsGone, "the client's connection gone on the server");
            Assert.AreEqual(1, Object.FindObjectsByType<ShipRider>(FindObjectsSortMode.None).Length, "The host still sees a player object of the client that left.");
        }

        private IEnumerator FinishCoroutine() {
            yield return _host.QuitClientCoroutine(_client);
            _host.AssertNoErrors(_client);
        }

        // Test setup: the nearest free tool of the deck rack goes straight into the client's hand on the server.
        private Grabbable GiveClientAnItem() {
            Grabbable item = LoopSmokeDeckItemCheck.FindLooseBoxItems(_host.Ship)[0];
            Assert.IsTrue(ClientRider().GetComponent<GrabController>().ServerGive(item), $"The {item.name} could not be put into the client's hand.");
            return item;
        }

        private void AssertBackOnDeck(PlayerKey key) {
            ShipRider rider = ClientRider();
            Assert.AreEqual(key, rider.Key, "The rejoined client has another player key.");
            Assert.IsFalse(rider.IsSeated, "The rejoined client is seated on the server.");
            Assert.IsTrue(SessionQueries.IsOnDeck(_host.Ship, rider.transform.position), $"The rejoined client is not on the deck by its server position: {rider.transform.position}.");
            _host.AssertSameView(_client, false);
        }

        private bool LiesOnDeck(Grabbable item) =>
            SessionQueries.IsOnDeck(_host.Ship, item.transform.position)
            && Mathf.Abs(ShipDeckItemRest.MeasureGapAboveDeck(_host.Ship, item.GetComponentInChildren<BoxCollider>())) < CoopObservationComparison.DECK_ITEM_GAP_TOLERANCE;

        private ShipRider ClientRider() {
            Assert.IsTrue(_client.TryGetPlayer(out NetworkIdentity player), "The client has no player on the server.");
            return player.GetComponent<ShipRider>();
        }

        private PlayerLifeState ClientLifeState() =>
            ClientRider().GetComponent<PlayerLifeBody>().LifeState;
    }
}
#endif
