using Features.SonarModule.Data;
using Mirror;

namespace Features.SonarModule.Scripts.Systems {
    // Server: the crew's marks.
    public interface ISonarMarkSystem {
        public void ServerMark(ISonarConsoleWriter writer, NetworkConnectionToClient connection, string contactId);
    }
}
