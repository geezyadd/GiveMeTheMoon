using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // The point this peer builds the field around.
    public interface IFieldStreamingViewpoint {
        public Vector3 GetPosition();
    }
}
