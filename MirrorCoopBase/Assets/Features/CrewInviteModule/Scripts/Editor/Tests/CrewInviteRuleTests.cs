using Features.CrewInviteModule.Scripts;
using NUnit.Framework;

namespace Features.CrewInviteModule.Editor.Tests {
    public sealed class CrewInviteRuleTests {
        private const int PLAYER_SLOTS = 4;
        private const int FREE_SLOT_COUNT = 2;

        private readonly CrewInviteRule _rule = new CrewInviteRule();

        [Test]
        public void WhenSteamLobbyHasFreeSlotAndJoinWindowIsOpen_ThenInviteIsAvailable() =>
            Assert.That(_rule.Evaluate(Request()), Is.EqualTo(CrewInviteStatus.Available));

        [Test]
        public void WhenOneSlotIsLeft_ThenInviteIsAvailable() =>
            Assert.That(_rule.Evaluate(Request(playerCount: PLAYER_SLOTS - 1)), Is.EqualTo(CrewInviteStatus.Available));

        [Test]
        public void WhenAllSlotsAreTaken_ThenCrewIsFull() =>
            Assert.That(_rule.Evaluate(Request(playerCount: PLAYER_SLOTS)), Is.EqualTo(CrewInviteStatus.CrewFull));

        [Test]
        public void WhenJoinWindowIsClosed_ThenInviteWaitsForStation() =>
            Assert.That(_rule.Evaluate(Request(isJoinWindowOpen: false)), Is.EqualTo(CrewInviteStatus.NotAtStation));

        [Test]
        public void WhenJoinWindowIsClosedAndCrewIsFull_ThenStationIsTheReason() =>
            Assert.That(_rule.Evaluate(Request(isJoinWindowOpen: false, playerCount: PLAYER_SLOTS)), Is.EqualTo(CrewInviteStatus.NotAtStation));

        [Test]
        public void WhenThereIsNoSteamLobby_ThenSteamIsTheReasonBeforeAnythingElse() =>
            Assert.That(
                _rule.Evaluate(Request(hasSteamLobby: false, isOverlayEnabled: false, isJoinWindowOpen: false, playerCount: PLAYER_SLOTS)),
                Is.EqualTo(CrewInviteStatus.NoSteam));

        [Test]
        public void WhenOverlayIsDisabled_ThenOverlayIsTheReasonBeforeTheCrew() =>
            Assert.That(
                _rule.Evaluate(Request(isOverlayEnabled: false, isJoinWindowOpen: false, playerCount: PLAYER_SLOTS)),
                Is.EqualTo(CrewInviteStatus.OverlayDisabled));

        private static CrewInviteRequest Request(
            bool hasSteamLobby = true,
            bool isOverlayEnabled = true,
            bool isJoinWindowOpen = true,
            int playerCount = PLAYER_SLOTS - FREE_SLOT_COUNT) =>
            new CrewInviteRequest(hasSteamLobby, isOverlayEnabled, isJoinWindowOpen, playerCount, PLAYER_SLOTS);
    }
}
