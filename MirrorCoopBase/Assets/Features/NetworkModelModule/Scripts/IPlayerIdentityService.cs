using Mirror;

namespace Features.NetworkModelModule.Scripts {
    public interface IPlayerIdentityService {
        PlayerKey GetKey(NetworkConnectionToClient connection);
    }
}
