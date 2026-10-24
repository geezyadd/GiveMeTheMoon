using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Impacts {
    // Carries a rock hit from the server to every peer: a one-shot effect, nothing a late joiner needs.
    public sealed class ShipImpactSync : NetworkBehaviour {
        private IShipImpactEventPublisher _shipImpactEventPublisher;

        [Inject]
        private void InjectDependencies(IShipImpactEventPublisher shipImpactEventPublisher) =>
            _shipImpactEventPublisher = shipImpactEventPublisher;

        [Server]
        internal void ServerBroadcast(ShipRockImpact impact) =>
            RpcImpact(impact.Kind, impact.Point, impact.Strength);

        [ClientRpc]
        private void RpcImpact(ShipRockImpactKind kind, Vector3 point, float strength) =>
            _shipImpactEventPublisher.PublishImpact(new ShipRockImpact(kind, point, strength));
    }
}
