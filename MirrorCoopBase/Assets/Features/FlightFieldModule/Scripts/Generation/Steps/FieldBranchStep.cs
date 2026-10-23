using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Routes;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Branches of every main route: alternatives that leave it and rejoin it (narrower, more debris, on high
    // difficulty sometimes closed by a ridge in the middle) and dead ends that end in a ridge. Every branch spans its
    // own share of the main centerline, so branches do not pile up, and none starts within the meander fade of a
    // station. The branch corridors are reserved like the main one.
    public sealed class FieldBranchStep : IFieldGenerationStep {
        // An alternative keeps at least this many cells between its corridor and the main one at its widest swing.
        private const int ALTERNATIVE_SEPARATION_CELLS = 2;
        private const int MIN_BRANCH_CELLS = 4;

        private readonly IFieldBranchPlanner _fieldBranchPlanner;
        private readonly IFieldCorridorMapper _fieldCorridorMapper;
        private readonly FlightFieldConfiguration _flightFieldConfiguration;

        public FieldBranchStep(IFieldBranchPlanner fieldBranchPlanner, IFieldCorridorMapper fieldCorridorMapper, FlightFieldConfiguration flightFieldConfiguration) {
            _fieldBranchPlanner = fieldBranchPlanner;
            _fieldCorridorMapper = fieldCorridorMapper;
            _flightFieldConfiguration = flightFieldConfiguration;
        }

        public bool IsUsedBy(FieldLayoutKind layout) =>
            layout == FieldLayoutKind.Corridors;

        public void Execute(FieldGenerationContext context) {
            List<FieldRoute> mainRoutes = new();
            foreach (FieldRoute route in context.Routes) {
                if (route.Kind == FieldRouteKind.Main)
                    mainRoutes.Add(route);
            }

            foreach (FieldRoute main in mainRoutes) {
                AddAlternatives(context, main);
                AddDeadEnds(context, main);
            }
        }

        private void AddAlternatives(FieldGenerationContext context, FieldRoute main) {
            FieldDifficultyProfile profile = context.Profile;
            float difficulty = main.Difficulty;
            int count = Mathf.RoundToInt(profile.AlternativeRoutes.Evaluate(difficulty));
            int halfWidth = HalfWidth(profile.AlternativeHalfWidthCells.Evaluate(difficulty));
            float offset = Mathf.Max(profile.AlternativeOffsetCells.Evaluate(difficulty), MaxHalfWidth(main) + halfWidth + ALTERNATIVE_SEPARATION_CELLS);
            int inset = halfWidth + profile.WallThicknessCells;
            for (int i = 0; i < count; i++) {
                if (!TryPickSpan(context, main, i, count, profile.AlternativeLengthCells, out Vector2Int span))
                    continue;

                List<Vector2Int> centerline = _fieldBranchPlanner.PlanAlternative(context, main.Centerline, span, Side(i), offset, inset);
                bool isClosed = context.Random.Value() < profile.AlternativeClosedChance.Evaluate(difficulty);
                AddBranch(context, centerline, halfWidth, FieldRouteKind.Alternative, FieldReserveKind.AlternativeCorridor, difficulty, isClosed);
                if (isClosed)
                    CloseMiddle(context, centerline, halfWidth);
            }
        }

        private void AddDeadEnds(FieldGenerationContext context, FieldRoute main) {
            FieldDifficultyProfile profile = context.Profile;
            float difficulty = main.Difficulty;
            int count = Mathf.RoundToInt(profile.DeadEnds.Evaluate(difficulty));
            int halfWidth = HalfWidth(profile.DeadEndHalfWidthCells.Evaluate(difficulty));
            int inset = halfWidth + profile.WallThicknessCells;
            for (int i = 0; i < count; i++) {
                if (!TryPickSpan(context, main, i, count, Vector2.one * MIN_BRANCH_CELLS, out Vector2Int span))
                    continue;

                float length = Mathf.Lerp(profile.DeadEndLengthCells.x, profile.DeadEndLengthCells.y, context.Random.Value());
                List<Vector2Int> centerline = _fieldBranchPlanner.PlanDeadEnd(context, main.Centerline, span.x, -Side(i), length, inset);
                AddBranch(context, centerline, halfWidth, FieldRouteKind.DeadEnd, FieldReserveKind.DeadEndCorridor, difficulty, false);
                CapTip(context, centerline[centerline.Count - 1], halfWidth + profile.DeadEndCapCells);
            }
        }

        // The main centerline is cut into one share per branch; the branch takes a random stretch of its share.
        private static bool TryPickSpan(FieldGenerationContext context, FieldRoute main, int index, int count, Vector2 lengthRange, out Vector2Int span) {
            span = default;
            int margin = Mathf.CeilToInt(context.Profile.MeanderFadeCells);
            int share = (main.Centerline.Count - 2 * margin) / Mathf.Max(1, count);
            int length = Mathf.Min(Mathf.RoundToInt(Mathf.Lerp(lengthRange.x, lengthRange.y, context.Random.Value())), share - 1);
            if (length < MIN_BRANCH_CELLS)
                return false;

            int from = margin + index * share + context.Random.Range(0, share - length);
            span = new Vector2Int(from, from + length);
            return true;
        }

        private void AddBranch(FieldGenerationContext context, List<Vector2Int> centerline, int halfWidth, FieldRouteKind kind, FieldReserveKind reserve, float difficulty,
                               bool isClosed) {
            List<int> halfWidths = new(centerline.Count);
            for (int i = 0; i < centerline.Count; i++)
                halfWidths.Add(halfWidth);

            foreach (Vector2Int cell in _fieldCorridorMapper.Map(context.Frame.Size, centerline, halfWidths)) {
                if (!context.Cells[cell].IsReserved)
                    context.Cells[cell] = context.Cells[cell].WithReserve(reserve).WithClass(FieldCellClass.None);
            }

            context.AddRoute(new FieldRoute(context.Routes.Count, kind, difficulty, centerline, halfWidths, isClosed));
        }

        // A ridge across the middle of the alternative: only its own cells, never the main corridor or a station zone.
        private static void CloseMiddle(FieldGenerationContext context, List<Vector2Int> centerline, int halfWidth) {
            Vector2Int middle = centerline[centerline.Count / 2];
            MarkRidge(context, middle, halfWidth + 1, FieldReserveKind.AlternativeCorridor);
        }

        private static void CapTip(FieldGenerationContext context, Vector2Int tip, int radius) =>
            MarkRidge(context, tip, radius, FieldReserveKind.None);

        private static void MarkRidge(FieldGenerationContext context, Vector2Int center, int radius, FieldReserveKind reserve) {
            for (int y = center.y - radius; y <= center.y + radius; y++) {
                for (int x = center.x - radius; x <= center.x + radius; x++) {
                    bool isRidgeCell = context.Cells.ValidateIndex(x, y) && context.Cells[x, y].Reserve == reserve && !context.Cells[x, y].IsOccupied;
                    if (isRidgeCell)
                        context.Cells[x, y] = context.Cells[x, y].WithClass(FieldCellClass.Ridge);
                }
            }
        }

        private int HalfWidth(float value) =>
            Mathf.Max(_flightFieldConfiguration.PassageClearanceCells, Mathf.RoundToInt(value));

        private static int MaxHalfWidth(FieldRoute route) {
            int widest = 0;
            foreach (int halfWidth in route.HalfWidths)
                widest = Mathf.Max(widest, halfWidth);

            return widest;
        }

        private static int Side(int index) =>
            index % 2 == 0 ? 1 : -1;
    }
}
