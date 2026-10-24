using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // The main route of an obstacle field: the straight line from the cruise start to station B, one cell per step
    // (diagonal steps too). It reserves no cell: nothing in the field marks it; the stations' volume clearance, the
    // dodge metric and the debug map read it.
    public sealed class FieldObstacleRouteStep : IFieldGenerationStep {
        private const int MAIN_ROUTE_ID = 0;
        private const int NO_CORRIDOR_HALF_WIDTH = 0;

        public void Execute(FieldGenerationContext context) {
            List<Vector2Int> centerline = CreateStraightLine(ClampInside(context.CruiseStartCell, context.Frame.Size), context.StationBCell);
            List<int> halfWidths = new(centerline.Count);
            for (int i = 0; i < centerline.Count; i++)
                halfWidths.Add(NO_CORRIDOR_HALF_WIDTH);

            context.AddRoute(new FieldRoute(MAIN_ROUTE_ID, FieldRouteKind.Main, context.Difficulty, centerline, halfWidths));
        }

        private static List<Vector2Int> CreateStraightLine(Vector2Int start, Vector2Int end) {
            Vector2Int delta = end - start;
            int steps = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
            List<Vector2Int> line = new(steps + 1) { start };
            for (int i = 1; i <= steps; i++)
                line.Add(Vector2Int.RoundToInt(Vector2.Lerp(start, end, i / (float)steps)));

            return line;
        }

        private static Vector2Int ClampInside(Vector2Int cell, Vector2Int size) =>
            new(Mathf.Clamp(cell.x, 0, size.x - 1), Mathf.Clamp(cell.y, 0, size.y - 1));
    }
}
