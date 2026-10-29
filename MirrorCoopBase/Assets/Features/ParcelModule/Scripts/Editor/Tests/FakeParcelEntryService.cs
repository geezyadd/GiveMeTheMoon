using System.Collections.Generic;
using Features.ParcelModule.Data;
using Features.ParcelModule.Scripts.Services;

namespace Features.ParcelModule.Scripts.Editor.Tests {
    // The synced parcel entries without a network bridge.
    public sealed class FakeParcelEntryService : IParcelEntryService {
        public List<ParcelEntry> Entries { get; } = new();

        public bool TryFind(uint netId, out ParcelEntry entry) {
            entry = Entries.Find(candidate => candidate.NetId == netId);
            return Entries.Exists(candidate => candidate.NetId == netId);
        }

        public void ServerAdd(ParcelEntry entry) =>
            Entries.Add(entry);

        public void ServerSetState(uint netId, ParcelState state, int carrierAgentId, string hookerId, string hookWeaponId) {
            int index = Entries.FindIndex(candidate => candidate.NetId == netId);
            Entries[index] = Entries[index].With(state, carrierAgentId, hookerId, hookWeaponId);
        }

        public void ServerRemove(uint netId) =>
            Entries.RemoveAll(candidate => candidate.NetId == netId);
    }
}
