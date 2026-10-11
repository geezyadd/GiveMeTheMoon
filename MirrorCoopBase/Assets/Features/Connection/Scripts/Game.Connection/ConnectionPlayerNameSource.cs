using Features.NetworkModelModule.Scripts;
using Mirror;

namespace Game.Connection {
    public sealed class ConnectionPlayerNameSource : IPlayerNameSource {
        public string GetAuthenticatedName(NetworkConnectionToClient connection) =>
            ConnectionAuthenticator.GetAuthRequest(connection).playerName;
    }
}
