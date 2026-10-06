using Mirror;

namespace Features.NetworkModelModule.Scripts {
    public interface IPlayerNameSource {
        // The name the client sent when it authenticated, not cleaned up yet.
        public string GetAuthenticatedName(NetworkConnectionToClient connection);
    }
}
