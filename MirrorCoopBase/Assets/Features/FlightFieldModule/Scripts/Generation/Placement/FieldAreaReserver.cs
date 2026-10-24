using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    // Marks the cells around a point or along a segment on the field plane as reserved. A cell counts when its center
    // is within the radius plus half a cell, so every cell the area touches is kept empty.
    public sealed class FieldAreaReserver : IFieldAreaReserver {
        public void Reserve(FieldGenerationContext context, Vector3 segmentStart, Vector3 segmentEnd, float radius, FieldReserveKind reserve) {
            Vector2 start = FieldPlaneGeometry.ToGround(segmentStart);
            Vector2 end = FieldPlaneGeometry.ToGround(segmentEnd);
            float reach = radius + context.Frame.CellSize / 2f;
            Vector2Int size = context.Frame.Size;
            for (int y = 0; y < size.y; y++) {
                for (int x = 0; x < size.x; x++)
                    ReserveCell(context, new Vector2Int(x, y), start, end, reach, reserve);
            }
        }

        private static void ReserveCell(FieldGenerationContext context, Vector2Int cell, Vector2 start, Vector2 end, float reach, FieldReserveKind reserve) {
            bool isInReach = FieldPlaneGeometry.DistanceToSegment(FieldPlaneGeometry.ToGround(context.Frame.CellToWorld(cell)), start, end) <= reach;
            if (isInReach && !context.Cells[cell].IsReserved)
                context.Cells[cell] = context.Cells[cell].WithReserve(reserve);
        }
    }
}
