using System.Threading.Tasks;

namespace Game.Connection
{
    public interface IConnectionSessionService
    {
        bool IsInLobby { get; }
        Task HostAsync();
        Task JoinAsync(string address);
        Task HostSteamAsync();
        Task JoinSteamAsync(ulong lobbyId);
        Task StopToMenuAsync();
        void StartGame();
        bool ReturnToLobby();
    }
}
