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
        private const int THIRD_CONNECTION_ID = 9003;
        private const int REJOIN_CONNECTION_ID = 9004;
        private const string SHARED_STEAM_KEY = "steam:76561198000000001";
        private const string OTHER_STEAM_KEY = "steam:76561198000000002";

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

        [Test]
        public void WhenThreeConnectionsShareSteamId_ThenEachGetsNextSuffix() {
            NetworkConnectionToClient host = AddConnection(HOST_CONNECTION_ID, SHARED_STEAM_ID);
            NetworkConnectionToClient client = AddConnection(CLIENT_CONNECTION_ID, SHARED_STEAM_ID);
            NetworkConnectionToClient third = AddConnection(THIRD_CONNECTION_ID, SHARED_STEAM_ID);

            Assert.That(_service.GetKey(host), Is.EqualTo(new PlayerKey(SHARED_STEAM_KEY)));
            Assert.That(_service.GetKey(client), Is.EqualTo(new PlayerKey(SHARED_STEAM_KEY + "#2")));
            Assert.That(_service.GetKey(third), Is.EqualTo(new PlayerKey(SHARED_STEAM_KEY + "#3")));
        }

        [Test]
        public void WhenLowerConnectionLeaves_ThenHigherDuplicatesKeepTheirKeys() {
            NetworkConnectionToClient host = AddConnection(HOST_CONNECTION_ID, SHARED_STEAM_ID);
            NetworkConnectionToClient client = AddConnection(CLIENT_CONNECTION_ID, SHARED_STEAM_ID);
            NetworkConnectionToClient third = AddConnection(THIRD_CONNECTION_ID, SHARED_STEAM_ID);
            _service.GetKey(host);
            _service.GetKey(client);
            _service.GetKey(third);

            RemoveConnection(HOST_CONNECTION_ID);

            Assert.That(_service.GetKey(client), Is.EqualTo(new PlayerKey(SHARED_STEAM_KEY + "#2")));
            Assert.That(_service.GetKey(third), Is.EqualTo(new PlayerKey(SHARED_STEAM_KEY + "#3")));
        }

        [Test]
        public void WhenDuplicateLeaves_AndRejoins_ThenItGetsItsOldKeyBack() {
            NetworkConnectionToClient host = AddConnection(HOST_CONNECTION_ID, SHARED_STEAM_ID);
            NetworkConnectionToClient client = AddConnection(CLIENT_CONNECTION_ID, SHARED_STEAM_ID);
            _service.GetKey(host);
            _service.GetKey(client);

            RemoveConnection(CLIENT_CONNECTION_ID);
            NetworkConnectionToClient rejoined = AddConnection(REJOIN_CONNECTION_ID, SHARED_STEAM_ID);

            Assert.That(_service.GetKey(rejoined), Is.EqualTo(new PlayerKey(SHARED_STEAM_KEY + "#2")));
            Assert.That(_service.GetKey(host), Is.EqualTo(new PlayerKey(SHARED_STEAM_KEY)));
        }

        [Test]
        public void WhenMiddleOfThreeLeaves_AndRejoins_ThenItGetsLowestFreeSuffix() {
            NetworkConnectionToClient host = AddConnection(HOST_CONNECTION_ID, SHARED_STEAM_ID);
            NetworkConnectionToClient client = AddConnection(CLIENT_CONNECTION_ID, SHARED_STEAM_ID);
            NetworkConnectionToClient third = AddConnection(THIRD_CONNECTION_ID, SHARED_STEAM_ID);
            _service.GetKey(host);
            _service.GetKey(client);
            _service.GetKey(third);

            RemoveConnection(CLIENT_CONNECTION_ID);
            NetworkConnectionToClient rejoined = AddConnection(REJOIN_CONNECTION_ID, SHARED_STEAM_ID);

            Assert.That(_service.GetKey(rejoined), Is.EqualTo(new PlayerKey(SHARED_STEAM_KEY + "#2")));
            Assert.That(_service.GetKey(third), Is.EqualTo(new PlayerKey(SHARED_STEAM_KEY + "#3")));
        }

        [Test]
        public void WhenConnectionIdIsReused_ThenNewConnectionGetsFreshKey() {
            NetworkConnectionToClient host = AddConnection(HOST_CONNECTION_ID, SHARED_STEAM_ID);
            NetworkConnectionToClient client = AddConnection(CLIENT_CONNECTION_ID, SHARED_STEAM_ID);
            _service.GetKey(host);
            _service.GetKey(client);

            RemoveConnection(HOST_CONNECTION_ID);
            NetworkConnectionToClient reused = AddConnection(HOST_CONNECTION_ID, OTHER_STEAM_ID);

            Assert.That(_service.GetKey(reused), Is.EqualTo(new PlayerKey(OTHER_STEAM_KEY)));
        }

        private void RemoveConnection(int connectionId) {
            NetworkServer.connections.Remove(connectionId);
            _connectionIds.Remove(connectionId);
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
