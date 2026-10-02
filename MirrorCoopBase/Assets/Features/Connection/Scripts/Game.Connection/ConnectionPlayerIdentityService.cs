using System;
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

        public static string GetOrCreateClientId() {
            string id = PlayerPrefs.GetString(ClientIdPreferenceKey, string.Empty);
            if (string.IsNullOrEmpty(id) == false)
                return id;

            id = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(ClientIdPreferenceKey, id);
            PlayerPrefs.Save();
            return id;
        }

        public PlayerKey GetKey(NetworkConnectionToClient connection) {
            if (connection == null || connection.authenticationData is ConnectionAuthenticator.AuthRequestMessage == false)
                throw new InvalidOperationException("Player connection has no authentication data.");

            string baseId = GetBaseKeyId((ConnectionAuthenticator.AuthRequestMessage)connection.authenticationData);
            if (string.IsNullOrEmpty(baseId))
                throw new InvalidOperationException("Direct connection has no client id.");

            int sameKeyBefore = CountLowerConnectionsWithKey(connection.connectionId, baseId);
            if (sameKeyBefore == 0)
                return new PlayerKey(baseId);

            return new PlayerKey(baseId + DUPLICATE_KEY_SEPARATOR + (sameKeyBefore + 1).ToString(CultureInfo.InvariantCulture));
        }

        private static string GetBaseKeyId(ConnectionAuthenticator.AuthRequestMessage message) {
            if (message.steamId != 0)
                return STEAM_KEY_PREFIX + message.steamId.ToString(CultureInfo.InvariantCulture);

            return string.IsNullOrEmpty(message.clientId)
                ? string.Empty
                : CLIENT_KEY_PREFIX + message.clientId;
        }

        // Two game instances on one Steam account (editor host + build on one PC) send the same steam id. Without a
        // suffix both players would share every per-player model: one death marks the other dead and blocks its controls.
        // The lowest live connection keeps the plain key, so a real rejoin still finds its record.
        private static int CountLowerConnectionsWithKey(int connectionId, string baseId) {
            int count = 0;
            foreach (NetworkConnectionToClient other in NetworkServer.connections.Values) {
                if (other.connectionId >= connectionId)
                    continue;

                if (other.authenticationData is ConnectionAuthenticator.AuthRequestMessage message
                    && GetBaseKeyId(message) == baseId)
                    count++;
            }

            return count;
        }
    }
}
