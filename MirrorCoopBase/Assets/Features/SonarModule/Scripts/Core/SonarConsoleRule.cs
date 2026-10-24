using Features.SonarModule.Scripts.Configurations;
using UnityEngine;

namespace Features.SonarModule.Scripts.Core {
    // One navigator at a time, at an active console, and only while standing at it.
    public sealed class SonarConsoleRule : ISonarConsoleRule {
        private readonly SonarConfiguration _sonarConfiguration;

        public SonarConsoleRule(SonarConfiguration sonarConfiguration) =>
            _sonarConfiguration = sonarConfiguration;

        public SonarConsoleEnterResult EvaluateEnter(string navigatorKey, string playerKey, bool isConsoleActive) {
            if (string.IsNullOrEmpty(playerKey))
                return SonarConsoleEnterResult.NoPlayer;

            if (isConsoleActive == false)
                return SonarConsoleEnterResult.Inactive;

            if (string.IsNullOrEmpty(navigatorKey))
                return SonarConsoleEnterResult.Entered;

            return navigatorKey == playerKey ? SonarConsoleEnterResult.AlreadyNavigator : SonarConsoleEnterResult.Busy;
        }

        public bool IsWithinReach(Vector3 playerPosition, Vector3 consolePosition) {
            float reach = _sonarConfiguration.ConsoleKeepOpenMeters;
            return (playerPosition - consolePosition).sqrMagnitude <= reach * reach;
        }
    }
}
