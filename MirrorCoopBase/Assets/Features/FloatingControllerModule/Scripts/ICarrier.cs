using UnityEngine;

namespace Features.FloatingControllerModule {
    // Something a body can stand on that moves and carries what stands on it: a moving platform, later the ship's deck.
    // It sits on the support's object or a parent of it. The poses are the carrier's on this physics step (where it
    // goes in this step) and on the one before, so a rider moves exactly as much as the carrier did.
    public interface ICarrier {
        // Network id of the carrier (0 is the still world).
        uint CarrierId { get; }

        Pose PoseBefore { get; }
        Pose PoseNow { get; }

        // The rotation as drawn this frame (between physics steps), for what follows it every frame: the camera.
        Quaternion DrawnRotation { get; }

        // Whether a point is in the space above the carrier where a body in the air still rides with it (a jump).
        bool Holds(Vector3 worldPoint);
    }
}
