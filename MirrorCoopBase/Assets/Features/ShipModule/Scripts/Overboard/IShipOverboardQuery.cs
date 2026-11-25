using Mirror;

namespace Features.ShipModule.Scripts.Overboard {
    // Whether a player drifts overboard now, on any peer.
    public interface IShipOverboardQuery {
        public bool IsOverboard(NetworkIdentity player);
    }
}
