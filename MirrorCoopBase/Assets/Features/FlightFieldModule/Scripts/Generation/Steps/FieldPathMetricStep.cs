using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Passage;
using Features.FlightFieldModule.Scripts.Generation.Placement;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // "The route matters": L_route is the shortest way with the ship clearance from the start of the first main route
    // to its end, L_free the same way with every corridor cell forbidden. While the free way is too short for the
    // profile (or exists when the profile forbids it), ridges are put on it outside the corridors, a limited number of
    // rounds. Corridor cells are never touched, so the main passage stays.
    public sealed class FieldPathMetricStep : IFieldGenerationStep {
        private const float FIRST_REPAIR_SHARE = 0.25f;
        private const float LAST_REPAIR_SHARE = 0.75f;
        private const int REPAIR_POINTS_PER_ROUND = 3;

        private readonly IFieldClearancePathFinder _fieldClearancePathFinder;
        private readonly IFieldGiantPacker _fieldGiantPacker;

        public FieldPathMetricStep(IFieldClearancePathFinder fieldClearancePathFinder, IFieldGiantPacker fieldGiantPacker) {
            _fieldClearancePathFinder = fieldClearancePathFinder;
            _fieldGiantPacker = fieldGiantPacker;
        }

        public bool IsUsedBy(FieldLayoutKind layout) =>
            layout == FieldLayoutKind.Corridors;

        public void Execute(FieldGenerationContext context) {
            FieldRoute main = FindFirstMain(context.Routes);
            FieldMetrics metrics = context.Metrics;
            int round = 0;
            Measure(context, main);
            while (!metrics.IsPathMetricMet && round < context.Profile.MaxPathRepairRounds) {
                round++;
                CloseFreePath(context, metrics.FreePath);
                Measure(context, main);
            }

            metrics.PathRepairRounds = round;
            metrics.IsStraightBlocked = IsStraightBlocked(context);
        }

        private void Measure(FieldGenerationContext context, FieldRoute main) {
            FieldMetrics metrics = context.Metrics;
            List<Vector2Int> routePath = _fieldClearancePathFinder.Find(context.Cells, main.Start, main.End, false);
            List<Vector2Int> freePath = _fieldClearancePathFinder.Find(context.Cells, main.Start, main.End, true);
            metrics.RoutePath = routePath;
            metrics.FreePath = freePath;
            metrics.RouteLength = routePath.Count > 0 ? routePath.Count - 1 : FieldMetrics.NO_PATH;
            metrics.FreeLength = freePath.Count > 0 ? freePath.Count - 1 : FieldMetrics.NO_PATH;
            metrics.IsPathMetricMet = IsMet(context.Profile, context.Difficulty, metrics);
        }

        private static bool IsMet(FieldDifficultyProfile profile, float difficulty, FieldMetrics metrics) {
            if (!metrics.HasFreePath)
                return true;

            return !profile.RequiresNoFreePath && metrics.FreeToRouteRatio >= profile.MinFreeToRouteRatio.Evaluate(difficulty);
        }

        private void CloseFreePath(FieldGenerationContext context, IReadOnlyList<Vector2Int> freePath) {
            int radius = context.Profile.PathRepairRadiusCells;
            for (int point = 0; point < REPAIR_POINTS_PER_ROUND; point++) {
                float share = Mathf.Lerp(FIRST_REPAIR_SHARE, LAST_REPAIR_SHARE, point / (float)(REPAIR_POINTS_PER_ROUND - 1));
                MarkRidge(context, freePath[Mathf.RoundToInt(share * (freePath.Count - 1))], radius);
            }

            _fieldGiantPacker.Pack(context, FieldCellClass.Ridge, context.Profile.RidgeObstacles);
        }

        private static void MarkRidge(FieldGenerationContext context, Vector2Int center, int radius) {
            for (int y = center.y - radius; y <= center.y + radius; y++) {
                for (int x = center.x - radius; x <= center.x + radius; x++) {
                    if (context.Cells.ValidateIndex(x, y) && context.Cells[x, y].IsFree)
                        context.Cells[x, y] = context.Cells[x, y].WithClass(FieldCellClass.Ridge);
                }
            }
        }

        // The straight line from A to B crosses a placement.
        private static bool IsStraightBlocked(FieldGenerationContext context) {
            Vector2 from = context.StationACell;
            Vector2 to = context.StationBCell;
            int steps = Mathf.CeilToInt(Vector2.Distance(from, to) * 2f);
            for (int i = 0; i <= steps; i++) {
                Vector2Int cell = Vector2Int.RoundToInt(Vector2.Lerp(from, to, i / (float)Mathf.Max(1, steps)));
                if (context.Cells.ValidateIndex(cell) && context.Cells[cell].IsOccupied)
                    return true;
            }

            return false;
        }

        private static FieldRoute FindFirstMain(IReadOnlyList<FieldRoute> routes) {
            foreach (FieldRoute route in routes) {
                if (route.Kind == FieldRouteKind.Main)
                    return route;
            }

            throw new System.InvalidOperationException("A field without a main route.");
        }
    }
}
