using Features.CrewInviteModule.Scripts.Core;
using Game.Connection;
using Mirror;
using Zenject;

namespace Features.CrewInviteModule.Scripts.Network {
    // Steam lives on each player's machine: the server only routes E back to the player who pressed it.
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
