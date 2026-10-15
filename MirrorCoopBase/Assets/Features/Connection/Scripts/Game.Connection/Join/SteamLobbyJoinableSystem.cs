using System;
using Features.GameCoreModule.Contracts;
using Mirror;
using Zenject;

namespace Game.Connection
{
    // Host: friends see the Steam lobby as joinable exactly when the join rule would let a new player in.
    public sealed class SteamLobbyJoinableSystem : IInitializable, IDisposable
    {
        private readonly ISteamLobbyService _steamLobbyService;
        private readonly IConnectionJoinPolicy _connectionJoinPolicy;
        private readonly ISessionJoinGate _sessionJoinGate;
        private readonly ConnectionNetworkEvents _connectionNetworkEvents;

        public SteamLobbyJoinableSystem(
            ISteamLobbyService steamLobbyService,
            IConnectionJoinPolicy connectionJoinPolicy,
            ISessionJoinGate sessionJoinGate,
            ConnectionNetworkEvents connectionNetworkEvents)
        {
            _steamLobbyService = steamLobbyService;
            _connectionJoinPolicy = connectionJoinPolicy;
            _sessionJoinGate = sessionJoinGate;
            _connectionNetworkEvents = connectionNetworkEvents;
        }

        public void Initialize()
        {
            _sessionJoinGate.OnWindowChanged += OnJoinStateChanged;
            _connectionNetworkEvents.MapLoadStarted += OnJoinStateChanged;
            _connectionNetworkEvents.MapReady += OnJoinStateChanged;
            _connectionNetworkEvents.ServerPlayersChanged += OnJoinStateChanged;
        }

        public void Dispose()
        {
            _sessionJoinGate.OnWindowChanged -= OnJoinStateChanged;
            _connectionNetworkEvents.MapLoadStarted -= OnJoinStateChanged;
            _connectionNetworkEvents.MapReady -= OnJoinStateChanged;
            _connectionNetworkEvents.ServerPlayersChanged -= OnJoinStateChanged;
        }

        private void OnJoinStateChanged()
        {
            if (NetworkServer.active == false)
                return;

            _steamLobbyService.SetLobbyJoinable(_connectionJoinPolicy.IsOpenForNewPlayers);
        }
    }
}
