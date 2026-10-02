using System.Collections.Generic;
using Features.NetworkModelModule.Scripts;
using Mirror;
using NUnit.Framework;

namespace Game.Connection.Editor.Tests {
    public sealed class ConnectionPlayerIdentityServiceTests {
        private const ulong SHARED_STEAM_ID = 76561198000000001;
        private const ulong OTHER_STEAM_ID = 76561198000000002;
        private const int HOST_CONNECTION_ID = 9001;
        private const int CLIENT_CONNECTION_ID = 9002;
        private const string SHARED_STEAM_KEY = "steam:76561198000000001";

        private readonly ConnectionPlayerIdentityService _service = new ConnectionPlayerIdentityService();
        private readonly List<int> _connectionIds = new List<int>();

        [TearDown]
        public void RemoveConnections() {
            for (int i = 0; i < _connectionIds.Count; i++)
                NetworkServer.connections.Remove(_connectionIds[i]);

            _connectionIds.Clear();
        }

        [Test]
        public void WhenOnlyOneConnectionHasSteamId_ThenKeyIsPlainSteamId() {
            NetworkConnectionToClient host = AddConnection(HOST_CONNECTION_ID, SHARED_STEAM_ID);

            Assert.That(_service.GetKey(host), Is.EqualTo(new PlayerKey(SHARED_STEAM_KEY)));
        }

        [Test]
        public void WhenTwoConnectionsShareSteamId_ThenEachGetsOwnKey() {
            NetworkConnectionToClient host = AddConnection(HOST_CONNECTION_ID, SHARED_STEAM_ID);
            NetworkConnectionToClient client = AddConnection(CLIENT_CONNECTION_ID, SHARED_STEAM_ID);

            Assert.That(_service.GetKey(host), Is.EqualTo(new PlayerKey(SHARED_STEAM_KEY)));
            Assert.That(_service.GetKey(client), Is.EqualTo(new PlayerKey(SHARED_STEAM_KEY + "#2")));
        }

        [Test]
        public void WhenSteamIdsDiffer_ThenNoSuffix() {
            AddConnection(HOST_CONNECTION_ID, OTHER_STEAM_ID);
            NetworkConnectionToClient client = AddConnection(CLIENT_CONNECTION_ID, SHARED_STEAM_ID);

            Assert.That(_service.GetKey(client), Is.EqualTo(new PlayerKey(SHARED_STEAM_KEY)));
        }

        [Test]
        public void WhenLowerDuplicateDisconnects_AndPlayerRejoins_ThenKeyIsPlainSteamId() {
            AddConnection(HOST_CONNECTION_ID, SHARED_STEAM_ID);
            NetworkServer.connections.Remove(HOST_CONNECTION_ID);
            NetworkConnectionToClient rejoined = AddConnection(CLIENT_CONNECTION_ID, SHARED_STEAM_ID);

            Assert.That(_service.GetKey(rejoined), Is.EqualTo(new PlayerKey(SHARED_STEAM_KEY)));
        }

        private NetworkConnectionToClient AddConnection(int connectionId, ulong steamId) {
            NetworkConnectionToClient connection = new NetworkConnectionToClient(connectionId) {
                authenticationData = new ConnectionAuthenticator.AuthRequestMessage { steamId = steamId }
            };
            NetworkServer.connections.Add(connectionId, connection);
            _connectionIds.Add(connectionId);
            return connection;
        }
    }
}
