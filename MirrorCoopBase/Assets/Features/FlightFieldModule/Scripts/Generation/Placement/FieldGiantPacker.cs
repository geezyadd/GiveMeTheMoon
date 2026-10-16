using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    // Merges neighbouring closed cells into as few giant rocks as it can: the footprints of the options are tried
    // from the largest to the smallest, every anchor in row order, and a footprint is placed wherever all its cells
    // have the class, no placement and one reserve. Among options of the same footprint the weight decides. With a
    // single-cell option every cell of the class ends up closed.
    public sealed class FieldGiantPacker : IFieldGiantPacker {
        private readonly IFieldStructurePlacer _fieldStructurePlacer;
        private readonly IFieldFootprintReader _fieldFootprintReader;

        public FieldGiantPacker(IFieldStructurePlacer fieldStructurePlacer, IFieldFootprintReader fieldFootprintReader) {
            _fieldStructurePlacer = fieldStructurePlacer;
            _fieldFootprintReader = fieldFootprintReader;
        }

        public void Pack(FieldGenerationContext context, FieldCellClass cellClass, IReadOnlyList<FieldObstacleOption> options) {
            foreach (List<FieldObstacleOption> group in GroupByFootprintLargestFirst(options))
                PackGroup(context, cellClass, group);
        }

        // A table of open cells per group skips most anchors at once; an anchor it lets through is still checked
        // cell by cell, because the placements of this group are not in the table.
        private void PackGroup(FieldGenerationContext context, FieldCellClass cellClass, List<FieldObstacleOption> group) {
            FieldFootprint footprint = _fieldFootprintReader.Read(group[0].Structure, FieldRotation.None);
            Vector2Int size = context.Frame.Size;
            int[,] openSums = SumOpenCells(context, cellClass, size);
            for (int y = 0; y + footprint.Height <= size.y; y++) {
                for (int x = 0; x + footprint.Width <= size.x; x++) {
                    Vector2Int anchor = new(x, y);
                    if (CountOpen(openSums, anchor, footprint) >= footprint.Cells.Count)
                        TryPackAt(context, cellClass, group, footprint, anchor);
                }
            }
        }

        // Summed-area table: [x + 1, y + 1] holds the open cells in the box from (0, 0) to (x, y).
        private static int[,] SumOpenCells(FieldGenerationContext context, FieldCellClass cellClass, Vector2Int size) {
            int[,] sums = new int[size.x + 1, size.y + 1];
            for (int y = 0; y < size.y; y++) {
                for (int x = 0; x < size.x; x++) {
                    FieldCell cell = context.Cells[x, y];
                    int open = cell.Class == cellClass && !cell.IsOccupied ? 1 : 0;
                    sums[x + 1, y + 1] = open + sums[x, y + 1] + sums[x + 1, y] - sums[x, y];
                }
            }

            return sums;
        }

        private static int CountOpen(int[,] sums, Vector2Int anchor, FieldFootprint footprint) {
            int right = anchor.x + footprint.Width;
            int top = anchor.y + footprint.Height;
            return sums[right, top] - sums[anchor.x, top] - sums[right, anchor.y] + sums[anchor.x, anchor.y];
        }

        private void TryPackAt(FieldGenerationContext context, FieldCellClass cellClass, List<FieldObstacleOption> group, FieldFootprint footprint, Vector2Int anchor) {
            FieldReserveKind reserve = context.Cells[anchor + footprint.Cells[0]].Reserve;
            if (!Fits(context, cellClass, footprint, anchor, reserve))
                return;

            _fieldStructurePlacer.TryPlace(context, PickByWeight(group, context.Random), anchor, FieldRotation.None, reserve, cellClass);
        }

        private static bool Fits(FieldGenerationContext context, FieldCellClass cellClass, FieldFootprint footprint, Vector2Int anchor, FieldReserveKind reserve) {
            foreach (Vector2Int footprintCell in footprint.Cells) {
                FieldCell cell = context.Cells[anchor + footprintCell];
                bool isOpen = cell.Class == cellClass && !cell.IsOccupied && cell.Reserve == reserve;
                if (!isOpen)
                    return false;
            }

            return true;
        }

        private List<List<FieldObstacleOption>> GroupByFootprintLargestFirst(IReadOnlyList<FieldObstacleOption> options) {
            List<List<FieldObstacleOption>> groups = new();
            foreach (FieldObstacleOption option in options) {
                if (option.Weight > 0f)
                    AddToGroup(groups, option);
            }

            groups.Sort(CompareLargestFirst);
            return groups;
        }

        private void AddToGroup(List<List<FieldObstacleOption>> groups, FieldObstacleOption option) {
            foreach (List<FieldObstacleOption> group in groups) {
                if (IsSameFootprint(group[0].Structure, option.Structure)) {
                    group.Add(option);
                    return;
                }
            }

            groups.Add(new List<FieldObstacleOption> { option });
        }

        private bool IsSameFootprint(FieldStructureDefinition first, FieldStructureDefinition second) {
            FieldFootprint firstFootprint = _fieldFootprintReader.Read(first, FieldRotation.None);
            FieldFootprint secondFootprint = _fieldFootprintReader.Read(second, FieldRotation.None);
            return firstFootprint.Width == secondFootprint.Width && firstFootprint.Height == secondFootprint.Height && firstFootprint.Cells.Count == secondFootprint.Cells.Count;
        }

        private int CompareLargestFirst(List<FieldObstacleOption> first, List<FieldObstacleOption> second) =>
            CountCells(second).CompareTo(CountCells(first));

        private int CountCells(List<FieldObstacleOption> group) =>
            _fieldFootprintReader.Read(group[0].Structure, FieldRotation.None).Cells.Count;

        private static FieldStructureDefinition PickByWeight(List<FieldObstacleOption> group, IFieldRandom random) {
            if (group.Count == 1)
                return group[0].Structure;

            float total = 0f;
            foreach (FieldObstacleOption option in group)
                total += option.Weight;

            float roll = random.Value() * total;
            foreach (FieldObstacleOption option in group) {
                roll -= option.Weight;
                if (roll < 0f)
                    return option.Structure;
            }

            return group[group.Count - 1].Structure;
        }
    }
}
