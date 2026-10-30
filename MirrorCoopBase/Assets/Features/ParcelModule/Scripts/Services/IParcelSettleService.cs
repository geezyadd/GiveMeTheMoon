using Features.ShipModule.Scripts;

namespace Features.ParcelModule.Scripts.Services {
    // Server: what becomes of the crew's parcels when the ship arrives somewhere or is gone.
    public interface IParcelSettleService {
        // The ship stands at the station of this loop: a parcel for it on board pays once and is handed over, one for
        // it not on board is lost, one for a later station waits.
        public void ServerSettleAtStation(int stationLoop, ShipBase ship);
        // The ship crashed, was destroyed or the run ended: every parcel is lost, without a fine.
        public void ServerLoseAll();
    }
}
