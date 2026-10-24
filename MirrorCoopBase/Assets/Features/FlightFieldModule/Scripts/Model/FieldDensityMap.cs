using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Model {
    // The obstacle weight of every cell (0 = thin, 1 = dense) and the large empty areas: the obstacle density follows
    // it, the debug map shows it.
    public sealed class FieldDensityMap {
        private readonly float[,] _density;
        private readonly bool[,] _isVoid;

        public Vector2Int Size { get; }

        public FieldDensityMap(Vector2Int size) {
            Size = size;
            _density = new float[size.x, size.y];
            _isVoid = new bool[size.x, size.y];
        }

        public float GetDensity(Vector2Int cell) =>
            _density[cell.x, cell.y];

        // A large empty area of the flight: nothing stands in it, not even debris.
        public bool IsVoid(Vector2Int cell) =>
            _isVoid[cell.x, cell.y];

        public void SetDensity(Vector2Int cell, float density) =>
            _density[cell.x, cell.y] = density;

        public void MarkVoid(Vector2Int cell) =>
            _isVoid[cell.x, cell.y] = true;
    }
}
