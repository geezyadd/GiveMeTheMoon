using Features.GameCoreModule.Contracts;
using NUnit.Framework;

namespace Game.Connection.Editor.Tests {
    public sealed class JoinRuleTests {
        private const int PLAYER_SLOTS = 4;
        private const int FREE_SLOT_COUNT = 2;

        private readonly JoinRule _rule = new JoinRule();

        [Test]
        public void WhenSessionIsInLobby_ThenPlayerIsAccepted() =>
            Assert.That(_rule.Evaluate(Request(isInLobby: true, runWindow: RunJoinWindow.NoRun)), Is.EqualTo(ConnectionRejectReason.Accepted));

        [Test]
        public void WhenShipStandsAtStation_ThenPlayerIsAccepted() =>
            Assert.That(_rule.Evaluate(Request(runWindow: RunJoinWindow.AtStation)), Is.EqualTo(ConnectionRejectReason.Accepted));

        [Test]
        public void WhenShipIsInFlight_ThenPlayerIsRejectedInFlight() =>
            Assert.That(_rule.Evaluate(Request(runWindow: RunJoinWindow.InFlight)), Is.EqualTo(ConnectionRejectReason.InFlight));

        [Test]
        public void WhenRunMapHasNoRunBoundYet_ThenPlayerIsAskedToRetry() =>
            Assert.That(_rule.Evaluate(Request(runWindow: RunJoinWindow.NoRun)), Is.EqualTo(ConnectionRejectReason.MapChanging));

        [Test]
        public void WhenMapIsChanging_ThenPlayerIsAskedToRetryEvenFromLobby() =>
            Assert.That(_rule.Evaluate(Request(isInLobby: true, isChangingMap: true)), Is.EqualTo(ConnectionRejectReason.MapChanging));

        [Test]
        public void WhenAllSlotsAreTaken_ThenPlayerIsRejectedAsFull() =>
            Assert.That(_rule.Evaluate(Request(playerCount: PLAYER_SLOTS)), Is.EqualTo(ConnectionRejectReason.ServerFull));

        [Test]
        public void WhenOneSlotIsLeft_ThenPlayerIsAccepted() =>
            Assert.That(_rule.Evaluate(Request(playerCount: PLAYER_SLOTS - 1)), Is.EqualTo(ConnectionRejectReason.Accepted));

        [Test]
        public void WhenShipIsInFlightAndCrewIsFull_ThenFlightIsTheReason() =>
            Assert.That(_rule.Evaluate(Request(runWindow: RunJoinWindow.InFlight, playerCount: PLAYER_SLOTS)), Is.EqualTo(ConnectionRejectReason.InFlight));

        [Test]
        public void WhenVersionDiffers_ThenVersionIsTheReasonBeforeAnythingElse() =>
            Assert.That(
                _rule.Evaluate(Request(versionMatches: false, hasValidSteamId: false, runWindow: RunJoinWindow.InFlight)),
                Is.EqualTo(ConnectionRejectReason.VersionMismatch));

        [Test]
        public void WhenSteamIdIsInvalid_ThenPlayerIsRejectedForSteamId() =>
            Assert.That(_rule.Evaluate(Request(hasValidSteamId: false)), Is.EqualTo(ConnectionRejectReason.InvalidSteamId));

        private static JoinRequest Request(
            bool versionMatches = true,
            bool hasValidSteamId = true,
            bool isInLobby = false,
            bool isChangingMap = false,
            RunJoinWindow runWindow = RunJoinWindow.AtStation,
            int playerCount = PLAYER_SLOTS - FREE_SLOT_COUNT) =>
            new JoinRequest(versionMatches, hasValidSteamId, isInLobby, isChangingMap, runWindow, playerCount, PLAYER_SLOTS);
    }
}
