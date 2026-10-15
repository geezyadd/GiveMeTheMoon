using Features.GameCoreModule.Contracts;
using Mirror;
using Steamworks;
using UnityEngine;

namespace Game.Connection
{
    public sealed class ConnectionJoinPolicy : IConnectionJoinPolicy
    {
        private readonly ConnectionSessionModel _connectionSessionModel;
        private readonly ISessionJoinGate _sessionJoinGate;
        private readonly IJoinRule _joinRule;
        private readonly ConnectionConfig _connectionConfig;

        public ConnectionJoinPolicy(
            ConnectionSessionModel connectionSessionModel,
            ISessionJoinGate sessionJoinGate,
            IJoinRule joinRule,
            ConnectionConfig connectionConfig)
        {
            _connectionSessionModel = connectionSessionModel;
            _sessionJoinGate = sessionJoinGate;
            _joinRule = joinRule;
            _connectionConfig = connectionConfig;
        }

        public bool IsOpenForNewPlayers =>
            NetworkServer.active
            && _joinRule.Evaluate(CreateRequest(versionMatches: true, hasValidSteamId: true)) == ConnectionRejectReason.Accepted;

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
    }
}
