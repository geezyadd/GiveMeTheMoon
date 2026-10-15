using Game.Connection;
using Mirror;
using Zenject;

namespace Features.CrewInviteModule.Scripts {
    // The player's side of the invite terminal: whether this player may invite now, and the Steam invite overlay
    // opened on this player's own machine.
    public sealed class CrewInviter : NetworkBehaviour {
        private ICrewInviteStatusService _crewInviteStatusService;
        private ISteamLobbyService _steamLobbyService;

        [Inject]
        private void InjectDependencies(ICrewInviteStatusService crewInviteStatusService, ISteamLobbyService steamLobbyService) {
            _crewInviteStatusService = crewInviteStatusService;
            _steamLobbyService = steamLobbyService;
        }

        public bool CanInvite() =>
            EvaluateStatus() == CrewInviteStatus.Available;

        [Server]
        public void ServerOpenInviteOverlay() =>
            TargetOpenInviteOverlay();

        private CrewInviteStatus EvaluateStatus() =>
            isLocalPlayer ? _crewInviteStatusService.EvaluateForLocalPlayer() : _crewInviteStatusService.EvaluateForRemotePlayer();

        [TargetRpc]
        private void TargetOpenInviteOverlay() =>
            _steamLobbyService.OpenInviteOverlay();
    }
}
