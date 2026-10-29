using Features.ParcelModule.Data;

namespace Features.ParcelModule.Scripts.Services {
    // The parcels in the synced model by their crate's net id; writes only on the server.
    public interface IParcelEntryService {
        public bool TryFind(uint netId, out ParcelEntry entry);
        public void ServerAdd(ParcelEntry entry);
        public void ServerSetState(uint netId, ParcelState state, int carrierAgentId, string hookerId, string hookWeaponId);
        public void ServerRemove(uint netId);
    }
}
