using System;
using System.Threading.Tasks;

namespace Game.Connection
{
    public interface ISteamLobbyService
    {
        public bool IsAvailable { get; }
        public ulong? CurrentLobbyId { get; }
        public ulong HostSteamId { get; }
        public bool IsOverlayEnabled { get; }

        public event Action<ulong> JoinRequested;

        // The lobby was entered or left, or the Steam overlay was turned on or off while in a lobby.
        public event Action InviteStateChanged;

        public Task CreateFriendsLobbyAsync(int maxPlayers);
        public Task JoinLobbyAsync(ulong lobbyId);
        public void LeaveLobby();
        public void SetLobbyJoinable(bool joinable);
        public void OpenInviteOverlay();
    }
}
