namespace Features.InventoryModule.Scripts {
    // The selected slot and whether its item is stowed (the hand empty while the item stays in the slot).
    public readonly struct InventorySelection {
        public InventorySelection(int slot, bool isStowed) {
            Slot = slot;
            IsStowed = isStowed;
        }

        public int Slot { get; }
        public bool IsStowed { get; }
    }
}
