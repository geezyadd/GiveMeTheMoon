using System.Collections.Generic;

namespace Features.ShipModule.Scripts {
    internal interface IShipRiders {
        public IReadOnlyList<ShipRider> All { get; }
        public void SetVolumeOverlap(ShipRider rider, bool inside);
        public void UnregisterRider(ShipRider rider);
        public void TrackRider(ShipRider rider);
        public void ServerReleaseRiders();
    }
}
