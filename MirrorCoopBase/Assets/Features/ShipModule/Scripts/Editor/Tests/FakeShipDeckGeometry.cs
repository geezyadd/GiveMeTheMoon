using System.Collections.Generic;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    // A deck made of flat ship-local rectangles, each with its own surface height (a raised spot is one more rectangle).
    public sealed class FakeShipDeckGeometry : IShipDeckGeometry {
        private readonly List<Rect> _areas = new List<Rect>();
        private readonly List<float> _surfaceHeights = new List<float>();

        public Bounds Bounds => new Bounds(Vector3.zero, Vector3.one);

        public FakeShipDeckGeometry AddArea(float minX, float maxX, float minZ, float maxZ, float surfaceY) {
            _areas.Add(Rect.MinMaxRect(minX, minZ, maxX, maxZ));
            _surfaceHeights.Add(surfaceY);
            return this;
        }

        public bool ContainsDeckWalk(Vector3 localOffset, float inset) {
            for (int i = 0; i < _areas.Count; i++) {
                Rect area = _areas[i];
                bool insideX = localOffset.x >= area.xMin + inset && localOffset.x <= area.xMax - inset;
                bool insideZ = localOffset.z >= area.yMin + inset && localOffset.z <= area.yMax - inset;
                if (insideX && insideZ)
                    return true;
            }

            return false;
        }

        public void ClampDeckWalk(ref Vector3 localOffset, float inset) {
            if (TryClosestDeckWalk(localOffset, out Vector3 closest, out _, out _) == false)
                return;

            localOffset.x = closest.x;
            localOffset.z = closest.z;
        }

        public bool TryGetDeckSurfaceY(Vector3 localOffset, out float surfaceY) {
            surfaceY = localOffset.y;
            int closest = ClosestArea(localOffset, out _);
            if (closest < 0)
                return false;

            surfaceY = _surfaceHeights[closest];
            return true;
        }

        public bool TryGetDeckStandPoint(out Vector3 shipLocal) {
            shipLocal = Vector3.zero;
            return false;
        }

        public bool TryClosestDeckWalk(Vector3 localOffset, out Vector3 closestLocal, out float dx, out float dz) {
            closestLocal = localOffset;
            dx = 0f;
            dz = 0f;
            if (ClosestArea(localOffset, out Vector2 closest) < 0)
                return false;

            closestLocal = new Vector3(closest.x, localOffset.y, closest.y);
            dx = closest.x - localOffset.x;
            dz = closest.y - localOffset.z;
            return true;
        }

        private int ClosestArea(Vector3 localOffset, out Vector2 closest) {
            closest = Vector2.zero;
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _areas.Count; i++) {
                Rect area = _areas[i];
                Vector2 point = new Vector2(
                    Mathf.Clamp(localOffset.x, area.xMin, area.xMax),
                    Mathf.Clamp(localOffset.z, area.yMin, area.yMax));
                float distance = (point - new Vector2(localOffset.x, localOffset.z)).sqrMagnitude;
                // Where areas overlap, the highest surface is the one a rider stands on.
                bool isHigherOnTie = best >= 0 && Mathf.Approximately(distance, bestDistance) && _surfaceHeights[i] > _surfaceHeights[best];
                if (distance >= bestDistance && isHigherOnTie == false)
                    continue;

                bestDistance = distance;
                best = i;
                closest = point;
            }

            return best;
        }
    }
}
