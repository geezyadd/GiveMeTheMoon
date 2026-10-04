using Features.GrabModule.Scripts.Generated;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.GrabModule.Scripts {
    // The player's hand. What it holds is the PlayerHand model (one per player, keyed by the stable player ID);
    // the server is the only one that changes it.
    public sealed class GrabController : PlayerHandBridge, IHeldItemRelease {
        [SerializeField] private Transform _armPoint;

        private HeldItemRegistry _heldItems;
        private InteractionReach _reach;

        public Transform ArmPoint => _armPoint != null ? _armPoint : transform;
        public bool IsHolding => Bound != null && Bound.HeldItemNetId != 0;
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

        public void RequestGrab(Grabbable item) {
            if (item != null && item.CanBeGrabbed && IsHolding == false)
                CmdTryGrab(item);
        }

        public void RequestRelease() {
            if (IsHolding)
                CmdRelease();
        }

        [Server]
        public void ServerReleaseHeld() =>
            ServerSetHeldItemNetId(0);

        [Server]
        public bool ServerGive(Grabbable item) {
            if (item == null || item.CanBeGrabbed == false || IsHolding)
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

        protected override void BindCallbacks() {
            Bound.OnHeldItemNetIdChanged += ApplyHeldItem;
            ApplyHeldItem();
        }

        protected override void ReleaseCallbacks() {
            if (Bound != null)
                Bound.OnHeldItemNetIdChanged -= ApplyHeldItem;

            _heldItems.Release(this);
        }

        [Command]
        private void CmdTryGrab(Grabbable item) {
            if (item == null || item.CanBeGrabbed == false || IsHolding)
                return;

            if (_reach.Contains(transform, item.transform.position) == false)
                return;

            ServerSetHeldItemNetId(item.netId);
        }

        [Command]
        private void CmdRelease() =>
            ServerReleaseHeld();

        private void ApplyHeldItem() {
            uint itemNetId = Bound.HeldItemNetId;
            if (itemNetId == 0)
                _heldItems.Release(this);
            else
                _heldItems.Hold(this, itemNetId);
        }
    }
}
