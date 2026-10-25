using Features.ShipModule.Scripts.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipImpactModule.Scripts {
    // Carries a rock hit from the server to every peer: a one-shot effect, nothing a late joiner needs.
    public sealed class ShipImpactSync : NetworkBehaviour {
        private IShipImpactEventPublisher _shipImpactEventPublisher;
        private IComponentRegistry<ShipImpactSync> _shipImpactSyncRegistry;

        [Inject]
        private void InjectDependencies(IShipImpactEventPublisher shipImpactEventPublisher, IComponentRegistry<ShipImpactSync> shipImpactSyncRegistry) {
            _shipImpactEventPublisher = shipImpactEventPublisher;
            _shipImpactSyncRegistry = shipImpactSyncRegistry;
        }

        private void OnEnable() =>
            _shipImpactSyncRegistry.Add(this);

        private void OnDisable() =>
            _shipImpactSyncRegistry.Remove(this);

        [Server]
        public void ServerBroadcast(ShipRockImpact impact) =>
            RpcImpact(impact.Kind, impact.Point, impact.Strength);

        [ClientRpc]
        private void RpcImpact(ShipRockImpactKind kind, Vector3 point, float strength) =>
            _shipImpactEventPublisher.PublishImpact(new ShipRockImpact(kind, point, strength));
    }
}
