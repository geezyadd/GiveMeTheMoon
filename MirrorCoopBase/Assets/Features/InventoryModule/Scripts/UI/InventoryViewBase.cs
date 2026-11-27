using Features.MvpModule;
using UnityEngine;

namespace Features.InventoryModule.Scripts.UI {
    public abstract class InventoryViewBase : ViewBehaviour {
        public abstract void CreateSlots(InventoryConfiguration inventoryConfiguration);
        public abstract void SetBarVisible(bool isVisible);
        public abstract void SetSlotItem(int slot, Sprite icon, string itemName);
        public abstract void ClearSlotItem(int slot);
        public abstract void SetSelectedSlot(int slot, bool isStowed);
    }
}
