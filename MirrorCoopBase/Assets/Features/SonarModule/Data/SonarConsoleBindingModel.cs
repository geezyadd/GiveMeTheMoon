using UnityEngine;

namespace Features.SonarModule.Data {
    // The ship's sonar console on this peer: its synced model's writer (used on the server) and where it stands.
    public sealed class SonarConsoleBindingModel {
        public ISonarConsoleWriter Writer { get; private set; }
        public Transform Console { get; private set; }
        public bool IsBound => Writer != null;

        public void Bind(ISonarConsoleWriter writer, Transform console) {
            Writer = writer;
            Console = console;
        }

        public void Clear(ISonarConsoleWriter writer) {
            if (Writer != writer)
                return;

            Writer = null;
            Console = null;
        }
    }
}
