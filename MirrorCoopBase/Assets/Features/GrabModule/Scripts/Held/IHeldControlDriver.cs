using Mirror;
using UnityEngine;

namespace Features.GrabModule.Scripts.Held {
    // The holder's peer side of a held control: the mouse moves it while the interact button is held.
    public interface IHeldControlDriver {
        public void LocalBeginHold(NetworkIdentity user);

        // Moves the control by this frame's look input; says whether to send a position and whether the hold is lost.
        public HeldControlFrame LocalDrive(Vector2 look, float deltaTime);

        // The hold ends here; the frame carries the last position when it was not sent yet.
        public HeldControlFrame LocalEndHold();
    }
}
