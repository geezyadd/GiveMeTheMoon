using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Rocks;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Checks a balloon spot against the rocks of a field. The debris of the cells around the spot is read exactly, as
    // the rock reader lays it out; a cell a placement stands on (a giant, a wall or ridge rock) keeps the spot a cell
    // farther away than the sphere, because its rocks stick out of the footprint by at most about a cell (a giant's
    // main rock, a wall's plane cover spill of 35 m): laying out every wall rock near every tried spot is far too slow.
    // Keeps nothing between calls: the debris it laid out lives in the caller's workspace, so the generation threads
    // share no state and a cell is laid out once per field.
    public sealed class FieldTargetRockClearance : IFieldTargetRockClearance {
        // A debris rock's middle stays in its cell and it is at most about 150 m across: two cells around the spot hold
        // every debris rock that can reach it.
        private const int REACH_CELLS = 2;
        private const float PLACEMENT_SPILL_CELLS = 1f;

        private readonly IFieldRockReader _fieldRockReader;

        public FieldTargetRockClearance(IFieldRockReader fieldRockReader) =>
            _fieldRockReader = fieldRockReader;

        public bool IsClear(FieldTargetRockWorkspace workspace, FieldFrame frame, Vector3 fieldPosition, float radius) {
            Vector3 worldPosition = frame.Origin + Quaternion.LookRotation(frame.Forward, Vector3.up) * fieldPosition;
            Vector2Int center = new(Mathf.FloorToInt(fieldPosition.z / frame.CellSize), Mathf.FloorToInt(fieldPosition.x / frame.CellSize));
            int reach = REACH_CELLS + Mathf.CeilToInt(radius / frame.CellSize);
            for (int along = center.x - reach; along <= center.x + reach; along++) {
                if (!IsRowClear(workspace, frame, along, center.y - reach, center.y + reach, fieldPosition, worldPosition, radius))
                    return false;
            }

            return true;
        }

        private bool IsRowClear(FieldTargetRockWorkspace workspace, FieldFrame frame, int along, int firstAcross, int lastAcross, Vector3 fieldPosition, Vector3 worldPosition,
                                float radius) {
            for (int across = firstAcross; across <= lastAcross; across++) {
                Vector2Int cell = new(along, across);
                if (workspace.FieldData.Field.Cells.ValidateIndex(cell) && !IsCellClear(workspace, frame, cell, fieldPosition, worldPosition, radius))
                    return false;
            }

            return true;
        }

        private bool IsCellClear(FieldTargetRockWorkspace workspace, FieldFrame frame, Vector2Int cell, Vector3 fieldPosition, Vector3 worldPosition, float radius) {
            FieldCell fieldCell = workspace.FieldData.Field.Cells[cell];
            if (fieldCell.IsOccupied && PlaneDistanceToCell(cell, frame.CellSize, fieldPosition) < radius + PLACEMENT_SPILL_CELLS * frame.CellSize)
                return false;

            return !fieldCell.HasDebris || AreRocksClear(ReadDebrisRocks(workspace, frame, cell), worldPosition, radius);
        }

        // Field space: x across (the cell's y), z along (the cell's x).
        private static float PlaneDistanceToCell(Vector2Int cell, float cellSize, Vector3 fieldPosition) {
            float across = Mathf.Clamp(fieldPosition.x, cell.y * cellSize, (cell.y + 1) * cellSize);
            float along = Mathf.Clamp(fieldPosition.z, cell.x * cellSize, (cell.x + 1) * cellSize);
            return new Vector2(fieldPosition.x - across, fieldPosition.z - along).magnitude;
        }

        private List<FieldRockDescription> ReadDebrisRocks(FieldTargetRockWorkspace workspace, FieldFrame frame, Vector2Int cell) {
            if (workspace.TryGetDebrisRocks(cell, out List<FieldRockDescription> rocks))
                return rocks;

            rocks = new List<FieldRockDescription>();
            _fieldRockReader.ReadDebrisCell(workspace.FieldData, frame, cell, rocks);
            workspace.AddDebrisRocks(cell, rocks);
            return rocks;
        }

        private static bool AreRocksClear(List<FieldRockDescription> rocks, Vector3 worldPosition, float radius) {
            foreach (FieldRockDescription rock in rocks) {
                float reach = rock.Radius + radius;
                if ((rock.Center - worldPosition).sqrMagnitude < reach * reach)
                    return false;
            }

            return true;
        }
    }
}
