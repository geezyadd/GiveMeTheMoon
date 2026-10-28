using Features.ShipModule.Scripts.Contracts;
using Mirror;
using Zenject;

namespace Features.BugModule.Scripts.Network {
    // Carries a bug's death from the server to every peer, on the ship's network identity: a one-shot effect, nothing
    // a late joiner needs (a dead bug is simply not in the next agent batch).
    public sealed class BugDeathSync : NetworkBehaviour {
        private IBugDeathEventPublisher _bugDeathEventPublisher;
        private IComponentRegistry<BugDeathSync> _bugDeathSyncRegistry;

        [Inject]
        private void InjectDependencies(IBugDeathEventPublisher bugDeathEventPublisher, IComponentRegistry<BugDeathSync> bugDeathSyncRegistry) {
            _bugDeathEventPublisher = bugDeathEventPublisher;
            _bugDeathSyncRegistry = bugDeathSyncRegistry;
        }

        private void OnEnable() =>
            _bugDeathSyncRegistry.Add(this);

        private void OnDisable() =>
            _bugDeathSyncRegistry.Remove(this);

        [Server]
        public void ServerBroadcast(int bugId) =>
            RpcDeath(bugId);

        [ClientRpc]
        private void RpcDeath(int bugId) =>
            _bugDeathEventPublisher.PublishDeath(bugId);
    }
}
