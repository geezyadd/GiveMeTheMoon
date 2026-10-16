using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    // Lays a route centerline: random waypoints moved sideways from the straight line, joined by path finding on the
    // field matrix. Every cell stays far enough from the border for the whole corridor and the wall.
    // The legs use side moves only: with a diagonal step the side cell of the corner may lack the ship clearance even
    // when the whole corridor is free, and then clearing the corridor would not give a passage.
    public sealed class FieldCenterlinePlanner : IFieldCenterlinePlanner {
        private const int WALL_CELLS = 1;

        private readonly FlightFieldConfiguration _flightFieldConfiguration;

        public FieldCenterlinePlanner(FlightFieldConfiguration flightFieldConfiguration) =>
            _flightFieldConfiguration = flightFieldConfiguration;

        public List<Vector2Int> Plan(FieldGenerationContext context, FieldCorridorShape shape) {
            int inset = shape.HalfWidthCells + WALL_CELLS;
            Vector2Int size = context.Frame.Size;
            List<Vector2Int> waypoints = CreateWaypoints(context, ClampInside(context.CruiseStartCell, inset, size), ClampInside(context.StationBCell, inset, size),
                shape.MaxSideOffsetCells, inset);
            List<Vector2Int> centerline = new() { waypoints[0] };
            for (int i = 1; i < waypoints.Count; i++)
                AppendLeg(context, waypoints[i - 1], waypoints[i], centerline);

            return centerline;
        }

        private List<Vector2Int> CreateWaypoints(FieldGenerationContext context, Vector2Int start, Vector2Int end, int maxSideOffset, int inset) {
            Vector2Int delta = end - start;
            int length = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
            int legCount = Mathf.Max(1, Mathf.CeilToInt(length / (float)_flightFieldConfiguration.WaypointSpacingCells));
            Vector2Int sideways = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? Vector2Int.up : Vector2Int.right;
            List<Vector2Int> waypoints = new() { start };
            for (int leg = 1; leg < legCount; leg++) {
                Vector2 onLine = Vector2.Lerp(start, end, leg / (float)legCount);
                int offset = context.Random.Range(-maxSideOffset, maxSideOffset + 1);
                waypoints.Add(ClampInside(Vector2Int.RoundToInt(onLine) + sideways * offset, inset, context.Frame.Size));
            }

            waypoints.Add(end);
            return waypoints;
        }

        private static void AppendLeg(FieldGenerationContext context, Vector2Int from, Vector2Int to, List<Vector2Int> centerline) {
            List<Vector2Int> leg = context.Cells.PathFinding(from, to);
            for (int i = 1; i < leg.Count; i++)
                centerline.Add(leg[i]);
        }

        private static Vector2Int ClampInside(Vector2Int cell, int inset, Vector2Int size) =>
            new(Mathf.Clamp(cell.x, inset, size.x - 1 - inset), Mathf.Clamp(cell.y, inset, size.y - 1 - inset));
    }
}
