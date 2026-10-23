using System;
using Mirror;
using Zenject;

namespace Game.Connection
{
    // Host: friends see the Steam lobby as joinable exactly when the join rule would let a new player in.
    public sealed class SteamLobbyJoinableSystem : IInitializable, IDisposable
    {
        private readonly ISteamLobbyService _steamLobbyService;
        private readonly IConnectionJoinPolicy _connectionJoinPolicy;

        public SteamLobbyJoinableSystem(ISteamLobbyService steamLobbyService, IConnectionJoinPolicy connectionJoinPolicy)
        {
            _steamLobbyService = steamLobbyService;
            _connectionJoinPolicy = connectionJoinPolicy;
        }

        public void Initialize() =>
            _connectionJoinPolicy.OnJoinStateChanged += OnJoinStateChanged;

        public void Dispose() =>
            _connectionJoinPolicy.OnJoinStateChanged -= OnJoinStateChanged;

        private void OnJoinStateChanged()
        {
            if (NetworkServer.active == false)
                return;

            _steamLobbyService.SetLobbyJoinable(_connectionJoinPolicy.IsOpenForNewPlayers);
        }
    }
}
