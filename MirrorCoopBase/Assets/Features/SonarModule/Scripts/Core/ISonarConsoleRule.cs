using UnityEngine;

namespace Features.SonarModule.Scripts.Core {
    public interface ISonarConsoleRule {
        public SonarConsoleEnterResult EvaluateEnter(string navigatorKey, string playerKey, bool isConsoleActive);
        public bool IsWithinReach(Vector3 playerPosition, Vector3 consolePosition);
    }
}
