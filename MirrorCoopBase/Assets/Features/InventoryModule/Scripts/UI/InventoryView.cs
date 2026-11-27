using System.Collections.Generic;
using UnityEngine;

namespace Features.InventoryModule.Scripts.UI {
    // The inventory bar at the bottom centre of the HUD: one slot view per inventory slot, laid out by the
    // container's layout group.
    public sealed class InventoryView : InventoryViewBase {
        [SerializeField] private GameObject _content;
        [SerializeField] private RectTransform _slotContainer;

        private readonly List<InventorySlotView> _slots = new List<InventorySlotView>(4);

        public override void CreateSlots(InventoryConfiguration inventoryConfiguration) {
            for (int i = _slots.Count; i < inventoryConfiguration.SlotCount; i++) {
                InventorySlotView slot = Instantiate(inventoryConfiguration.SlotViewPrefab, _slotContainer);
                slot.Setup(i + 1, inventoryConfiguration);
                _slots.Add(slot);
            }
        }

        public override void SetBarVisible(bool isVisible) =>
            _content.SetActive(isVisible);

        public override void SetSlotItem(int slot, Sprite icon, string itemName) =>
            _slots[slot].SetItem(icon, itemName);

        public override void ClearSlotItem(int slot) =>
            _slots[slot].ClearItem();

        public override void SetSelectedSlot(int slot, bool isStowed) {
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].SetSelected(i == slot, isStowed);
        }
    }
}
