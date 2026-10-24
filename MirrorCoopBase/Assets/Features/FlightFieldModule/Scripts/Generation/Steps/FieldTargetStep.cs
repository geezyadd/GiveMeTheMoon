using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.FlightFieldModule.Scripts.Structures;
using Features.FlightFieldModule.Scripts.Targets;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // The target balloons of a field, the last step for every layout: the count from the loop and the stage preset,
    // each balloon of a kind picked by weight in a passable cell (no placement, nothing reserved, no wall or ridge)
    // within reach of the main course, picked more often the farther it is from the course, at a random spot and
    // height in the cell, kept only clear of every rock and of the other balloons. It changes no cell, so routes,
    // passage and walls stay as they are. Its own random sequence from the field seed: the same field gets the same
    // balloons, and the other steps draw what they drew before.
    public sealed class FieldTargetStep : IFieldGenerationStep {
        private const int SEED_SALT = 0x7A26E7;
        // The field is not published yet: its rock data needs a number, any number.
        private const int GENERATION_FIELD_NUMBER = 0;
        private const int NO_DEBRIS_SEED = 0;

        private readonly FieldTargetConfiguration _fieldTargetConfiguration;
        private readonly FieldTargetCatalog _fieldTargetCatalog;
        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly IFieldTargetCountRule _fieldTargetCountRule;
        private readonly IFieldTargetRockClearance _fieldTargetRockClearance;
        private readonly IFieldRandomFactory _fieldRandomFactory;

        public FieldTargetStep(FieldTargetConfiguration fieldTargetConfiguration, FieldTargetCatalog fieldTargetCatalog, FieldStructureCatalog fieldStructureCatalog,
                               IFieldTargetCountRule fieldTargetCountRule, IFieldTargetRockClearance fieldTargetRockClearance, IFieldRandomFactory fieldRandomFactory) {
            _fieldTargetConfiguration = fieldTargetConfiguration;
            _fieldTargetCatalog = fieldTargetCatalog;
            _fieldStructureCatalog = fieldStructureCatalog;
            _fieldTargetCountRule = fieldTargetCountRule;
            _fieldTargetRockClearance = fieldTargetRockClearance;
            _fieldRandomFactory = fieldRandomFactory;
        }

        public bool IsUsedBy(FieldLayoutKind layout) =>
            true;

        public void Execute(FieldGenerationContext context) {
            IFieldRandom random = _fieldRandomFactory.Create(unchecked(context.Seed ^ SEED_SALT));
            int count = _fieldTargetCountRule.GetCount(context.Request.LoopIndex, context.Request.TargetCountScale, random);
            List<FieldTargetCell> cells = CollectCells(context);
            if (count == 0 || cells.Count == 0)
                return;

            List<float> runningWeights = SumWeights(cells);

            FieldTargetRockWorkspace rocks = ReadRocks(context);
            for (int i = 0; i < count; i++)
                TryPlaceTarget(context, cells, runningWeights, rocks, random);
        }

        private void TryPlaceTarget(FieldGenerationContext context, List<FieldTargetCell> cells, List<float> runningWeights, FieldTargetRockWorkspace rocks,
                                    IFieldRandom random) {
            FieldTargetDefinition kind = FieldWeightedPick.Pick(_fieldTargetCatalog.Targets, random);
            float clearance = kind.RadiusMeters + _fieldTargetConfiguration.RockClearanceMeters;
            for (int attempt = 0; attempt < _fieldTargetConfiguration.PlacementTriesPerTarget; attempt++) {
                Vector3 position = PickSpot(cells[FieldWeightedPick.PickIndex(runningWeights, random)].Cell, context.Frame.CellSize, random);
                if (!IsSpaced(context.Targets, position) || !_fieldTargetRockClearance.IsClear(rocks, context.Frame, position, clearance))
                    continue;

                context.AddTarget(new FieldTargetPlacement(kind.Id, position));
                return;
            }
        }

        private Vector3 PickSpot(Vector2Int cell, float cellSize, IFieldRandom random) {
            Vector2 heights = _fieldTargetConfiguration.HeightMeters;
            float across = (cell.y + random.Value()) * cellSize;
            float along = (cell.x + random.Value()) * cellSize;
            return new Vector3(across, Mathf.Lerp(heights.x, heights.y, random.Value()), along);
        }

        private bool IsSpaced(IReadOnlyList<FieldTargetPlacement> targets, Vector3 position) {
            float minSpacingSquared = _fieldTargetConfiguration.MinSpacingMeters * _fieldTargetConfiguration.MinSpacingMeters;
            foreach (FieldTargetPlacement target in targets) {
                if ((target.FieldPosition - position).sqrMagnitude < minSpacingSquared)
                    return false;
            }

            return true;
        }

        private List<FieldTargetCell> CollectCells(FieldGenerationContext context) {
            List<Vector2Int> course = CollectMainCourse(context.Routes);
            List<FieldTargetCell> cells = new();
            Vector2Int size = context.Frame.Size;
            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++)
                    AddIfNearTheCourse(context, course, new Vector2Int(x, y), cells);
            }

            return cells;
        }

        private void AddIfNearTheCourse(FieldGenerationContext context, List<Vector2Int> course, Vector2Int cell, List<FieldTargetCell> cells) {
            if (!IsPassable(context.Cells[cell]))
                return;

            float maxDistance = _fieldTargetConfiguration.MaxCourseDistanceMeters;
            float distance = Mathf.Sqrt(SquaredCellDistance(course, cell)) * context.Frame.CellSize;
            if (distance <= maxDistance)
                cells.Add(new FieldTargetCell(cell, 1f + _fieldTargetConfiguration.AwayFromCourseWeight * distance / maxDistance));
        }

        private static List<float> SumWeights(List<FieldTargetCell> cells) {
            List<float> runningWeights = new(cells.Count);
            float total = 0f;
            foreach (FieldTargetCell cell in cells) {
                total += cell.Weight;
                runningWeights.Add(total);
            }

            return runningWeights;
        }

        private static bool IsPassable(FieldCell cell) =>
            cell.IsFree && !cell.IsBlockedClass;

        private static List<Vector2Int> CollectMainCourse(IReadOnlyList<FieldRoute> routes) {
            List<Vector2Int> course = new();
            foreach (FieldRoute route in routes) {
                if (route.Kind == FieldRouteKind.Main)
                    course.AddRange(route.Centerline);
            }

            return course;
        }

        // A field without a main route counts every cell as on the course.
        private static float SquaredCellDistance(List<Vector2Int> course, Vector2Int cell) {
            float nearest = course.Count == 0 ? 0f : float.MaxValue;
            foreach (Vector2Int courseCell in course)
                nearest = Mathf.Min(nearest, (courseCell - cell).sqrMagnitude);

            return nearest;
        }

        // Per generation: generations may run on several threads at once.
        private FieldTargetRockWorkspace ReadRocks(FieldGenerationContext context) {
            FlightField field = new(context.Frame, context.Seed, context.Cells, context.Placements, context.Routes, context.StationACell, context.StationBCell,
                context.CruiseStartCell, isPassageForced: false, context.Profile.Id, context.Difficulty, context.Density, context.Metrics, context.Request.LoopIndex, context.Targets);
            FieldRockFieldData fieldData = new(new FieldDebrisWorkspace(_fieldRandomFactory.Create(NO_DEBRIS_SEED)));
            fieldData.Read(field, GENERATION_FIELD_NUMBER, new MatrixFieldDebrisCells(context.Cells, _fieldStructureCatalog, context.Profile));
            return new FieldTargetRockWorkspace(fieldData);
        }
    }
}
