using System;
using System.Collections.Generic;
using System.Globalization;
using Features.NetworkModelModule.Scripts;
using Mirror;
using UnityEngine;

namespace Game.Connection {
    public sealed class ConnectionPlayerIdentityService : IPlayerIdentityService {
        public const string ClientIdPreferenceKey = "network-model.client-id";
        private const string STEAM_KEY_PREFIX = "steam:";
        private const string CLIENT_KEY_PREFIX = "client:";
        private const string DUPLICATE_KEY_SEPARATOR = "#";
        private const int FIRST_DUPLICATE_SUFFIX = 2;

        public static string GetOrCreateClientId() {
            string id = PlayerPrefs.GetString(ClientIdPreferenceKey, string.Empty);
            if (string.IsNullOrEmpty(id) == false)
                return id;

            id = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(ClientIdPreferenceKey, id);
            PlayerPrefs.Save();
            return id;
        }

        private readonly Dictionary<NetworkConnectionToClient, PlayerKey> _keyByConnection =
            new Dictionary<NetworkConnectionToClient, PlayerKey>();
        private readonly List<NetworkConnectionToClient> _closedConnections = new List<NetworkConnectionToClient>();

        public PlayerKey GetKey(NetworkConnectionToClient connection) {
            ConnectionAuthenticator.AuthRequestMessage request = ConnectionAuthenticator.GetAuthRequest(connection);

            ForgetClosedConnections();
            if (_keyByConnection.TryGetValue(connection, out PlayerKey assigned))
                return assigned;

            string baseId = GetBaseKeyId(request);
            if (string.IsNullOrEmpty(baseId))
                throw new InvalidOperationException("Direct connection has no client id.");

            PlayerKey key = CreateFreeKey(baseId);
            _keyByConnection.Add(connection, key);
            return key;
        }

        private static string GetBaseKeyId(ConnectionAuthenticator.AuthRequestMessage message) {
            if (message.steamId != 0)
                return STEAM_KEY_PREFIX + message.steamId.ToString(CultureInfo.InvariantCulture);

            return string.IsNullOrEmpty(message.clientId)
                ? string.Empty
                : CLIENT_KEY_PREFIX + message.clientId;
        }

        // The key is assigned once per connection and kept until it disconnects: recomputing it from the connection
        // order would hand a player another player's record when a lower connection leaves.
        private void ForgetClosedConnections() {
            _closedConnections.Clear();
            foreach (NetworkConnectionToClient connection in _keyByConnection.Keys) {
                if (IsOpen(connection) == false)
                    _closedConnections.Add(connection);
            }

            for (int i = 0; i < _closedConnections.Count; i++)
                _keyByConnection.Remove(_closedConnections[i]);
        }

        private static bool IsOpen(NetworkConnectionToClient connection) =>
            NetworkServer.connections.TryGetValue(connection.connectionId, out NetworkConnectionToClient open)
            && open == connection;

        // Two game instances on one Steam account (editor host + build on one PC) send the same steam id. Without a
        // suffix both players would share every per-player model: one death marks the other dead and blocks its controls.
        // A rejoin gets the plain key back when it is offline; a duplicate of an online key gets the lowest free suffix.
        private PlayerKey CreateFreeKey(string baseId) {
            if (IsKeyOnline(baseId) == false)
                return new PlayerKey(baseId);

            int suffix = FIRST_DUPLICATE_SUFFIX;
            while (IsKeyOnline(CreateDuplicateId(baseId, suffix)))
                suffix++;

            return new PlayerKey(CreateDuplicateId(baseId, suffix));
        }

        private bool IsKeyOnline(string id) =>
            _keyByConnection.ContainsValue(new PlayerKey(id));

        private static string CreateDuplicateId(string baseId, int suffix) =>
            baseId + DUPLICATE_KEY_SEPARATOR + suffix.ToString(CultureInfo.InvariantCulture);
    }
}
