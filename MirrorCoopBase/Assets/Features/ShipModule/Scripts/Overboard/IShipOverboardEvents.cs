using System;
using Mirror;

namespace Features.ShipModule.Scripts.Overboard {
    // Server: a drifting player went past the limit; the player's life decides what that does (a normal death).
    public interface IShipOverboardEvents {
        public event Action<NetworkIdentity> OnDriftLimitReached;
    }
}
