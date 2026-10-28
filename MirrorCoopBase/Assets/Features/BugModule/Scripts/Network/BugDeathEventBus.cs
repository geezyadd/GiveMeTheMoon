using System;

namespace Features.BugModule.Scripts.Network {
    public sealed class BugDeathEventBus : IBugDeathEvents, IBugDeathEventPublisher {
        public event Action<int> OnBugDied;

        public void PublishDeath(int bugId) =>
            OnBugDied?.Invoke(bugId);
    }
}
