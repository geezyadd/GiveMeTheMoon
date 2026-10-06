using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    public interface IShipStationPads {
        ShipLandingPad CurrentPad { get; }
        NetworkIdentity CurrentIdentity { get; }
        NetworkIdentity PreviousIdentity { get; }
        void Reset(ShipLandingPad startPad);
        bool TrySpawnNext(Vector3 padPosition, Vector3 forward, bool matchLandingPoint, out ShipLandingPad pad);
        void Shift(Vector3 delta);
    }
}
