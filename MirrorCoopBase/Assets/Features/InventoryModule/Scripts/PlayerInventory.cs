using System.Collections.Generic;
using Features.GrabModule.Scripts;
using Features.InventoryModule.Scripts.Generated;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.InventoryModule.Scripts {
    // A player's inventory slots (the PlayerInventory model, one per player) and the storage of the player's hand.
    // The hand holds the selected slot's item; every other carried item is stowed by HeldItemRegistry on every peer,
    // from this model. Only the server changes the slots: a pickup, a slot switch, a throw. A rejoining player starts
    // empty: everything carried drops on death or on leaving.
    public sealed class PlayerInventory : PlayerInventoryBridge, IHeldItemStorage {
        private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

        [SerializeField] private GrabController _grab;
        [Tooltip("The player's body: a throw adds its velocity.")]
        [SerializeField] private Rigidbody _body;

        private readonly List<uint> _droppedItemNetIds = new List<uint>(4);
        private HeldItemRegistry _heldItemRegistry;
        private InventoryConfiguration _inventoryConfiguration;
        private LocalInventoryBindingModel _localInventoryBindingModel;

        public bool CanSwitchSlots => Bound != null && IsHoldingForeignItem() == false;

        private InventorySlots Slots =>
            new InventorySlots(Bound.SlotItemNetIds, _inventoryConfiguration.SlotCount, new InventorySelection(Bound.SelectedSlot, Bound.IsStowed));

        [Inject]
        private void InjectDependencies(
            HeldItemRegistry heldItemRegistry,
            InventoryConfiguration inventoryConfiguration,
            LocalInventoryBindingModel localInventoryBindingModel) {
            _heldItemRegistry = heldItemRegistry;
            _inventoryConfiguration = inventoryConfiguration;
            _localInventoryBindingModel = localInventoryBindingModel;
        }

        private void Awake() =>
            _grab.UseStorage(this);

        public override void OnStartServer() {
            base.OnStartServer();
            ServerResetSlots();
        }

        // Applied only after the full state is in: a rejoining player's entry still holds the ids of what it dropped.
        public override void OnStartClient() {
            base.OnStartClient();
            if (Bound != null)
                ApplyCarried();
        }

        public override void OnStopServer() {
            if (Bound != null)
                ServerDropAll();

            base.OnStopServer();
        }

        public override void OnStartLocalPlayer() =>
            _localInventoryBindingModel.Bind(Bound);

        public override void OnStopLocalPlayer() =>
            _localInventoryBindingModel.Clear();

        public bool CanStore(Grabbable item) =>
            Bound != null && IsHoldingForeignItem() == false && Slots.FindPickupSlot() != InventorySlots.NO_SLOT;

        // A slot key: the highlight moves at once on this peer, the server confirms or refuses it.
        public void RequestSelect(int slot) {
            if (CanSwitchSlots == false || Slots.IsValidSlot(slot) == false)
                return;

            if (slot != _localInventoryBindingModel.DisplayedSlot)
                _localInventoryBindingModel.PredictSlot(slot);

            CmdSelectSlot(slot);
        }

        public void RequestCycle(int steps) {
            if (CanSwitchSlots == false || Slots.Count <= 1)
                return;

            int slot = Slots.Cycle(_localInventoryBindingModel.DisplayedSlot, steps);
            if (slot != _localInventoryBindingModel.DisplayedSlot)
                RequestSelect(slot);
        }

        public void RequestThrow(Vector3 viewDirection) {
            if (_grab.IsHolding)
                CmdThrow(viewDirection);
        }

        [Server]
        public bool ServerStore(Grabbable item) {
            if (CanStore(item) == false)
                return false;

            int slot = Slots.FindPickupSlot();
            ServerSetSlotItemNetIds(slot, item.netId);
            ServerSetSelectedSlot(slot);
            ServerSetIsStowed(false);
            _grab.ServerSetHeldItemNetId(item.netId);
            return true;
        }

        [Server]
        public void ServerForget(uint itemNetId) {
            if (Bound == null)
                return;

            int slot = Slots.IndexOf(itemNetId);
            if (slot == InventorySlots.NO_SLOT)
                return;

            ServerSetSlotItemNetIds(slot, 0);
            if (slot == Bound.SelectedSlot)
                ServerSetIsStowed(false);
        }

        // Every carried item drops around where the player was, scattered so they do not land in one point.
        [Server]
        public void ServerDropAll() {
            if (Bound == null)
                return;

            _droppedItemNetIds.Clear();
            for (int i = 0; i < Bound.SlotItemNetIds.Count; i++)
                if (Bound.SlotItemNetIds[i] != 0)
                    _droppedItemNetIds.Add(Bound.SlotItemNetIds[i]);

            for (int i = 0; i < _droppedItemNetIds.Count; i++)
                ServerDrop(_droppedItemNetIds[i]);

            ServerSetIsStowed(false);
        }

        protected override void BindCallbacks() {
            base.BindCallbacks();
            Bound.OnSlotItemNetIdsChanged += ApplyCarried;
            Bound.OnSelectedSlotChanged += OnSelectedSlotChanged;
        }

        protected override void ReleaseCallbacks() {
            base.ReleaseCallbacks();
            if (Bound != null) {
                Bound.OnSlotItemNetIdsChanged -= ApplyCarried;
                Bound.OnSelectedSlotChanged -= OnSelectedSlotChanged;
            }

            _heldItemRegistry.ClearCarried(_grab);
        }

        // A hand holding an item that is in no slot (a ship module) keeps it: no switching, no storing.
        private bool IsHoldingForeignItem() =>
            _grab.HeldItemNetId != 0 && Slots.IndexOf(_grab.HeldItemNetId) == InventorySlots.NO_SLOT;

        // A rejoining player's entry is reused: it starts empty, nothing carried is saved.
        private void ServerResetSlots() {
            ServerClearSlotItemNetIds();
            for (int i = 0; i < _inventoryConfiguration.SlotCount; i++)
                ServerAddSlotItemNetIds(0);

            ServerSetSelectedSlot(0);
            ServerSetIsStowed(false);
        }

        private void ServerApplySelection(InventorySelection selection) {
            uint handItemNetId = Slots.HandItemNetIdFor(selection);
            ServerSetSelectedSlot(selection.Slot);
            ServerSetIsStowed(selection.IsStowed);
            _grab.ServerSetHeldItemNetId(handItemNetId);
        }

        private void ServerDrop(uint itemNetId) {
            if (itemNetId == _grab.HeldItemNetId)
                _grab.ServerReleaseHeld();
            else
                ServerForget(itemNetId);

            if (NetworkServer.spawned.TryGetValue(itemNetId, out NetworkIdentity identity)
                && identity != null
                && identity.TryGetComponent(out Grabbable item))
                item.ServerMoveTo(ScatteredDropPoint());
        }

        private Vector3 ScatteredDropPoint() {
            Vector2 offset = Random.insideUnitCircle * _inventoryConfiguration.DropScatterRadius;
            return _grab.ArmPoint.position + new Vector3(offset.x, 0f, offset.y);
        }

        private Vector3 ThrowDirection(Vector3 viewDirection) =>
            viewDirection.sqrMagnitude < MIN_DIRECTION_SQR_MAGNITUDE ? transform.forward : viewDirection.normalized;

        private void ApplyCarried() =>
            _heldItemRegistry.SetCarried(_grab, Bound.SlotItemNetIds);

        private void OnSelectedSlotChanged() {
            if (isLocalPlayer)
                _localInventoryBindingModel.EndPrediction(Bound.SelectedSlot);
        }

        [Command]
        private void CmdSelectSlot(int slot) {
            if (CanSwitchSlots == false || Slots.IsValidSlot(slot) == false) {
                TargetRefuseSelection(slot);
                return;
            }

            ServerApplySelection(Slots.Select(slot));
        }

        // Thrown along the view: the client sends where it looks, the server gives the item its velocity.
        [Command]
        private void CmdThrow(Vector3 viewDirection) {
            Grabbable item = _grab.Held;
            if (item == null)
                return;

            Vector3 velocity = ThrowDirection(viewDirection) * _inventoryConfiguration.ThrowSpeed + _body.linearVelocity;
            _grab.ServerReleaseHeld();
            item.ServerLaunch(velocity);
        }

        [TargetRpc]
        private void TargetRefuseSelection(int slot) =>
            _localInventoryBindingModel.EndPrediction(slot);
    }
}
