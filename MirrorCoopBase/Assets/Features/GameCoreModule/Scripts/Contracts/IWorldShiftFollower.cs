using UnityEngine;

namespace Features.GameCoreModule.Contracts {
    // World content that has to move with a floating-origin shift. Called once per shift on every peer: on the server
    // (and host) by the shift itself, on a remote client in the frame its copies of the station pads move.
    public interface IWorldShiftFollower {
        public void FollowWorldShift(Vector3 delta);
    }
}
