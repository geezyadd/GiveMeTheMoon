using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The occupied cells of a field rebuilt from its placement list only (not from the generator's grid), so the
    // tests check what a client building the field from placements would get.
    public sealed class FieldTestOccupancy {
        private static readonly Vector2Int[] _sideSteps = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        private readonly int[,] _placementIds;

        public int Side { get; }

        public FieldTestOccupancy(FlightField field, FieldStructureCatalog catalog, IFieldFootprintReader footprintReader) {
            Side = field.Frame.SideCells;
            _placementIds = new int[Side, Side];
            foreach (FieldPlacement placement in field.Placements)
                Occupy(placement, catalog, footprintReader);
        }

        public bool IsOccupied(Vector2Int cell) =>
            _placementIds[cell.x, cell.y] != FieldCell.NO_PLACEMENT;

        public bool IsInside(Vector2Int cell) =>
            cell.x >= 0 && cell.x < Side && cell.y >= 0 && cell.y < Side;

        // A cell the ship fits through: it and every cell within the clearance (diagonals included) are inside and free.
        public bool HasClearance(Vector2Int cell, int clearance) {
            for (int y = cell.y - clearance; y <= cell.y + clearance; y++) {
                if (!IsRowClear(cell.x, y, clearance))
                    return false;
            }

            return true;
        }

        // Breadth-first search over side moves; a diagonal move with both side cells passable equals two side moves.
        public bool HasPassage(Vector2Int start, Vector2Int end, int clearance) {
            if (!HasClearance(start, clearance))
                return false;

            Queue<Vector2Int> open = new();
            HashSet<Vector2Int> seen = new() { start };
            open.Enqueue(start);
            while (open.Count > 0) {
                Vector2Int cell = open.Dequeue();
                if (cell == end)
                    return true;

                OpenSideNeighbors(cell, clearance, seen, open);
            }

            return false;
        }

        private void OpenSideNeighbors(Vector2Int cell, int clearance, HashSet<Vector2Int> seen, Queue<Vector2Int> open) {
            foreach (Vector2Int step in _sideSteps) {
                Vector2Int next = cell + step;
                if (IsUnseenPassable(next, clearance, seen) && seen.Add(next))
                    open.Enqueue(next);
            }
        }

        private bool IsUnseenPassable(Vector2Int cell, int clearance, HashSet<Vector2Int> seen) =>
            IsInside(cell) && !seen.Contains(cell) && HasClearance(cell, clearance);

        private bool IsRowClear(int x, int y, int clearance) {
            for (int nearX = x - clearance; nearX <= x + clearance; nearX++) {
                if (IsBlocked(new Vector2Int(nearX, y)))
                    return false;
            }

            return true;
        }

        private bool IsBlocked(Vector2Int cell) =>
            !IsInside(cell) || IsOccupied(cell);

        private void Occupy(FieldPlacement placement, FieldStructureCatalog catalog, IFieldFootprintReader footprintReader) {
            Assert.IsTrue(catalog.TryGet(placement.StructureId, out FieldStructureDefinition structure), $"Placement of unknown structure '{placement.StructureId}'.");
            foreach (Vector2Int footprintCell in footprintReader.Read(structure, placement.Rotation).Cells) {
                Vector2Int cell = placement.Anchor + footprintCell;
                Assert.IsTrue(IsInside(cell), $"Placement {placement.Id} ({placement.StructureId}) leaves the field at {cell}.");
                Assert.AreEqual(FieldCell.NO_PLACEMENT, _placementIds[cell.x, cell.y], $"Placements {_placementIds[cell.x, cell.y]} and {placement.Id} overlap at {cell}.");
                _placementIds[cell.x, cell.y] = placement.Id;
            }
        }
    }
}
