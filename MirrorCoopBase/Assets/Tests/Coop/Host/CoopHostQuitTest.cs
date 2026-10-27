#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.ShipModule.Scripts;
using NUnit.Framework;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine.TestTools;

namespace Tests.Coop.Host {
    // The host leaves through the HUD while the client process plays: the client is sent back to its menu cleanly (no
    // error logged) and can host a session of its own right after.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopHostQuitTest {
        private const int TEST_TIMEOUT_MS = 240000;
        private const string MENU_SCREENSHOT_SUFFIX = "-menu-after-host-quit";

        private CoopHostHarness _host;
        private CoopClientBot _client;

        [SetUp]
        public void SetUp() =>
            _host = new CoopHostHarness(TestContext.CurrentContext.Test.Name);

        [UnityTearDown]
        public IEnumerator TearDown() =>
            _host.TearDownCoroutine();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenHostQuitsInFlight_ThenTheClientIsBackInTheMenuWithoutErrors_AndCanHost() {
            yield return StartRunWithClientCoroutine();
            yield return _host.LaunchCoroutine(1);
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.WAIT_PHASE, nameof(ShipRunPhase.Cruise));
            yield return QuitAndWatchClientCoroutine("in-flight");
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenHostQuitsAtStation_ThenTheClientIsBackInTheMenuWithoutErrors_AndCanHost() {
            yield return StartRunWithClientCoroutine();
            yield return QuitAndWatchClientCoroutine("at-station");
        }

        private IEnumerator StartRunWithClientCoroutine() {
            yield return _host.HostLobbyCoroutine();
            yield return _host.StartClientAndJoinCoroutine(CoopHostHarness.CLIENT_NAME, bot => _client = bot);
            yield return _host.StartRunCoroutine();
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.WAIT_STATION);
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.BOARD);
        }

        private IEnumerator QuitAndWatchClientCoroutine(string where) {
            _host.BeginStep($"host quits {where}");
            yield return _host.Session.LeaveToMenuCoroutine();
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.WAIT_MENU);
            Assert.IsFalse(_client.LastReport.RunState.HasLocalPlayer, "The client still has a player after the host quit.");
            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.SCREENSHOT, where + MENU_SCREENSHOT_SUFFIX);
            _host.AssertNoErrors(_client);

            yield return _host.CommandCoroutine(_client, CoopConnectionCommands.HOST_AND_LEAVE);
            CoopRunState hosting = _client.LastReport.RunState;
            Assert.IsTrue(hosting.IsHosting, "The client could not host after the host quit.");
            Assert.AreEqual(SessionQueries.NetworkManager.LobbySceneName, hosting.Scene, "The client hosting after the host quit is not in its lobby.");
            Assert.IsTrue(hosting.HasLocalPlayer, "The client hosting after the host quit has no player in its lobby.");
            yield return _host.QuitClientCoroutine(_client);
            _host.AssertNoErrors(_client);
        }
    }
}
#endif
