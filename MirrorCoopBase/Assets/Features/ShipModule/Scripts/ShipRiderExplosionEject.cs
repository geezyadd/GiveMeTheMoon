using System;
using System.Collections.Generic;
using Features.ExplosionModule.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts {
    // On the server, an explosion's wave that is strong enough to knock this player down stands them up from their
    // seat or the helm, so the knockdown can throw the body (PlayerBodyModule's reaction waits for the seat to let go).
    public sealed class ShipRiderExplosionEject : MonoBehaviour, IExplosionReceiver {
        [SerializeField] private ShipRider _rider;

        private IExplosionReceiverRegistry _registry;

        public Vector3 Center => transform.position;
        public IReadOnlyList<Rigidbody> Parts => Array.Empty<Rigidbody>();

        [Inject]
        private void Construct(IExplosionReceiverRegistry registry) {
            _registry = registry;
            if (isActiveAndEnabled)
                _registry.Add(this);
        }

        private void OnEnable() =>
            _registry?.Add(this);

        private void OnDisable() =>
            _registry?.Remove(this);

        public void ReceiveExplosion(ExplosionHit hit) {
            if (NetworkServer.active == false || hit.KnocksDown == false || _rider == null || _rider.IsSeated == false)
                return;

            _rider.ServerStand();
        }
    }
}
