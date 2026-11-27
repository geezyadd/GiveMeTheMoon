using System;
using System.Collections.Generic;
using Features.GameCoreModule.Contracts;
using Mirror;

namespace Features.GrabModule.Scripts {
    // Applies every hand's held item to the item object on this peer: hold pose, physics and follow.
    // An item that is not spawned here yet stays pending and is picked up as soon as it appears.
    // A holder also carries items (its inventory): a carried item outside the hand is stowed, so a hand switching
    // between carried items never lets one go; it is loose only once it is neither held nor carried.
    public sealed class HeldItemRegistry : IGameplaySession {
        private readonly List<HeldItem> _held = new List<HeldItem>(8);
        private readonly List<HeldItem> _carried = new List<HeldItem>(16);

        // An item left a hand or a carrier on this peer and is loose again (the ship's deck catches one let go in
        // flight). A stow is not a release.
        public event Action<Grabbable> OnItemReleased;

        public void Hold(GrabController holder, uint itemNetId) {
            int index = IndexOf(holder);
            if (index >= 0 && _held[index].ItemNetId == itemNetId)
                return;

            if (index >= 0)
                ReleaseAt(index);

            HeldItem held = new HeldItem(holder, itemNetId);
            _held.Add(held);
            TryAttach(held, true);
        }

        public void Release(GrabController holder) {
            int index = IndexOf(holder);
            if (index >= 0)
                ReleaseAt(index);
        }

        // Everything the holder carries now (0 = an empty slot); items no longer in it drop unless a hand holds them.
        public void SetCarried(GrabController holder, IReadOnlyList<uint> itemNetIds) {
            for (int i = _carried.Count - 1; i >= 0; i--) {
                HeldItem carried = _carried[i];
                if (carried.Holder == holder && Contains(itemNetIds, carried.ItemNetId) == false)
                    StopCarryingAt(i);
            }

            for (int i = 0; i < itemNetIds.Count; i++) {
                uint itemNetId = itemNetIds[i];
                if (itemNetId == 0 || IndexOfCarried(holder, itemNetId) >= 0)
                    continue;

                HeldItem carried = new HeldItem(holder, itemNetId);
                _carried.Add(carried);
                TryAttachCarried(carried, true);
            }
        }

        public void ClearCarried(GrabController holder) {
            for (int i = _carried.Count - 1; i >= 0; i--)
                if (_carried[i].Holder == holder)
                    StopCarryingAt(i);
        }

        public bool TryGetItem(GrabController holder, out Grabbable item) {
            int index = IndexOf(holder);
            item = index >= 0 ? _held[index].Item : null;
            return item != null;
        }

        // Called once per frame after the look is applied, so a held item never lags behind the hand on screen.
        public void FollowHolders() {
            for (int i = 0; i < _held.Count; i++) {
                HeldItem held = _held[i];
                if (held.Item == null && TryAttach(held, false) == false)
                    continue;

                held.Item.FollowHolder();
            }

            for (int i = 0; i < _carried.Count; i++) {
                HeldItem carried = _carried[i];
                if (carried.Item == null && TryAttachCarried(carried, false) == false)
                    continue;

                if (carried.Item.IsStowed)
                    carried.Item.FollowHolder();
            }
        }

        public void CleanupGameplay() {
            for (int i = _carried.Count - 1; i >= 0; i--)
                StopCarryingAt(i);

            for (int i = _held.Count - 1; i >= 0; i--)
                ReleaseAt(i);
        }

        public void RestartGameplay() {
        }

        // On a client two hands can briefly claim one item while their updates arrive: a new claim takes the item,
        // and the older hand waits as pending until its own update releases it.
        private bool TryAttach(HeldItem held, bool isNewClaim) {
            if (TryResolveItem(held.ItemNetId, out Grabbable item) == false)
                return false;

            int previous = IndexOf(item);
            if (previous >= 0 && isNewClaim == false)
                return false;

            if (previous >= 0)
                Detach(_held[previous]);

            held.Item = item;
            item.Stopped += OnItemStopped;
            item.ApplyHold(held.Holder.ArmPoint);
            return true;
        }

