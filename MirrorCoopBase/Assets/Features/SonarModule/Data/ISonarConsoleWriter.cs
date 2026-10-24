using Mirror;

namespace Features.SonarModule.Data {
    // The server's writes to the console's synced model, and the one message that seats a navigator on their peer.
    public interface ISonarConsoleWriter {
        public void ServerSetNavigatorKey(string value);
        public void ServerAddMarks(SonarMark value);
        public void ServerSetMarks(int index, SonarMark value);
        public void ServerRemoveMarksAt(int index);
        public void ServerClearMarks();
        public void ServerOpenConsole(NetworkConnectionToClient connection, string navigatorKey);
    }
}
