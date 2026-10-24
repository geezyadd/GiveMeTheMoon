using UnityEngine;

namespace Features.SonarModule.Scripts.Core {
    public interface ISonarMarkerProjection {
        // The point is the camera's WorldToScreenPoint of the object: z below 0 lies behind the camera.
        public SonarMarkerPlacement Place(Vector3 screenPoint, Vector2 screenSize);
    }
}
