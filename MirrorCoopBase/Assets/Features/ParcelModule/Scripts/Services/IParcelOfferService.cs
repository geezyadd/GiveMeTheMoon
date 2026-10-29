using UnityEngine;

namespace Features.ParcelModule.Scripts.Services {
    // Server: the delivery stand's offers at the station the ship stands at.
    public interface IParcelOfferService {
        // 0..2 new offers for the next station, the old ones gone.
        public void ServerCreateOffers(int stationLoop);
        public void ServerClearOffers();
        // The crew takes the offer of this slot for free: its crate appears at the point.
        public ParcelTakeResult ServerTryTakeOffer(int slot, Vector3 cratePosition, Quaternion crateRotation);
    }
}
