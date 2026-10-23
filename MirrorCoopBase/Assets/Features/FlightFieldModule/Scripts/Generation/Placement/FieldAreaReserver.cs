using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    // Marks the cells around a point or along a segment on the field plane as reserved. A cell counts when its center
    // is within the radius plus half a cell, so every cell the area touches is kept empty.
    public sealed class FieldAreaReserver : IFieldAreaReserver {
        public void Reserve(FieldGenerationContext context, Vector3 segmentStart, Vector3 segmentEnd, float radius, FieldReserveKind reserve) {
            Vector2 start = ToPlane(segmentStart);
            Vector2 end = ToPlane(segmentEnd);
            float reach = radius + context.Frame.CellSize / 2f;
            Vector2Int size = context.Frame.Size;
            for (int y = 0; y < size.y; y++) {
                for (int x = 0; x < size.x; x++)
                    ReserveCell(context, new Vector2Int(x, y), start, end, reach, reserve);
            }
        }

        private static void ReserveCell(FieldGenerationContext context, Vector2Int cell, Vector2 start, Vector2 end, float reach, FieldReserveKind reserve) {
            bool isInReach = DistanceToSegment(ToPlane(context.Frame.CellToWorld(cell)), start, end) <= reach;
            if (isInReach && !context.Cells[cell].IsReserved)
                context.Cells[cell] = context.Cells[cell].WithReserve(reserve);
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end) {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            float along = lengthSquared > 0f ? Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared) : 0f;
            return Vector2.Distance(point, start + segment * along);
        }

        private static Vector2 ToPlane(Vector3 point) =>
            new(point.x, point.z);
    }
}
