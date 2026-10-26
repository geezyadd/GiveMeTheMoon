using Features.NetworkModelModule.Scripts;

namespace Features.ShipModule.Scripts {
    // What a seat's module does with the player sitting in it (a turret: aim and fire; the helm: steer). The socket tells
    // the station on every peer who sits there, by the player's stable key; the station takes the owner's input itself.
    // A seat without a station only seats.
    public interface ISeatStation {
        public void Enter(PlayerKey occupant);

        public void Exit();
    }
}
