#if UNITY_INCLUDE_TESTS
using System;
using Features.NetworkModelModule.Scripts;
using Features.PlayerLifeModule.Scripts;
using Features.ShopModule.Scripts.Network;
using Game.Connection;
using Mirror;
using NUnit.Framework;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // A second crew member on the host, with no client of its own: it sends the server the messages a real client
    // sends while joining (authentication, ready, add player, map loaded) through the transport's receive callback,
    // so the server handles it like a real client; the test acts for it on the server.
    public sealed class LoopSmokeBot {
        public const string NAME = "SmokeBot";
        private const int CONNECTION_ID = 90001;
        private const string CLIENT_ID = "loop-smoke-bot";

        private readonly LoopSmokeBotConnection _connection;
        private readonly string _clientId;
        private readonly string _playerName;

        private LoopSmokeBot(LoopSmokeBotConnection connection, string clientId, string playerName) {
            _connection = connection;
            _clientId = clientId;
            _playerName = playerName;
        }

        public NetworkIdentity Identity =>
            _connection.identity;

        public bool IsAuthenticated =>
            _connection.isAuthenticated;

        public ConnectionRejectReason? AuthResponse =>
            _connection.AuthResponse;

        public PlayerLifeBody Life =>
            Identity.GetComponent<PlayerLifeBody>();

        public PlayerKey Key =>
            Life.Key;

        public ShopCustomer Customer =>
            Identity.GetComponent<ShopCustomer>();

        // Accepted at a station like a real client; the player object follows once the "map" is reported loaded.
        public static LoopSmokeBot Join() {
            LoopSmokeBot bot = Connect(CONNECTION_ID, CLIENT_ID, NAME);
            bot.Authenticate();
            Assert.IsTrue(bot.IsAuthenticated, "The server did not accept the bot.");
            bot.EnterMap();
            return bot;
        }

        // The transport's connect step: the connection exists, nothing is authenticated yet.
        public static LoopSmokeBot Connect(int connectionId, string clientId, string playerName) {
            LoopSmokeBotConnection connection = new LoopSmokeBotConnection(connectionId);
            Assert.IsTrue(NetworkServer.AddConnection(connection), "The bot connection id is taken.");
            NetworkServer.OnConnectedEvent?.Invoke(connection);
            return new LoopSmokeBot(connection, clientId, playerName);
        }

        public void Authenticate() =>
            Send(new ConnectionAuthenticator.AuthRequestMessage {
                steamId = 0,
                playerName = _playerName,
                version = Application.version,
                clientId = _clientId
            });

        // What a client sends while it loads the server's scene and after it has loaded it.
        public void EnterMap() {
            Send(new ReadyMessage());
            Send(new AddPlayerMessage());
            Send(new MapLoadedMessage());
        }

        // The same steps as NetworkServer on a transport disconnect.
        public void Leave() {
            _connection.Cleanup();
            NetworkServer.RemoveConnection(_connection.connectionId);
            NetworkServer.OnDisconnectedEvent?.Invoke(_connection);
        }

        private void Send<T>(T message) where T : struct, NetworkMessage =>
            LoopSmokeTransportInput.Deliver(message, ReceiveOnServer);

        private void ReceiveOnServer(ArraySegment<byte> batch, int channelId) =>
            Transport.active.OnServerDataReceived.Invoke(_connection.connectionId, batch, channelId);
    }
}
#endif
