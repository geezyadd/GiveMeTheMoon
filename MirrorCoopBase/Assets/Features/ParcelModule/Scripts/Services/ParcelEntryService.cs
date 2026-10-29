using System.Collections.Generic;
using Features.ParcelModule.Data;
using Features.ParcelModule.Data.Generated;

namespace Features.ParcelModule.Scripts.Services {
    public sealed class ParcelEntryService : IParcelEntryService {
        private readonly IReadOnlyParcelsModel _parcelsModel;
        private readonly ParcelsBindingModel _parcelsBindingModel;

        public ParcelEntryService(IReadOnlyParcelsModel parcelsModel, ParcelsBindingModel parcelsBindingModel) {
            _parcelsModel = parcelsModel;
            _parcelsBindingModel = parcelsBindingModel;
        }

        public bool TryFind(uint netId, out ParcelEntry entry) {
            entry = default;
            if (TryFindIndex(netId, out int index) == false)
                return false;

            entry = _parcelsModel.Entries[index];
            return true;
        }

        public void ServerAdd(ParcelEntry entry) =>
            _parcelsBindingModel.Writer.ServerAddEntries(entry);

        public void ServerSetState(uint netId, ParcelState state, int carrierAgentId, string hookerId, string hookWeaponId) {
            if (TryFindIndex(netId, out int index))
                _parcelsBindingModel.Writer.ServerSetEntries(index, _parcelsModel.Entries[index].With(state, carrierAgentId, hookerId, hookWeaponId));
        }

        public void ServerRemove(uint netId) {
            if (TryFindIndex(netId, out int index))
                _parcelsBindingModel.Writer.ServerRemoveEntriesAt(index);
        }

        private bool TryFindIndex(uint netId, out int index) {
            IReadOnlyList<ParcelEntry> entries = _parcelsModel.Entries;
            for (index = 0; index < entries.Count; index++) {
                if (entries[index].NetId == netId)
                    return true;
            }

            index = -1;
            return false;
        }
    }
}
