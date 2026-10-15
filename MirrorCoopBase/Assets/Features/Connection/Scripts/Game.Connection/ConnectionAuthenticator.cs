using System.Collections;
using System.Collections.Generic;
using Mirror;
using Steamworks;
using UnityEngine;
using Zenject;

namespace Game.Connection
{
    [AddComponentMenu("Network/ Authenticators/Connection Authenticator")]
    public class ConnectionAuthenticator : NetworkAuthenticator
    {
        public struct AuthRequestMessage : NetworkMessage
        {
            public ulong steamId;
            public string playerName;
            public string version;
            public string clientId;
        }

        public struct AuthResponseMessage : NetworkMessage
        {
            public byte rejection;
        }

        [Header("Client")]
        [SerializeField] string playerName = "Player";

        readonly HashSet<NetworkConnection> pendingDisconnects = new HashSet<NetworkConnection>();

        ConnectionSessionModel sessionModel;
        IConnectionJoinPolicy joinPolicy;
        ConnectionRejectModel rejectModel;

        public string PlayerName
        {
            get => playerName;
            set => playerName = value;
        }

        // The one place that knows how the server stores the accepted request on a connection.
        public static AuthRequestMessage GetAuthRequest(NetworkConnectionToClient connection)
        {
            if (connection == null || connection.authenticationData is AuthRequestMessage == false)
                throw new System.InvalidOperationException("Player connection has no authentication data.");

            return (AuthRequestMessage)connection.authenticationData;
        }

        [Inject]
        void Construct(
            ConnectionSessionModel connectionSessionModel,
            IConnectionJoinPolicy connectionJoinPolicy,
            ConnectionRejectModel connectionRejectModel)
        {
            sessionModel = connectionSessionModel;
            joinPolicy = connectionJoinPolicy;
            rejectModel = connectionRejectModel;
        }

        public override void OnStartServer()
        {
            NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage, requireAuthentication: false);
        }

        public override void OnStopServer()
        {
            NetworkServer.UnregisterHandler<AuthRequestMessage>();
        }

        void OnAuthRequestMessage(NetworkConnectionToClient conn, AuthRequestMessage msg)
        {
            if (pendingDisconnects.Contains(conn))
                return;

            // Decided here, once: a player accepted at a station finishes joining even if the ship takes off meanwhile.
            ConnectionRejectReason rejection = joinPolicy.EvaluateJoin(msg.version, msg.steamId);
            if (rejection != ConnectionRejectReason.Accepted)
            {
                Reject(conn, rejection);
                return;
            }

            conn.authenticationData = msg;
            conn.Send(new AuthResponseMessage { rejection = (byte)ConnectionRejectReason.Accepted });
            ServerAccept(conn);
        }

        void Reject(NetworkConnectionToClient conn, ConnectionRejectReason reason)
        {
            pendingDisconnects.Add(conn);
            conn.Send(new AuthResponseMessage { rejection = (byte)reason });
            conn.isAuthenticated = false;
            StartCoroutine(DelayedDisconnect(conn));
        }

        IEnumerator DelayedDisconnect(NetworkConnectionToClient conn)
        {
            yield return new WaitForSeconds(1f);
            ServerReject(conn);
            yield return null;
            pendingDisconnects.Remove(conn);
        }

        public override void OnStartClient()
        {
            NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, requireAuthentication: false);
        }

        public override void OnStopClient()
        {
            NetworkClient.UnregisterHandler<AuthResponseMessage>();
        }

        public override void OnClientAuthenticate()
        {
            ulong steamId = 0;
            string name = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName;
            try
            {
                CSteamID id = SteamUser.GetSteamID();
                if (id.IsValid())
                {
                    steamId = id.m_SteamID;
                    string personaName = SteamFriends.GetPersonaName();
                    if (string.IsNullOrWhiteSpace(personaName) == false)
                        name = personaName;
                }
            }
            catch
            {
                // Direct IP still authenticates without Steam.
            }

            NetworkClient.Send(new AuthRequestMessage
            {
                steamId = steamId,
                playerName = name,
                version = Application.version,
                clientId = ConnectionPlayerIdentityService.GetOrCreateClientId()
            });
        }

        void OnAuthResponseMessage(AuthResponseMessage msg)
        {
            ConnectionRejectReason reason = (ConnectionRejectReason)msg.rejection;
            if (reason == ConnectionRejectReason.Accepted)
            {
                ClientAccept();
                return;
            }

            // An expected answer, not a failure: the menu shows the reason.
            rejectModel.Set(reason);
            sessionModel.NetworkManager.StopSessionAndReturnToMenu();
            if (NetworkClient.connection != null)
                ClientReject();
        }
    }
}
