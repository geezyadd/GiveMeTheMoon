#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.NetworkModelModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Game.Connection;
using Mirror;
using NUnit.Framework;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Coop.Host {
    // The client process is killed in flight while it steers at the helm, as a crash would end it: the host drops the
    // player (helm, its station and the throttle free, no player object left behind), a relaunched client is turned
    // away in flight and comes back at the next station under the same player key, on the deck and not seated.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopClientCrashTest {
        private const int TEST_TIMEOUT_MS = 300000;
        private const string CRASH_SCREENSHOT = "crash-before-kill";
        private const string REJOINED_SCREENSHOT = "crash-rejoined";

        private CoopHostHarness _host;
        private CoopClientBot _client;

        [SetUp]
        public void SetUp() =>
            _host = new CoopHostHarness(nameof(CoopClientCrashTest));

        [UnityTearDown]
        public IEnumerator TearDown() =>
            _host.TearDownCoroutine();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenClientCrashesInFlightAtTheHelm_ThenTheHostFreesItsPlace_AndTheRelaunchedClientRejoinsAtTheNextStation() {
            yield return _host.HostLobbyCoroutine();
            yield return _host.StartClientAndJoinCoroutine(CoopHostHarness.CLIENT_NAME, bot => _client = bot);
            yield return _host.StartRunCoroutine();
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.WAIT_STATION);
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.SIT_AT_HELM);
            ShipSocket helm = CoopHelm.FindSocket(_host.Ship);
            PlayerKey key = ClientRider().Key;
            yield return _host.WaitForCoroutine(() => helm.IsHeldBy(key), "the helm held by the client's key on the server");

            yield return _host.LaunchCoroutine(1);
            _host.FaceAwayFromStation();
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.WAIT_PHASE, nameof(ShipRunPhase.Cruise));
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.SCREENSHOT, CRASH_SCREENSHOT);
            _host.BeginStep("kill the client process in flight");
            _client.Kill();
            yield return _host.WaitForCoroutine(_client.IsGone, "the crashed client's connection gone on the server");
            AssertCrashedClientRemoved();

            _client = _host.StartClient(CoopHostHarness.CLIENT_NAME);
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.JOIN_EXPECT_REJECT, CoopHostHarness.CLIENT_NAME);
            Assert.AreEqual(nameof(ConnectionRejectReason.InFlight), _client.LastReport.RejectReason, "The relaunched client was turned away in flight for another reason.");
            Assert.IsTrue(_client.LastReport.IsRejectTextShown, "The relaunched client's menu does not show the in-flight reason.");
            _host.Flight.FaceDestination();

            yield return _host.LandCoroutine(1);
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.JOIN, CoopHostHarness.CLIENT_NAME);
            ShipRider rider = ClientRider();
            Assert.AreEqual(key, rider.Key, "The relaunched client came back under another player key.");
            Assert.IsFalse(rider.IsSeated, "The relaunched client is seated on the server.");
            Assert.IsFalse(helm.HasOccupant, "The helm is taken after the crashed pilot came back.");
            Assert.IsTrue(SessionQueries.IsOnDeck(_host.Ship, rider.transform.position), $"The relaunched client is not on the deck by its server position: {rider.transform.position}.");
            Assert.IsFalse(_client.LastReport.IsHelmPilot, "The relaunched client steers from the helm.");
            _host.AssertSameView(_client, false);
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.SCREENSHOT, REJOINED_SCREENSHOT);
            yield return _host.QuitClientCoroutine(_client);
            _host.AssertNoErrors(_client);
        }

        private void AssertCrashedClientRemoved() {
            Assert.IsFalse(CoopHelm.FindSocket(_host.Ship).HasOccupant, "The crashed pilot still holds the helm.");
            Assert.IsFalse(CoopHelm.FindStation(_host.Ship).HasPilot, "The helm station still has the crashed pilot.");
            Assert.IsTrue(string.IsNullOrEmpty(SessionQueries.Resolve<IReadOnlyShipThrottleModel>().HolderKey), "The throttle is still held after the crash.");
            Assert.AreEqual(1, Object.FindObjectsByType<ShipRider>(FindObjectsSortMode.None).Length, "The host still sees a player object of the crashed client.");
            Assert.AreEqual(ShipRunPhase.Cruise, _host.Model.Phase, "The flight did not go on after the client crashed.");
        }

        private ShipRider ClientRider() {
            Assert.IsTrue(_client.TryGetPlayer(out NetworkIdentity player), "The client has no player on the server.");
            return player.GetComponent<ShipRider>();
        }
    }
}
#endif
