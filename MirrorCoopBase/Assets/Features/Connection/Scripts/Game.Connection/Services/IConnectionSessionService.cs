using System;
using System.Threading.Tasks;

namespace Game.Connection
{
    public interface IConnectionSessionService
    {
        bool IsInLobby { get; }
        event Action OnMapReady;
        Task HostAsync();
        Task JoinAsync(string address);
        Task HostSteamAsync();
        Task JoinSteamAsync(ulong lobbyId);
        Task StopToMenuAsync();
        bool CanStartGame { get; }
        void StartGame();
        bool ReturnToLobby();
    }
}
