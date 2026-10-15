using UnityEngine;

namespace Features.GameCoreModule.Contracts {
    // Where the run's ship is now, for tools that must not depend on the ship module (the flight field debug map);
    // false while no ship is bound to the run on this peer.
    public interface IShipPositionSource {
        public bool TryGetShipPosition(out Vector3 position);
    }
}
