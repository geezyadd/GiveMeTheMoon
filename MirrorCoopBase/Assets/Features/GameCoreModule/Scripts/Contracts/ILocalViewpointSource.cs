using UnityEngine;

namespace Features.GameCoreModule.Contracts {
    // The point this peer builds the world around (the flight field streams near it), for modules below the camera
    // module.
    public interface ILocalViewpointSource {
        public Vector3 GetPosition();
    }
}
