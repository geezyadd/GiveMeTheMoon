using System;
using Features.FieldAgentModule.Scripts.Configuration;
using Zenject;

namespace Features.BugModule.Scripts.Configuration {
    // The one startup check of the bug data: its kind is a listed field agent kind, its sonar kind and death effect
    // are set, and the group sizes and timings make sense.
    public sealed class BugConfigurationValidator : IInitializable {
        private const string ERROR_FORMAT = "{0}: {1}";

        private readonly BugConfiguration _bugConfiguration;
        private readonly IBugKindService _bugKindService;

        public BugConfigurationValidator(BugConfiguration bugConfiguration, IBugKindService bugKindService) {
            _bugConfiguration = bugConfiguration;
            _bugKindService = bugKindService;
        }

        public void Initialize() {
            if (_bugConfiguration.Profile == null || _bugKindService.TryGetKindIndex(out byte _) == false)
                Fail($"{nameof(BugConfiguration.Profile)} must be one of {nameof(FieldAgentConfiguration)}.{nameof(FieldAgentConfiguration.Profiles)}.");

            if (_bugConfiguration.SonarKind == null)
                Fail($"{nameof(BugConfiguration.SonarKind)} is not set.");

            if (_bugConfiguration.DeathEffectPrefab == null)
                Fail($"{nameof(BugConfiguration.DeathEffectPrefab)} is not set.");

            if (_bugConfiguration.GroupMinSize > _bugConfiguration.GroupMaxSize)
                Fail($"{nameof(BugConfiguration.GroupMinSize)} {_bugConfiguration.GroupMinSize} is above {nameof(BugConfiguration.GroupMaxSize)} {_bugConfiguration.GroupMaxSize}.");

            if (_bugConfiguration.FreezeShare >= 1f)
                Fail($"{nameof(BugConfiguration.FreezeShare)} must leave a dash in the cycle.");
        }

        private void Fail(string message) =>
            throw new InvalidOperationException(string.Format(ERROR_FORMAT, _bugConfiguration.name, message));
    }
}
