using Features.ParcelModule.Scripts.Server;
using Features.ShipModule.Scripts;
using Features.WeaponModule.Contracts;

namespace Features.ParcelModule.Scripts.Services {
    // Server: a drifting parcel comes back aboard, on a harpoon rope or by the ship flying into it.
    public interface IParcelRecoveryService {
        public bool IsHookable(uint netId);
        public void ServerHook(uint netId, in WeaponHit hit, in WeaponPull pull);
        // Moves a pulled parcel toward its rope's holder; boards it there, lets it drift again when the rope breaks.
        public void ServerPullIn(ParcelServerRecord record, ShipBase ship, float deltaTime);
        // Boards a drifting parcel the flying ship has come up to.
        public void ServerCatchFlownInto(ParcelServerRecord record, ShipBase ship);
    }
}
