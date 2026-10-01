using System;
using System.Globalization;
using Features.NetworkModelModule.Scripts;
using Mirror;
using UnityEngine;

namespace Game.Connection {
    public sealed class ConnectionPlayerIdentityService : IPlayerIdentityService {
        public const string ClientIdPreferenceKey = "network-model.client-id";

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

            ConnectionAuthenticator.AuthRequestMessage message =
                (ConnectionAuthenticator.AuthRequestMessage)connection.authenticationData;
            if (message.steamId != 0)
                return new PlayerKey("steam:" + message.steamId.ToString(CultureInfo.InvariantCulture));

            if (string.IsNullOrEmpty(message.clientId))
                throw new InvalidOperationException("Direct connection has no client id.");

            return new PlayerKey("client:" + message.clientId);
        }
    }
}
