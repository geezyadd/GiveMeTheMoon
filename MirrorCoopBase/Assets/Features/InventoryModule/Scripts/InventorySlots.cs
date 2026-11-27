using System.Collections.Generic;

namespace Features.InventoryModule.Scripts {
    // The slot rules over one snapshot of a player's inventory: item net ids per slot (0 = empty), the slot count
    // and the selection. The hand holds the selected slot's item unless it is stowed.
    public readonly struct InventorySlots {
        public const int NO_SLOT = -1;

        private readonly IReadOnlyList<uint> _itemNetIds;

        public InventorySlots(IReadOnlyList<uint> itemNetIds, int count, InventorySelection selection) {
            _itemNetIds = itemNetIds;
            Count = count;
            Selection = selection;
        }

        public int Count { get; }
        public InventorySelection Selection { get; }
        public bool IsFull => FindFreeSlot() == NO_SLOT;
        public uint HandItemNetId => HandItemNetIdFor(Selection);

        public bool IsValidSlot(int slot) =>
            slot >= 0 && slot < Count;

        public uint ItemAt(int slot) =>
            IsValidSlot(slot) && slot < _itemNetIds.Count ? _itemNetIds[slot] : 0;

        public uint HandItemNetIdFor(InventorySelection selection) =>
            selection.IsStowed ? 0 : ItemAt(selection.Slot);

        public int IndexOf(uint itemNetId) {
            if (itemNetId == 0)
                return NO_SLOT;

            for (int slot = 0; slot < Count; slot++) {
                if (ItemAt(slot) == itemNetId)
                    return slot;
            }

            return NO_SLOT;
        }

        // The selected slot when it is empty, else the first free one; NO_SLOT when every slot is taken.
        public int FindPickupSlot() =>
            IsValidSlot(Selection.Slot) && ItemAt(Selection.Slot) == 0 ? Selection.Slot : FindFreeSlot();

        // The selected slot's number again stows its item or takes it out; another slot shows its own item.
        public InventorySelection Select(int slot) {
            if (slot != Selection.Slot)
                return new InventorySelection(slot, false);

            return ItemAt(slot) != 0 ? new InventorySelection(slot, Selection.IsStowed == false) : Selection;
        }

        // One slot per step, wrapping around; there is no empty-hand step.
        public int Cycle(int fromSlot, int steps) {
            if (Count <= 0)
                return NO_SLOT;

            int slot = (fromSlot + steps) % Count;
            return slot < 0 ? slot + Count : slot;
        }

        public int FindFreeSlot() {
            for (int slot = 0; slot < Count; slot++) {
                if (ItemAt(slot) == 0)
                    return slot;
            }

            return NO_SLOT;
        }
    }
}
