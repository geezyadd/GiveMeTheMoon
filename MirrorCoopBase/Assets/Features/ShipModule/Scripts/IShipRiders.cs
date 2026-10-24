using System.Collections.Generic;
using Features.NetworkModelModule.Scripts;

namespace Features.ShipModule.Scripts {
    internal interface IShipRiders {
        public IReadOnlyList<ShipRider> All { get; }
        public void SetVolumeOverlap(ShipRider rider, bool inside);
        public void UnregisterRider(ShipRider rider);
        public void TrackRider(ShipRider rider);
        public void ServerReleaseRiders();
        public bool IsLocalPlayerRiding();
        public bool TryFindByKey(PlayerKey key, out ShipRider rider);
    }
}
