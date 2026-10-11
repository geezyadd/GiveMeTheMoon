using System.Collections.Generic;
using Features.GameCoreModule.Contracts;
using Mirror;

namespace Features.GrabModule.Scripts {
    // Applies every hand's held item to the item object on this peer: hold pose, physics and follow.
    // An item that is not spawned here yet stays pending and is picked up as soon as it appears.
    public sealed class HeldItemRegistry : IGameplaySession {
        private readonly List<HeldItem> _held = new List<HeldItem>(8);

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
        }

        public void CleanupGameplay() {
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

        private void ReleaseAt(int index) {
            HeldItem held = _held[index];
            _held.RemoveAt(index);
            if (held.Item == null)
                return;

            held.Item.Stopped -= OnItemStopped;
            held.Item.ApplyRelease();
        }

        private void Detach(HeldItem held) {
            held.Item.Stopped -= OnItemStopped;
            held.Item = null;
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
