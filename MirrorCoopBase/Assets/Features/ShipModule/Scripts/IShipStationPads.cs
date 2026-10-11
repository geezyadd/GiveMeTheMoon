using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    public interface IShipStationPads {
        public ShipLandingPad CurrentPad { get; }
        public NetworkIdentity CurrentIdentity { get; }
        public NetworkIdentity PreviousIdentity { get; }
        public void Reset(ShipLandingPad startPad);
        public bool TrySpawnNext(Vector3 padPosition, Vector3 forward, bool matchLandingPoint, out ShipLandingPad pad);
        public void Shift(Vector3 delta);
    }
}
