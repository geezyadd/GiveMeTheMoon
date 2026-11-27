using Features.GrabModule.Scripts.Generated;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.GrabModule.Scripts {
    // The player's hand. What it holds is the PlayerHand model (one per player, keyed by the stable player ID);
    // the server is the only one that changes it. A storable item is picked up through the storage the player's
    // inventory binds here; the hand stays the truth of what is held.
    public sealed class GrabController : PlayerHandBridge, IHeldItemRelease {
        [SerializeField] private Transform _armPoint;

        private HeldItemRegistry _heldItems;
        private InteractionReach _reach;
        private IHeldItemStorage _storage;

        public Transform ArmPoint => _armPoint != null ? _armPoint : transform;
        public bool IsHolding => Bound != null && Bound.HeldItemNetId != 0;
        public bool IsHandFree => IsHolding == false;
        public uint HeldItemNetId => Bound != null ? Bound.HeldItemNetId : 0;
        public Grabbable Held => _heldItems.TryGetItem(this, out Grabbable item) ? item : null;

        [Inject]
        private void Construct(HeldItemRegistry heldItems, InteractionReach reach) {
            _heldItems = heldItems;
            _reach = reach;
        }

        // An item held at disconnect or death drops where the player was; nothing stays in an offline player's hand.
        public override void OnStopServer() {
            if (Bound != null)
                ServerReleaseHeld();

            base.OnStopServer();
        }

        // The player's inventory, on the same prefab, binds itself as the storage of this hand.
        public void UseStorage(IHeldItemStorage storage) =>
            _storage = storage;

        // A storable item goes to the storage when there is one; anything else only into an empty hand.
        public bool CanTake(Grabbable item) =>
            IsStoredItem(item) ? _storage.CanStore(item) : IsHolding == false;

        public void RequestGrab(Grabbable item) {
            if (item != null && item.CanBeGrabbed && CanTake(item))
                CmdTryGrab(item);
        }

        public void RequestRelease() {
            if (IsHolding)
                CmdRelease();
        }

        // The item leaves the hand for the world: its storage slot is emptied first, so it drops instead of being
        // stowed.
        [Server]
        public void ServerReleaseHeld() {
            uint heldItemNetId = HeldItemNetId;
            if (_storage != null && heldItemNetId != 0)
                _storage.ServerForget(heldItemNetId);

            ServerSetHeldItemNetId(0);
        }

        // Death: the hand and everything the player carries drop where the player was.
        [Server]
        public void ServerDropEverything() {
            if (_storage != null)
                _storage.ServerDropAll();

            ServerReleaseHeld();
        }

        [Server]
        public bool ServerGive(Grabbable item) {
            if (item == null || item.CanBeGrabbed == false)
                return false;

            if (IsStoredItem(item))
                return _storage.ServerStore(item);

            if (IsHolding)
                return false;

            ServerSetHeldItemNetId(item.netId);
            return true;
        }

        [Server]
        public void ServerConsumeHeld() {
            Grabbable held = Held;
            if (held == null)
                return;

            ServerReleaseHeld();
            NetworkServer.Destroy(held.gameObject);
        }

        // HeldItemRegistry reports an item destroyed under the hand; a newer item in the hand is left alone.
        internal void ServerForgetHeld(uint itemNetId) {
            if (isServer && Bound != null && Bound.HeldItemNetId == itemNetId)
                ServerReleaseHeld();
        }

        // HeldItemRegistry reports a stowed item destroyed: it leaves its storage slot.
        internal void ServerForgetCarried(uint itemNetId) {
            if (isServer && _storage != null)
                _storage.ServerForget(itemNetId);
        }

        protected override void BindCallbacks() {
            Bound.OnHeldItemNetIdChanged += ApplyChangedHeldItem;
            ApplyHeldItem(false);
        }

        protected override void ReleaseCallbacks() {
            if (Bound != null)
                Bound.OnHeldItemNetIdChanged -= ApplyChangedHeldItem;

            _heldItems.Release(this);
        }

        [Command]
        private void CmdTryGrab(Grabbable item) {
            if (item == null || item.CanBeGrabbed == false)
                return;

            if (_reach.Contains(transform, item.transform.position) == false)
                return;

            ServerGive(item);
        }

        [Command]
        private void CmdRelease() =>
            ServerReleaseHeld();

        private bool IsStoredItem(Grabbable item) =>
            _storage != null && item.CanStoreInInventory;

        private void ApplyChangedHeldItem() =>
            ApplyHeldItem(true);

        private void ApplyHeldItem(bool playsEquipSound) {
            uint itemNetId = Bound.HeldItemNetId;
            if (itemNetId == 0)
                _heldItems.Release(this);
            else
                _heldItems.Hold(this, itemNetId, playsEquipSound);
        }
    }
}
