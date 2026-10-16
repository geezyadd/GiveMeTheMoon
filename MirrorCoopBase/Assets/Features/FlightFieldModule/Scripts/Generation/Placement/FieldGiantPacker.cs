using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    // Merges neighbouring closed cells into as few giant rocks as it can: the footprints of the options (a long one
    // along and across the field) are tried from the largest to the smallest, every anchor in row order, and a
    // footprint is placed wherever all its cells have the class, no placement and one reserve. Among options of the
    // same footprint the weight decides. With a single-cell option every cell of the class ends up closed.
    public sealed class FieldGiantPacker : IFieldGiantPacker {
        private readonly IFieldStructurePlacer _fieldStructurePlacer;
        private readonly IFieldFootprintReader _fieldFootprintReader;

        public FieldGiantPacker(IFieldStructurePlacer fieldStructurePlacer, IFieldFootprintReader fieldFootprintReader) {
            _fieldStructurePlacer = fieldStructurePlacer;
            _fieldFootprintReader = fieldFootprintReader;
        }

        public void Pack(FieldGenerationContext context, FieldCellClass cellClass, IReadOnlyList<FieldObstacleOption> options) {
            foreach (List<FieldPackingChoice> group in GroupByFootprintLargestFirst(options))
                PackGroup(context, cellClass, group);
        }

        // A table of open cells per group skips most anchors at once; an anchor it lets through is still checked
        // cell by cell, because the placements of this group are not in the table.
        private void PackGroup(FieldGenerationContext context, FieldCellClass cellClass, List<FieldPackingChoice> group) {
            FieldFootprint footprint = Read(group[0]);
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

        private void TryPackAt(FieldGenerationContext context, FieldCellClass cellClass, List<FieldPackingChoice> group, FieldFootprint footprint, Vector2Int anchor) {
            FieldReserveKind reserve = context.Cells[anchor + footprint.Cells[0]].Reserve;
            if (!Fits(context, cellClass, footprint, anchor, reserve))
                return;

            FieldPackingChoice choice = PickByWeight(group, context.Random);
            _fieldStructurePlacer.TryPlace(context, choice.Option.Structure, anchor, choice.Rotation, reserve, cellClass);
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

        // A footprint that is longer one way is tried along and across the field.
        private List<List<FieldPackingChoice>> GroupByFootprintLargestFirst(IReadOnlyList<FieldObstacleOption> options) {
            List<List<FieldPackingChoice>> groups = new();
            foreach (FieldObstacleOption option in options) {
                if (option.Weight <= 0f)
                    continue;

                AddToGroup(groups, new FieldPackingChoice(option, FieldRotation.None));
                FieldFootprint footprint = _fieldFootprintReader.Read(option.Structure, FieldRotation.None);
                if (option.Structure.IsRotatable && footprint.Width != footprint.Height)
                    AddToGroup(groups, new FieldPackingChoice(option, FieldRotation.Quarter));
            }

            groups.Sort(CompareLargestFirst);
            return groups;
        }

        private void AddToGroup(List<List<FieldPackingChoice>> groups, FieldPackingChoice choice) {
            foreach (List<FieldPackingChoice> group in groups) {
                if (IsSameFootprint(Read(group[0]), Read(choice))) {
                    group.Add(choice);
                    return;
                }
            }

            groups.Add(new List<FieldPackingChoice> { choice });
        }

        private static bool IsSameFootprint(FieldFootprint first, FieldFootprint second) =>
            first.Width == second.Width && first.Height == second.Height && first.Cells.Count == second.Cells.Count;

        private int CompareLargestFirst(List<FieldPackingChoice> first, List<FieldPackingChoice> second) =>
            Read(second[0]).Cells.Count.CompareTo(Read(first[0]).Cells.Count);

        private FieldFootprint Read(FieldPackingChoice choice) =>
            _fieldFootprintReader.Read(choice.Option.Structure, choice.Rotation);

        private static FieldPackingChoice PickByWeight(List<FieldPackingChoice> group, IFieldRandom random) {
            if (group.Count == 1)
                return group[0];

            float total = 0f;
            foreach (FieldPackingChoice choice in group)
                total += choice.Option.Weight;

            float roll = random.Value() * total;
            foreach (FieldPackingChoice choice in group) {
                roll -= choice.Option.Weight;
                if (roll < 0f)
                    return choice;
            }

            return group[group.Count - 1];
        }
    }
}
