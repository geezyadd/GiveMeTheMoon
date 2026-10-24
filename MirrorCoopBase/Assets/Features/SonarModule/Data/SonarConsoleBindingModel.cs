using UnityEngine;

namespace Features.SonarModule.Data {
    // The ship's sonar console on this peer: its synced model's writer (used on the server), where it stands and the
    // ship socket of the radar module it belongs to.
    public sealed class SonarConsoleBindingModel {
        public ISonarConsoleWriter Writer { get; private set; }
        public Transform Console { get; private set; }
        public string SocketId { get; private set; }
        public bool IsBound => Writer != null;

        public void Bind(ISonarConsoleWriter writer, Transform console, string socketId) {
            Writer = writer;
            Console = console;
            SocketId = socketId;
        }

        public void Clear(ISonarConsoleWriter writer) {
            if (Writer != writer)
                return;

            Writer = null;
            Console = null;
            SocketId = null;
        }
    }
}
