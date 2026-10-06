using UnityEngine;

namespace Features.ShipModule.Scripts {
    public interface IShipDeckGeometry {
        public Bounds Bounds { get; }
        public bool ContainsDeckWalk(Vector3 localOffset, float inset);
        public void ClampDeckWalk(ref Vector3 localOffset, float inset);
        public bool TryGetDeckSurfaceY(Vector3 localOffset, out float surfaceY);
        public bool TryGetDeckStandPoint(out Vector3 shipLocal);
        public bool TryClosestDeckWalk(Vector3 localOffset, out Vector3 closestLocal, out float dx, out float dz);
    }
}
