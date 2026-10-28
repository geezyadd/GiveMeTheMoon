#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.ShipModule.Scripts;
using NUnit.Framework;
using Tests.Coop.Shared;
using UnityEngine.TestTools;

namespace Tests.Coop.Host {
    // Three players: two client processes join the host's lobby and fly together; the second one leaves in flight and
    // comes back at the next station, while the first one rides on as if nothing happened and sees the crew change.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopThreePlayerTest {
        private const int TEST_TIMEOUT_MS = 300000;
        private const int CREW_OF_THREE = 3;
        private const int CREW_OF_TWO = 2;
        private const string CREW_SCREENSHOT = "three-players";

        private CoopHostHarness _host;
        private CoopClientBot _first;
        private CoopClientBot _second;

        [SetUp]
        public void SetUp() =>
            _host = new CoopHostHarness(nameof(CoopThreePlayerTest));

        [UnityTearDown]
        public IEnumerator TearDown() =>
            _host.TearDownCoroutine();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenTwoClientsJoin_AndOneLeavesInFlightAndRejoins_ThenTheOtherIsUnaffected() {
            yield return _host.HostLobbyCoroutine();
            yield return _host.StartClientAndJoinCoroutine(CoopHostHarness.CLIENT_NAME, bot => _first = bot);
            yield return _host.StartClientAndJoinCoroutine(CoopHostHarness.SECOND_CLIENT_NAME, bot => _second = bot);
            yield return _host.StartRunCoroutine();
            yield return _host.CommandCoroutine(_first, CoopConnectionCommands.WAIT_STATION);
            yield return _host.CommandCoroutine(_second, CoopConnectionCommands.WAIT_STATION);
            yield return _host.CommandCoroutine(_first, CoopConnectionCommands.BOARD);
            yield return _host.CommandCoroutine(_second, CoopConnectionCommands.BOARD);
            yield return ReportBothCoroutine(CoopConnectionCommands.REPORT, CREW_OF_THREE);
            yield return _host.CommandCoroutine(_first, CoopConnectionCommands.SCREENSHOT, CREW_SCREENSHOT);

            yield return _host.LaunchCoroutine(1);
            _host.FaceAwayFromStation();
            yield return _host.CommandCoroutine(_first, CoopConnectionCommands.WAIT_PHASE, nameof(ShipRunPhase.Cruise));
            yield return _host.CommandCoroutine(_second, CoopConnectionCommands.WAIT_PHASE, nameof(ShipRunPhase.Cruise));
            yield return _host.CommandCoroutine(_second, CoopConnectionCommands.LEAVE);
            yield return _host.WaitForCoroutine(_second.IsGone, $"{_second.Name}'s connection gone on the server");
            yield return _host.CommandCoroutine(_first, CoopConnectionCommands.REPORT);
            AssertUnaffected(_first, CREW_OF_TWO, "after the other client left in flight");
            Assert.IsTrue(_first.LastReport.RunState.IsRiding, $"{_first.Name} stopped riding when the other client left.");
            _host.Flight.FaceDestination();

            yield return _host.LandCoroutine(1);
            yield return _host.CommandCoroutine(_second, CoopConnectionCommands.JOIN, CoopHostHarness.SECOND_CLIENT_NAME);
            yield return ReportBothCoroutine(CoopConnectionCommands.REPORT, CREW_OF_THREE);
            yield return _host.QuitClientCoroutine(_first);
            yield return _host.QuitClientCoroutine(_second);
            _host.AssertNoErrors(_first, _second);
        }

        private IEnumerator ReportBothCoroutine(string command, int crew) {
            yield return _host.CommandCoroutine(_first, command);
            AssertUnaffected(_first, crew, $"at '{command}'");
            yield return _host.CommandCoroutine(_second, command);
            AssertUnaffected(_second, crew, $"at '{command}'");
        }

        // The run as the host sees it, the client on the deck and as many players as the host has.
        private void AssertUnaffected(CoopClientBot bot, int crew, string when) {
            CoopObservation client = bot.LastReport;
            CoopObservation host = CoopObserver.CaptureGame(client.Step);
            Assert.AreEqual(host.Phase, client.Phase, $"{bot.Name} sees another phase {when}.");
            Assert.AreEqual(host.LoopIndex, client.LoopIndex, $"{bot.Name} sees another loop {when}.");
            CollectionAssert.AreEqual(host.Sockets, client.Sockets, $"{bot.Name} sees other modules {when}.");
            Assert.AreEqual(host.Balance, client.Balance, $"{bot.Name} sees another crew balance {when}.");
            Assert.AreEqual(host.FieldSeed, client.FieldSeed, $"{bot.Name} sees another field {when}.");
            Assert.IsTrue(client.IsOnDeck, $"{bot.Name} is off the deck {when}: {client.Position}.");
            Assert.AreEqual(crew, client.RunState.PlayerCount, $"{bot.Name} sees {client.RunState.PlayerCount} players {when}, the host {crew}.");
            StringAssert.Contains(CoopTestPlayers.HOST_NAME, client.OtherNameplate, $"{bot.Name} does not see the host's nameplate {when}.");
        }
    }
}
#endif
