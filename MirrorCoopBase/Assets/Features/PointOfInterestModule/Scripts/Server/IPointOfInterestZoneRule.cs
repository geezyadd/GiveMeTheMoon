using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Server {
    public interface IPointOfInterestZoneRule {
        // The ship's middle is inside the point's zone sphere.
        public bool IsInZone(Vector3 point, Vector3 shipPosition);
        // The hull reaches the reward container of the given radius.
        public bool IsRamming(Vector3 point, Vector3 shipPosition, float containerRadius);
        // A reported weapon hit lies on the container of the given radius (with the hit tolerance).
        public bool IsOnContainer(Vector3 point, Vector3 hitPoint, float containerRadius);
    }
}
