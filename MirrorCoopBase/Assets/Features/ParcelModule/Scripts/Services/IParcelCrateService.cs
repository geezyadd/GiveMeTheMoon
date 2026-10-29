using Features.ParcelModule.Scripts.Server;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Services {
    // Server: a parcel's crate comes into the world with its synced entry and goes with it.
    public interface IParcelCrateService {
        public ParcelServerRecord ServerSpawn(Vector3 position, Quaternion rotation, long reward, int destinationLoop);
        public void ServerDestroy(uint netId);
    }
}
