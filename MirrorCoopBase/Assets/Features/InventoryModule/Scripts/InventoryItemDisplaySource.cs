using Features.GrabModule.Scripts;
using Mirror;
using UnityEngine;

namespace Features.InventoryModule.Scripts {
    // How a slot's item shows on this peer: the item's own display (its definition's icon and name), or its object's
    // name for a storable item without one.
    public sealed class InventoryItemDisplaySource : IInventoryItemDisplaySource {
        private const string CLONE_SUFFIX = "(Clone)";

        public bool TryGetDisplay(uint itemNetId, out Sprite icon, out string itemName) {
            icon = null;
            itemName = string.Empty;
            if (TryGetSpawned(itemNetId, out NetworkIdentity identity) == false)
                return false;

            if (identity.TryGetComponent(out IInventoryItemDisplay display)) {
                icon = display.InventoryIcon;
                itemName = display.InventoryName;
                return true;
            }

            itemName = identity.name.Replace(CLONE_SUFFIX, string.Empty).Trim();
            return true;
        }

        private static bool TryGetSpawned(uint netId, out NetworkIdentity identity) {
            identity = null;
            if (NetworkClient.active && NetworkClient.spawned.TryGetValue(netId, out identity) && identity != null)
                return true;

            return NetworkServer.active && NetworkServer.spawned.TryGetValue(netId, out identity) && identity != null;
        }
    }
}
