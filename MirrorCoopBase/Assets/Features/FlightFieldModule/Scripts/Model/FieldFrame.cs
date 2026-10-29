using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Model {
    // The coordinate system of a field: every cell <-> world conversion goes through it. Matrix x grows along Forward
    // (A -> B) over LengthCells, matrix y along Right over WidthCells. A world shift changes only Origin.
    public readonly struct FieldFrame {
        private const float CELL_CENTER = 0.5f;

        // The world corner of cell (0, 0) on the flight plane.
        public Vector3 Origin { get; }
        public Vector3 Forward { get; }
        public float CellSize { get; }
        public int LengthCells { get; }
        public int WidthCells { get; }
        // Matrix size: x = length along Forward, y = width along Right.
        public Vector2Int Size => new(LengthCells, WidthCells);
        public float Height => Origin.y;
        public Vector3 Right => Vector3.Cross(Vector3.up, Forward);

        public FieldFrame(Vector3 origin, Vector3 forward, float cellSize, int lengthCells, int widthCells) {
            Origin = origin;
            Forward = forward;
            CellSize = cellSize;
            LengthCells = lengthCells;
            WidthCells = widthCells;
        }

        public Vector3 CellToWorld(Vector2Int cell) =>
            Origin + Forward * ((cell.x + CELL_CENTER) * CellSize) + Right * ((cell.y + CELL_CENTER) * CellSize);

        // The flight plane in field space: x along Right, y along Forward (the field root's local x and z).
        public Vector2 CellToPlane(Vector2Int cell) {
            Vector3 center = CellCenterInField(cell, CellSize);
            return new Vector2(center.x, center.z);
        }

        // The middle of a cell on the flight plane in field space (the field root's local space: x along Right, y up,
        // z along Forward): where the rocks of a debris cell are laid out from, on every peer and on the server.
        public static Vector3 CellCenterInField(Vector2Int cell, float cellSize) =>
            new((cell.y + CELL_CENTER) * cellSize, 0f, (cell.x + CELL_CENTER) * cellSize);

        public Vector2 WorldToPlane(Vector3 point) {
            Vector3 offset = point - Origin;
            return new Vector2(Vector3.Dot(offset, Right), Vector3.Dot(offset, Forward));
        }

        // Field space: x along Right, y up from the flight plane, z along Forward from Origin.
        public Vector3 WorldToField(Vector3 point) {
            Vector3 offset = point - Origin;
            return new Vector3(Vector3.Dot(offset, Right), offset.y, Vector3.Dot(offset, Forward));
        }

        public Vector3 FieldToWorld(Vector3 fieldPosition) =>
            Origin + Right * fieldPosition.x + Vector3.up * fieldPosition.y + Forward * fieldPosition.z;

        public Vector2Int WorldToCell(Vector3 point) {
            Vector3 offset = point - Origin;
            return new Vector2Int(Mathf.FloorToInt(Vector3.Dot(offset, Forward) / CellSize), Mathf.FloorToInt(Vector3.Dot(offset, Right) / CellSize));
        }

        public FieldFrame Shift(Vector3 offset) =>
            new(Origin + offset, Forward, CellSize, LengthCells, WidthCells);
    }
}
