using Features.FieldAgentModule.Scripts.Cargo;
using Features.ParcelModule.Scripts.Services;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Server {
    // Server: one parcel as the bugs see it, a cargo on board they can grab and carry off. What a grab, the carry and
    // the let-go do is the parcel's theft service.
    public sealed class ParcelCargoTarget : IFieldAgentCargoTarget {
        private readonly uint _netId;
        private readonly IParcelTheftService _parcelTheftService;

        public bool IsTaken => _parcelTheftService.IsFreeOnBoard(_netId) == false;
        public Vector3 ShipLocalPosition => _parcelTheftService.GetShipLocalPosition(_netId);

        public ParcelCargoTarget(uint netId, IParcelTheftService parcelTheftService) {
            _netId = netId;
            _parcelTheftService = parcelTheftService;
        }

        public void ServerTake(int carrierAgentId) =>
            _parcelTheftService.ServerTake(_netId, carrierAgentId);

        public void ServerRelease(Vector3 worldPosition) =>
            _parcelTheftService.ServerDrop(_netId, worldPosition);

        public void FollowCarrier(Vector3 carrierWorldPosition) =>
            _parcelTheftService.ServerFollowCarrier(_netId, carrierWorldPosition);
    }
}
