using Features.GrabModule.Scripts;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipLaunchInteractable : InteractableBase {
        [SerializeField] private ShipLaunchLever _lever;

        public override bool CanUse(NetworkIdentity user, GrabController grab) {
            if (_lever == null)
                return false;

            return grab == null || grab.IsHolding == false;
        }

        public override void ServerUse(NetworkIdentity user, GrabController grab) {
            if (_lever == null)
                return;

            _lever.ServerTryLaunch();
        }
    }
}
