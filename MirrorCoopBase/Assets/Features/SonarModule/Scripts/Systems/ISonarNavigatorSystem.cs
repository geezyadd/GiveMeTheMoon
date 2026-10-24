using Features.SonarModule.Data;
using Mirror;

namespace Features.SonarModule.Scripts.Systems {
    // Server: who stands at the sonar console.
    public interface ISonarNavigatorSystem {
        public void ServerEnter(ISonarConsoleWriter writer, NetworkConnectionToClient connection, bool isConsoleActive);
        public void ServerLeave(ISonarConsoleWriter writer, NetworkConnectionToClient connection);
    }
}
