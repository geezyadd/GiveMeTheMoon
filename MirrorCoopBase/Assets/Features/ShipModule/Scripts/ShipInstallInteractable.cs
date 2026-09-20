using Features.GrabModule.Scripts;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipInstallInteractable : InteractableBase {
        [SerializeField] private ShipSocket _socket;

        private Grabbable _cachedHeld;
        private ShipItem _cachedItem;

        public override bool CanUse(NetworkIdentity user, GrabController grab) {
            ShipItem item = ResolveHeldItem(grab);
            return _socket != null && item != null && _socket.CanAccept(item.Type);
        }

        public override void ServerUse(NetworkIdentity user, GrabController grab) {
            ShipItem item = ResolveHeldItem(grab);
            if (_socket == null || item == null)
                return;

            if (_socket.ServerTryInstall(item.Type, item.View) == false)
                return;

            grab.ServerConsumeHeld();
        }

        private ShipItem ResolveHeldItem(GrabController grab) {
            Grabbable held = grab != null ? grab.Held : null;
            if (held == _cachedHeld)
                return _cachedItem;

            _cachedHeld = held;
            _cachedItem = held != null ? held.GetComponent<ShipItem>() : null;
            return _cachedItem;
        }
    }
}
