using UnityEngine;

namespace Features.ParcelModule.Scripts.Services {
    // Server: a parcel as the bugs' cargo: free on board, grabbed, carried under its thief and let go when the thief
    // is shot down or gone.
    public interface IParcelTheftService {
        public bool IsFreeOnBoard(uint netId);
        public Vector3 GetShipLocalPosition(uint netId);
        public void ServerTake(uint netId, int carrierAgentId);
        public void ServerFollowCarrier(uint netId, Vector3 carrierWorldPosition);
        public void ServerDrop(uint netId, Vector3 carrierWorldPosition);
    }
}
