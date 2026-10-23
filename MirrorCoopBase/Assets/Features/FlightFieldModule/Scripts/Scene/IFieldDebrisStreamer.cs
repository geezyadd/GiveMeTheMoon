using Features.FlightFieldModule.Scripts.Network.Generated;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    public interface IFieldDebrisStreamer {
        // Drops the built debris and reads the debris cells of the live field again.
        public void Restart(IReadOnlyLiveFieldModel liveFieldModel);
        // Builds the debris cells near the viewpoint (field plane, root space) within the frame budget and releases the
        // far ones.
        public void Stream(Transform root, IReadOnlyLiveFieldModel liveFieldModel, Vector2 viewpointOnPlane);
        public void Clear();
    }
}
