using UnityEngine;

namespace Features.InventoryModule.Scripts {
    public interface IInventoryItemDisplaySource {
        public bool TryGetDisplay(uint itemNetId, out Sprite icon, out string itemName);
    }
}