        // A carried item claimed by a new carrier moves to it; an older claim waits as pending, as for the hand.
        private bool TryAttachCarried(HeldItem carried, bool isNewClaim) {
            if (TryResolveItem(carried.ItemNetId, out Grabbable item) == false)
                return false;

            int previous = IndexOfCarried(item);
            if (previous >= 0 && isNewClaim == false)
                return false;

            if (previous >= 0)
                DetachCarried(_carried[previous]);

            carried.Item = item;
            item.Stopped += OnCarriedItemStopped;
            if (IndexOf(item) < 0)
                item.ApplyStow(carried.Holder.ArmPoint);

            return true;
        }

        private void ReleaseAt(int index) {
            HeldItem held = _held[index];
            _held.RemoveAt(index);
            if (held.Item == null)
                return;

            held.Item.Stopped -= OnItemStopped;
            LetGo(held.Item);
        }

        private void StopCarryingAt(int index) {
            HeldItem carried = _carried[index];
            _carried.RemoveAt(index);
            if (carried.Item == null)
                return;

            carried.Item.Stopped -= OnCarriedItemStopped;
            if (IndexOf(carried.Item) < 0)
                Release(carried.Item);
        }

        // Out of the hand: a carried item is stowed with its carrier, anything else is loose.
        private void LetGo(Grabbable item) {
            int carried = IndexOfCarried(item);
            if (carried >= 0)
                item.ApplyStow(_carried[carried].Holder.ArmPoint);
            else
                Release(item);
        }

        private void Release(Grabbable item) {
            item.ApplyRelease();
            OnItemReleased?.Invoke(item);
        }

        private void Detach(HeldItem held) {
            held.Item.Stopped -= OnItemStopped;
            held.Item = null;
        }

        private void DetachCarried(HeldItem carried) {
            carried.Item.Stopped -= OnCarriedItemStopped;
            carried.Item = null;
        }

        // The item is gone under the hand (installed, or its scene unloaded): the server empties that hand.
        private void OnItemStopped(Grabbable item) {
            int index = IndexOf(item);
            if (index < 0)
                return;

            HeldItem held = _held[index];
            _held.RemoveAt(index);
            item.Stopped -= OnItemStopped;
            if (held.Holder != null)
                held.Holder.ServerForgetHeld(held.ItemNetId);
        }

        // A stowed item is gone (despawned, its scene unloaded): the server empties its slot.
        private void OnCarriedItemStopped(Grabbable item) {
            int index = IndexOfCarried(item);
            if (index < 0)
                return;

            HeldItem carried = _carried[index];
            _carried.RemoveAt(index);
            item.Stopped -= OnCarriedItemStopped;
            if (carried.Holder != null)
                carried.Holder.ServerForgetCarried(carried.ItemNetId);
        }

        private int IndexOf(GrabController holder) {
            for (int i = 0; i < _held.Count; i++) {
                if (_held[i].Holder == holder)
                    return i;
            }

            return -1;
        }

        private int IndexOf(Grabbable item) {
            for (int i = 0; i < _held.Count; i++) {
                if (_held[i].Item == item)
                    return i;
            }

            return -1;
        }

        private int IndexOfCarried(GrabController holder, uint itemNetId) {
            for (int i = 0; i < _carried.Count; i++) {
                if (_carried[i].Holder == holder && _carried[i].ItemNetId == itemNetId)
                    return i;
            }

            return -1;
        }

        private int IndexOfCarried(Grabbable item) {
            for (int i = 0; i < _carried.Count; i++) {
                if (_carried[i].Item == item)
                    return i;
            }

            return -1;
        }

        private static bool Contains(IReadOnlyList<uint> itemNetIds, uint itemNetId) {
            for (int i = 0; i < itemNetIds.Count; i++) {
                if (itemNetIds[i] == itemNetId)
                    return true;
            }

            return false;
        }

        private static bool TryResolveItem(uint itemNetId, out Grabbable item) {
            item = null;
            if (TryGetSpawned(itemNetId, out NetworkIdentity identity) == false)
                return false;

            item = identity.GetComponent<Grabbable>();
            return item != null;
        }

        private static bool TryGetSpawned(uint netId, out NetworkIdentity identity) {
            identity = null;
            if (NetworkServer.active && NetworkServer.spawned.TryGetValue(netId, out identity) && identity != null)
                return true;

            return NetworkClient.active && NetworkClient.spawned.TryGetValue(netId, out identity) && identity != null;
        }

        private sealed class HeldItem {
            public HeldItem(GrabController holder, uint itemNetId) {
                Holder = holder;
                ItemNetId = itemNetId;
            }

            public GrabController Holder { get; }
            public uint ItemNetId { get; }
            public Grabbable Item { get; set; }
        }
    }
}
