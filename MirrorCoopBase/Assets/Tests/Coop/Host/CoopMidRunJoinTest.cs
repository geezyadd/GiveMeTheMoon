#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.GrabModule.Scripts;
using Features.ShipDamageModule.Scripts.Debug;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using Features.ShipModule.Scripts.Generated;
using Mirror;
using NUnit.Framework;
using Tests.Coop.Shared;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Coop.Host {
    // A run is going (loop 1 flown) when the client process joins for the first time at the station: it lands on the
    // deck of that station and gets the whole shared state at once, the same as the host's: modules with the light
    // machine gun in the turret, the crew balance after a debug grant, the field, the sonar marks, the item the host
    // dropped on the deck, the damaged hull with a broken turret and an open breach. Then both fly the next loop.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopMidRunJoinTest {
        private const int TEST_TIMEOUT_MS = 300000;
        private const float HULL_DAMAGE = 30f;
        private const string BREACH_ID = "breach-01";
        private const string JOINED_SCREENSHOT = "mid-run-join";

        private CoopHostHarness _host;
        private CoopClientBot _client;
        private Grabbable _deckItem;

        [SetUp]
        public void SetUp() =>
            _host = new CoopHostHarness(nameof(CoopMidRunJoinTest));

        [UnityTearDown]
        public IEnumerator TearDown() =>
            _host.TearDownCoroutine();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenClientJoinsMidRunAtStation_ThenItGetsTheWholeRunState() {
            yield return _host.HostLobbyCoroutine();
            yield return _host.StartRunCoroutine();
            new LoopSmokeSonarCheck(_host.Session).InstallRadarAtStation();
            Assert.IsTrue(CoopTurret.FindSocket(_host.Ship).ServerTryInstall(CoopTurret.LIGHT_GUN_MODULE_ID), "The light machine gun was not installed in the turret.");
            yield return _host.LaunchCoroutine(1);
            yield return _host.LandCoroutine(1);
            yield return ChangeSharedStateAtStationCoroutine();

            yield return _host.StartClientAndJoinCoroutine(CoopHostHarness.CLIENT_NAME, bot => _client = bot);
            AssertJoinedAtStation();
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.SCREENSHOT, JOINED_SCREENSHOT);

            yield return _host.LaunchCoroutine(2);
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.WAIT_PHASE, nameof(ShipRunPhase.Cruise));
            _host.AssertSameView(_client, true);
            CollectionAssert.Contains(_client.LastReport.DeckItemIds, _deckItem.netId, "The joined client does not see the host's item on the deck in flight.");
            yield return _host.LandCoroutine(2);
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.WAIT_STATION);
            _host.AssertSameView(_client, false);
            yield return _host.QuitClientCoroutine(_client);
            _host.AssertNoErrors(_client);
        }

        // Everything a late joiner must get from the shared models: money, an item on the deck, hull damage, a broken
        // module and an open breach (repaired only on the next landing).
        private IEnumerator ChangeSharedStateAtStationCoroutine() {
            _host.BeginStep("change the shared state at the station");
            yield return LoopSmokeDebugMoneyGrant.GrantThroughDebugWindowCoroutine();
            yield return LoopSmokeDeckItemCheck.DropOnDeckByHandCoroutine(_host.Ship, LoopSmokeDeckItemCheck.FindLooseBoxItems(_host.Ship), item => _deckItem = item);
            SessionQueries.Resolve<IShipHullDebug>().DebugDamage(HULL_DAMAGE);
            IShipDamageDebug damage = SessionQueries.Resolve<IShipDamageDebug>();
            Assert.IsTrue(damage.DebugBreakModule(CoopTurret.SOCKET_ID), "The turret did not break.");
            Assert.IsTrue(damage.DebugOpenBreach(BREACH_ID), $"The breach {BREACH_ID} did not open.");
            yield return _host.Flight.BoardPlayerCoroutine();
        }

        private void AssertJoinedAtStation() {
            CoopObservation report = _client.LastReport;
            Assert.AreEqual(nameof(ShipRunPhase.Build), report.Phase, "The joined client is not at a station.");
            Assert.AreEqual(1, report.LoopIndex, "The joined client is not at the station after loop 1.");
            Assert.Less(report.RunState.Hull, report.RunState.HullMax, "The joined client sees a full hull: the damage did not arrive.");
            CollectionAssert.Contains(report.Sockets, $"{CoopTurret.SOCKET_ID}={CoopTurret.LIGHT_GUN_MODULE_ID}", "The joined client does not see the light machine gun in the turret.");
            CollectionAssert.Contains(report.DeckItemIds, _deckItem.netId, "The joined client does not see the host's item on the deck.");
            Assert.AreEqual(report.FieldNumber, CoopObserver.CaptureGame(report.Step).FieldNumber, "The joined client has another field number than the host.");
            Assert.AreEqual(report.FieldSeed, CoopObserver.CaptureGame(report.Step).FieldSeed, "The joined client has another field seed than the host.");
            Assert.IsTrue(_client.TryGetPlayer(out NetworkIdentity player) && SessionQueries.IsOnDeck(_host.Ship, player.transform.position), "The joined client is not on the deck by its server position.");
            Assert.IsFalse(player.GetComponent<ShipRider>().IsSeated, "The joined client is seated on the server.");
            _host.AssertSameView(_client, false);
        }
    }
}
#endif
