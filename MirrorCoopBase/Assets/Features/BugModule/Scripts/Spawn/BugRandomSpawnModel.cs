using System;
using Features.BugModule.Scripts.Configuration;

namespace Features.BugModule.Scripts.Spawn {
    // Server: whether random bug groups come in flight now; starts as the configuration says, the debug window and
    // the loop tests switch it. Bound in DataInstaller.
    public sealed class BugRandomSpawnModel {
        public bool IsEnabled { get; private set; }

        public event Action OnEnabledChanged;

        public BugRandomSpawnModel(BugConfiguration bugConfiguration) =>
            IsEnabled = bugConfiguration.IsRandomSpawnEnabled;

        public void SetEnabled(bool isEnabled) {
            if (IsEnabled == isEnabled)
                return;

            IsEnabled = isEnabled;
            OnEnabledChanged?.Invoke();
        }
    }
}
