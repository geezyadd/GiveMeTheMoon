using System;
using Features.GameCoreModule.Contracts;
using Mirror;
using Steamworks;
using UnityEngine;
using Zenject;

namespace Game.Connection
{
    public sealed class ConnectionJoinPolicy : IConnectionJoinPolicy, IInitializable, IDisposable
    {
        private readonly ConnectionSessionModel _connectionSessionModel;
        private readonly ISessionJoinGate _sessionJoinGate;
        private readonly IJoinRule _joinRule;
        private readonly ConnectionConfig _connectionConfig;
        private readonly ConnectionNetworkEvents _connectionNetworkEvents;

        public event Action OnJoinStateChanged;

        public ConnectionJoinPolicy(
            ConnectionSessionModel connectionSessionModel,
            ISessionJoinGate sessionJoinGate,
            IJoinRule joinRule,
            ConnectionConfig connectionConfig,
            ConnectionNetworkEvents connectionNetworkEvents)
        {
            _connectionSessionModel = connectionSessionModel;
            _sessionJoinGate = sessionJoinGate;
            _joinRule = joinRule;
            _connectionConfig = connectionConfig;
            _connectionNetworkEvents = connectionNetworkEvents;
        }

        public void Initialize()
        {
            _sessionJoinGate.OnWindowChanged += OnSessionChanged;
            _connectionNetworkEvents.MapLoadStarted += OnSessionChanged;
            _connectionNetworkEvents.MapReady += OnSessionChanged;
            _connectionNetworkEvents.ServerPlayersChanged += OnSessionChanged;
        }

        public void Dispose()
        {
            _sessionJoinGate.OnWindowChanged -= OnSessionChanged;
            _connectionNetworkEvents.MapLoadStarted -= OnSessionChanged;
            _connectionNetworkEvents.MapReady -= OnSessionChanged;
            _connectionNetworkEvents.ServerPlayersChanged -= OnSessionChanged;
        }

        public bool IsOpenForNewPlayers =>
            NetworkServer.active
            && _joinRule.Evaluate(CreateRequest(versionMatches: true, hasValidSteamId: true)) == ConnectionRejectReason.Accepted;

        public bool IsJoinWindowOpen =>
            NetworkServer.active && _joinRule.IsJoinWindowOpen(CreateRequest(versionMatches: true, hasValidSteamId: true));

        public int PlayerCount =>
            CountPlayers();

        public int PlayerSlots =>
            _connectionConfig.MaxConnections;

        public ConnectionRejectReason EvaluateJoin(string version, ulong steamId)
        {
            bool hasValidSteamId = _connectionSessionModel.NetworkManager.UsesSteamTransport == false
                || ((CSteamID)steamId).IsValid();
            return _joinRule.Evaluate(CreateRequest(Application.version == version, hasValidSteamId));
        }

        private JoinRequest CreateRequest(bool versionMatches, bool hasValidSteamId)
        {
            ConnectionNetworkManager networkManager = _connectionSessionModel.NetworkManager;
            return new JoinRequest(
                versionMatches,
                hasValidSteamId,
                networkManager.IsInLobby,
                networkManager.IsChangingMap,
                _sessionJoinGate.Window,
                CountPlayers(),
                _connectionConfig.MaxConnections);
        }

        // A knocking connection is not authenticated yet, so it does not take a slot from itself; a rejected one
        // waiting for its disconnect is not counted either.
        private static int CountPlayers()
        {
            int count = 0;
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (connection.isAuthenticated)
                    count++;
            }

            return count;
        }

        private void OnSessionChanged() =>
            OnJoinStateChanged?.Invoke();
    }
}
