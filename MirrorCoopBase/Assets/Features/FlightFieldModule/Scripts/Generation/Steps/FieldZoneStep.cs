using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Placement;
using Features.FlightFieldModule.Scripts.Generation.Routes;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Zones: decides the class of every cell the earlier steps left open, so the field meets the profile's zone shares.
    // Corridors carry debris at their route's density. Open cells are closed in order of density, highest first, until
    // the blocked share is reached (a cell above the ridge threshold always, a glade or void cell never); then glades
    // and the densest of the rest (rubble beside the ridges) get debris until the debris share is reached; everything
    // else stays empty.
    public sealed class FieldZoneStep : IFieldGenerationStep {
        private readonly IFieldRouteShaper _fieldRouteShaper;
        private readonly IFieldDebrisPicker _fieldDebrisPicker;

        public FieldZoneStep(IFieldRouteShaper fieldRouteShaper, IFieldDebrisPicker fieldDebrisPicker) {
            _fieldRouteShaper = fieldRouteShaper;
            _fieldDebrisPicker = fieldDebrisPicker;
        }

        public bool IsUsedBy(FieldLayoutKind layout) =>
            layout == FieldLayoutKind.Corridors;

        public void Execute(FieldGenerationContext context) {
            int total = context.Frame.LengthCells * context.Frame.WidthCells;
            FillCorridors(context);
            List<Vector2Int> open = CollectOpenCells(context);
            SortByDensity(context.Density, open);
            int blockedTarget = Mathf.RoundToInt(context.Profile.BlockedShare.Evaluate(context.Difficulty) * total) - CountCells(context, IsBlocked);
            List<Vector2Int> rest = CloseDensest(context, open, blockedTarget);
            int debrisTarget = Mathf.RoundToInt(context.Profile.DebrisShare.Evaluate(context.Difficulty) * total) - CountCells(context, HasDebris);
            ScatterDebris(context, rest, debrisTarget);
        }

        private void FillCorridors(FieldGenerationContext context) {
            FieldDifficultyProfile profile = context.Profile;
            float difficulty = context.Difficulty;
            float mainDensity = _fieldRouteShaper.Shape(profile, difficulty).DebrisDensity;
            float branchDensity = Mathf.Clamp01(profile.AlternativeDebrisDensity.Evaluate(difficulty));
            string mainDebris = _fieldDebrisPicker.Pick(profile, context.Random);
            string branchDebris = _fieldDebrisPicker.Pick(profile, context.Random);
            Vector2Int size = context.Frame.Size;
            for (int y = 0; y < size.y; y++) {
                for (int x = 0; x < size.x; x++)
                    FillCorridorCell(context, x, y, mainDensity, mainDebris, branchDensity, branchDebris);
            }
        }

        private static void FillCorridorCell(FieldGenerationContext context, int x, int y, float mainDensity, string mainDebris, float branchDensity, string branchDebris) {
            FieldCell cell = context.Cells[x, y];
            bool isOpenCorridor = cell.IsCorridor && cell.Class == FieldCellClass.None;
            if (!isOpenCorridor)
                return;

            bool isMain = cell.Reserve == FieldReserveKind.RouteCorridor;
            float density = isMain ? mainDensity : branchDensity;
            context.Cells[x, y] = density > 0f ? cell.WithDebris(density, isMain ? mainDebris : branchDebris) : cell.WithClass(FieldCellClass.Empty);
        }

        private static List<Vector2Int> CollectOpenCells(FieldGenerationContext context) {
            List<Vector2Int> open = new();
            Vector2Int size = context.Frame.Size;
            for (int y = 0; y < size.y; y++) {
                for (int x = 0; x < size.x; x++)
                    CollectOpenCell(context, new Vector2Int(x, y), open);
            }

            return open;
        }

        // Reserved cells outside the corridors (station zones, takeoff corridor) are empty for good.
        private static void CollectOpenCell(FieldGenerationContext context, Vector2Int cell, List<Vector2Int> open) {
            FieldCell fieldCell = context.Cells[cell];
            if (fieldCell.Class != FieldCellClass.None)
                return;

            if (fieldCell.IsReserved)
                context.Cells[cell] = fieldCell.WithClass(FieldCellClass.Empty);
            else
                open.Add(cell);
        }

        // Densest first; the row order breaks ties, so the order is the same on every machine.
        private static void SortByDensity(FieldDensityMap map, List<Vector2Int> cells) {
            List<(float density, int order, Vector2Int cell)> keyed = new(cells.Count);
            for (int i = 0; i < cells.Count; i++)
                keyed.Add((map.GetDensity(cells[i]), i, cells[i]));

            keyed.Sort(CompareDensestFirst);
            for (int i = 0; i < cells.Count; i++)
                cells[i] = keyed[i].cell;
        }

        private static int CompareDensestFirst((float density, int order, Vector2Int cell) first, (float density, int order, Vector2Int cell) second) {
            int byDensity = second.density.CompareTo(first.density);
            return byDensity != 0 ? byDensity : first.order.CompareTo(second.order);
        }

        // Returns the cells left open, densest first.
        private static List<Vector2Int> CloseDensest(FieldGenerationContext context, List<Vector2Int> open, int target) {
            float ridgeThreshold = context.Profile.RidgeThreshold.Evaluate(context.Difficulty);
            FieldDensityMap map = context.Density;
            List<Vector2Int> rest = new(open.Count);
            int closed = 0;
            foreach (Vector2Int cell in open) {
                bool isKeptOpen = map.IsGlade(cell) || map.IsVoid(cell);
                float density = map.GetDensity(cell);
                bool isClosed = !isKeptOpen && (closed < target || density >= ridgeThreshold);
                if (isClosed) {
                    context.Cells[cell] = context.Cells[cell].WithClass(FieldCellClass.Ridge);
                    closed++;
                }
                else
                    rest.Add(cell);
            }

            return rest;
        }

        // Glades first, then the densest open cells; voids never.
        private void ScatterDebris(FieldGenerationContext context, List<Vector2Int> rest, int target) {
            float density = Mathf.Clamp01(context.Profile.GladeDebrisDensity.Evaluate(context.Difficulty));
            string debris = _fieldDebrisPicker.Pick(context.Profile, context.Random);
            int scattered = ScatterOver(context, rest, true, density, debris, target);
            ScatterOver(context, rest, false, density, debris, target - scattered);
            foreach (Vector2Int cell in rest) {
                if (context.Cells[cell].Class == FieldCellClass.None)
                    context.Cells[cell] = context.Cells[cell].WithClass(FieldCellClass.Empty);
            }
        }

        private static int ScatterOver(FieldGenerationContext context, List<Vector2Int> cells, bool isGladeOnly, float density, string debris, int budget) {
            int scattered = 0;
            foreach (Vector2Int cell in cells) {
                if (scattered >= budget)
                    break;

                if (!IsDebrisSpot(context, cell, isGladeOnly))
                    continue;

                context.Cells[cell] = context.Cells[cell].WithDebris(density, debris);
                scattered++;
            }

            return scattered;
        }

        private static bool IsDebrisSpot(FieldGenerationContext context, Vector2Int cell, bool isGladeOnly) {
            FieldDensityMap map = context.Density;
            bool isGladeWanted = !isGladeOnly || map.IsGlade(cell);
            return context.Cells[cell].Class == FieldCellClass.None && !map.IsVoid(cell) && isGladeWanted;
        }

        private static int CountCells(FieldGenerationContext context, Predicate<FieldCell> predicate) {
            int count = 0;
            foreach (FieldCell cell in context.Cells) {
                if (predicate(cell))
                    count++;
            }

            return count;
        }

        private static bool IsBlocked(FieldCell cell) =>
            cell.IsBlockedClass;

        private static bool HasDebris(FieldCell cell) =>
            cell.Class == FieldCellClass.Debris;
    }
}
