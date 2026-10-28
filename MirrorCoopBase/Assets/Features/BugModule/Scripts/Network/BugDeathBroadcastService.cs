using Features.ShipModule.Scripts.Contracts;

namespace Features.BugModule.Scripts.Network {
    // Sends a death through the bug death sync of the run's ship; without a ship there is no bug to die.
    public sealed class BugDeathBroadcastService : IBugDeathBroadcast {
        private readonly IReadOnlyComponentRegistry<BugDeathSync> _bugDeathSyncRegistry;

        public BugDeathBroadcastService(IReadOnlyComponentRegistry<BugDeathSync> bugDeathSyncRegistry) =>
            _bugDeathSyncRegistry = bugDeathSyncRegistry;

        public void ServerBroadcastDeath(int bugId) {
            foreach (BugDeathSync sync in _bugDeathSyncRegistry.Items)
                sync.ServerBroadcast(bugId);
        }
    }
}
