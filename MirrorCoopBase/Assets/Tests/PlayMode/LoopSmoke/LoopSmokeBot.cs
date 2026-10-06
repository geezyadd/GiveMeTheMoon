#if UNITY_INCLUDE_TESTS
using Features.NetworkModelModule.Scripts;
using Features.PlayerLifeModule.Scripts;
using Features.ShopModule.Scripts.Network;
using Game.Connection;
using Mirror;
using NUnit.Framework;

namespace Tests.PlayMode.LoopSmoke {
    // A second crew member on the host, joined and left the way the server handles a real client, but with no client
    // of its own: the test acts for it on the server.
    public sealed class LoopSmokeBot {
        public const string NAME = "SmokeBot";
        private const int CONNECTION_ID = 90001;
        private const string CLIENT_ID = "loop-smoke-bot";

        private readonly LoopSmokeBotConnection _connection;

        private LoopSmokeBot(LoopSmokeBotConnection connection) =>
            _connection = connection;

        public NetworkIdentity Identity =>
            _connection.identity;

        public PlayerLifeBody Life =>
            Identity.GetComponent<PlayerLifeBody>();

        public PlayerKey Key =>
            Life.Key;

        public ShopCustomer Customer =>
            Identity.GetComponent<ShopCustomer>();

        public static LoopSmokeBot Join(ConnectionNetworkManager manager) {
            LoopSmokeBotConnection connection = new LoopSmokeBotConnection(CONNECTION_ID);
            connection.authenticationData = new ConnectionAuthenticator.AuthRequestMessage {
                clientId = CLIENT_ID,
                playerName = NAME
            };
            connection.isAuthenticated = true;
            Assert.IsTrue(NetworkServer.AddConnection(connection), "The bot connection id is taken.");
            NetworkServer.SetClientReady(connection);
            manager.OnServerAddPlayer(connection);
            return new LoopSmokeBot(connection);
        }

        // The same steps as NetworkServer on a transport disconnect.
        public void Leave() {
            _connection.Cleanup();
            NetworkServer.RemoveConnection(_connection.connectionId);
            NetworkServer.OnDisconnectedEvent?.Invoke(_connection);
        }
    }
}
#endif
