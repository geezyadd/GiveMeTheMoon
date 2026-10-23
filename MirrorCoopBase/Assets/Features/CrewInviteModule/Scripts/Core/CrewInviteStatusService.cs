using Features.CrewInviteModule.Data.Generated;
using Game.Connection;

namespace Features.CrewInviteModule.Scripts.Core {
    public sealed class CrewInviteStatusService : ICrewInviteStatusService {
        private readonly ICrewInviteRule _crewInviteRule;
        private readonly IReadOnlyCrewRosterModel _crewRosterModel;
        private readonly ISteamLobbyService _steamLobbyService;

        public CrewInviteStatusService(
            ICrewInviteRule crewInviteRule,
            IReadOnlyCrewRosterModel crewRosterModel,
            ISteamLobbyService steamLobbyService) {
            _crewInviteRule = crewInviteRule;
            _crewRosterModel = crewRosterModel;
            _steamLobbyService = steamLobbyService;
        }

        public CrewInviteStatus EvaluateForLocalPlayer() {
            bool hasSteamLobby = _steamLobbyService.CurrentLobbyId.HasValue;
            return _crewInviteRule.Evaluate(CreateRequest(hasSteamLobby, hasSteamLobby && _steamLobbyService.IsOverlayEnabled));
        }

        // The server cannot see a remote player's Steam client: that player's own check ran before it asked.
        public CrewInviteStatus EvaluateForRemotePlayer() =>
            _crewInviteRule.Evaluate(CreateRequest(hasSteamLobby: true, isOverlayEnabled: true));

        private CrewInviteRequest CreateRequest(bool hasSteamLobby, bool isOverlayEnabled) =>
            new CrewInviteRequest(
                hasSteamLobby,
                isOverlayEnabled,
                _crewRosterModel.IsAvailable && _crewRosterModel.IsJoinWindowOpen,
                _crewRosterModel.PlayerCount,
                _crewRosterModel.PlayerSlots);
    }
}
