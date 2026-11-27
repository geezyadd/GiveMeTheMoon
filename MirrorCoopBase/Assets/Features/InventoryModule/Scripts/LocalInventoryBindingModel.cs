using System;
using Features.InventoryModule.Scripts.Generated;

namespace Features.InventoryModule.Scripts {
    // The local player's inventory for the HUD, and the slot the player just picked before the server confirms it,
    // so the highlight moves at once; a refused pick goes back to the server's slot.
    public sealed class LocalInventoryBindingModel {
        public const int NO_PREDICTION = -1;

        public IReadOnlyPlayerInventoryModel Inventory { get; private set; }
        public int PredictedSlot { get; private set; } = NO_PREDICTION;
        public bool IsPredicting => PredictedSlot != NO_PREDICTION;
        public int DisplayedSlot => IsPredicting ? PredictedSlot : Inventory != null ? Inventory.SelectedSlot : 0;
        public bool IsDisplayedSlotStowed => IsPredicting == false && Inventory != null && Inventory.IsStowed;

        public event Action OnInventoryChanged;
        public event Action OnPredictedSlotChanged;

        public void Bind(IReadOnlyPlayerInventoryModel inventory) {
            Inventory = inventory;
            PredictedSlot = NO_PREDICTION;
            OnInventoryChanged?.Invoke();
        }

        public void Clear() {
            if (Inventory == null)
                return;

            Inventory = null;
            PredictedSlot = NO_PREDICTION;
            OnInventoryChanged?.Invoke();
        }

        public void PredictSlot(int slot) {
            if (PredictedSlot == slot)
                return;

            PredictedSlot = slot;
            OnPredictedSlotChanged?.Invoke();
        }

        // The server confirmed or refused this slot; a newer pick stays predicted.
        public void EndPrediction(int slot) {
            if (PredictedSlot != slot)
                return;

            PredictedSlot = NO_PREDICTION;
            OnPredictedSlotChanged?.Invoke();
        }
    }
}
