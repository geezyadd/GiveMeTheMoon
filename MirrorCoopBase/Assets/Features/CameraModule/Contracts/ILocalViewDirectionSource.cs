using UnityEngine;

namespace Features.CameraModule.Contracts {
    // Where the local player looks: the game camera's axes, for code below the camera module (a drifting body pushes
    // along the view).
    public interface ILocalViewDirectionSource {
        public Vector3 Forward { get; }
        public Vector3 Right { get; }
    }
}
