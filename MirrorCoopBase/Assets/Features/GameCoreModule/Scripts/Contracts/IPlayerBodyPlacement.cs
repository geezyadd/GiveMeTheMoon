using UnityEngine;

namespace Features.GameCoreModule.Contracts {
    // Server: moves a player's body on every peer at once (a respawn, a map change, a world shift). The owner's body is
    // moved by the owner, which simulates it; every copy jumps there too and waits for the owner's first state packet
    // sent after the move, so no copy is pulled back to where the player was.
    public interface IPlayerBodyPlacement {
        // Puts the body at the pose, at rest and off any carrier.
        void ServerPlace(Pose pose);

        // Moves the body by the delta, keeping its velocity and rotation (the world moved under it).
        void ServerShift(Vector3 delta);
    }
}
