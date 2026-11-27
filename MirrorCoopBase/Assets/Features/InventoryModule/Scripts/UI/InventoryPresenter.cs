using Features.InventoryModule.Scripts.Generated;
using Features.MvpModule;
using UnityEngine;

namespace Features.InventoryModule.Scripts.UI {
    // The local player's inventory as the HUD bar: each slot's item, and the selected slot (the one just picked
    // while the server has not confirmed it yet). No local inventory (before the player spawns): no bar.
    public sealed class InventoryPresenter : PresenterBehaviour<InventoryViewBase> {
        private readonly LocalInventoryBindingModel _localInventoryBindingModel;
        private readonly InventoryConfiguration _inventoryConfiguration;
        private readonly IInventoryItemDisplaySource _inventoryItemDisplaySource;

        private IReadOnlyPlayerInventoryModel _inventory;

        public InventoryPresenter(
            LocalInventoryBindingModel localInventoryBindingModel,
            InventoryConfiguration inventoryConfiguration,
            IInventoryItemDisplaySource inventoryItemDisplaySource) {
            _localInventoryBindingModel = localInventoryBindingModel;
            _inventoryConfiguration = inventoryConfiguration;
            _inventoryItemDisplaySource = inventoryItemDisplaySource;
        }

        protected override void OnViewSet() {
            View.CreateSlots(_inventoryConfiguration);
            _localInventoryBindingModel.OnInventoryChanged += Rebind;
            _localInventoryBindingModel.OnPredictedSlotChanged += RefreshSelection;
            Rebind();
        }

        protected override void OnDisposed() {
            _localInventoryBindingModel.OnInventoryChanged -= Rebind;
            _localInventoryBindingModel.OnPredictedSlotChanged -= RefreshSelection;
            Unbind();
        }

        private void Rebind() {
            Unbind();
            _inventory = _localInventoryBindingModel.Inventory;
            View.SetBarVisible(_inventory != null);
            if (_inventory == null)
                return;

            _inventory.OnSlotItemNetIdsChanged += RefreshItems;
            _inventory.OnSelectedSlotChanged += RefreshSelection;
            _inventory.OnIsStowedChanged += RefreshSelection;
            RefreshItems();
            RefreshSelection();
        }

        private void Unbind() {
            if (_inventory == null)
                return;

            _inventory.OnSlotItemNetIdsChanged -= RefreshItems;
            _inventory.OnSelectedSlotChanged -= RefreshSelection;
            _inventory.OnIsStowedChanged -= RefreshSelection;
            _inventory = null;
        }

        private void RefreshItems() {
            for (int slot = 0; slot < _inventoryConfiguration.SlotCount; slot++)
                RefreshItem(slot);
        }

        private void RefreshItem(int slot) {
            uint itemNetId = slot < _inventory.SlotItemNetIds.Count ? _inventory.SlotItemNetIds[slot] : 0;
            if (itemNetId != 0 && _inventoryItemDisplaySource.TryGetDisplay(itemNetId, out Sprite icon, out string itemName))
                View.SetSlotItem(slot, icon, itemName);
            else
                View.ClearSlotItem(slot);
        }

        private void RefreshSelection() {
            if (_inventory != null)
                View.SetSelectedSlot(_localInventoryBindingModel.DisplayedSlot, _localInventoryBindingModel.IsDisplayedSlotStowed);
        }
    }
}
