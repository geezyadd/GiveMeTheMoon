using Features.ShipModule.Scripts;

namespace Features.ParcelModule.Scripts.Services {
    // Server: the one delivery stand stands beside the pad the ship stands at.
    public interface IParcelStandSpawnService {
        // The old stand goes, a new one stands beside this pad.
        public void ServerPlaceAt(ShipLandingPad pad);
        public void ServerRemove();
    }
}
