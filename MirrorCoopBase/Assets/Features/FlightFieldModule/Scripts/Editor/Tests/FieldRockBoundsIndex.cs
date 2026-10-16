using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // Rock bounds in field space, filed under every field cell they reach, so a point is checked against the rocks
    // of its own cell only.
    public sealed class FieldRockBoundsIndex {
        private readonly Dictionary<Vector2Int, List<Bounds>> _boundsByCell = new();
        private readonly float _cellSize;

        public FieldRockBoundsIndex(float cellSize) =>
            _cellSize = cellSize;

        public void Add(Bounds bounds) {
            Vector2Int min = ToCell(bounds.min);
            Vector2Int max = ToCell(bounds.max);
            for (int x = min.x; x <= max.x; x++) {
                for (int y = min.y; y <= max.y; y++)
                    GetOrCreate(new Vector2Int(x, y)).Add(bounds);
            }
        }

        public bool Contains(Vector3 point) {
            if (!_boundsByCell.TryGetValue(ToCell(point), out List<Bounds> candidates))
                return false;

            foreach (Bounds bounds in candidates) {
                if (bounds.Contains(point))
                    return true;
            }

            return false;
        }

        private List<Bounds> GetOrCreate(Vector2Int cell) {
            if (!_boundsByCell.TryGetValue(cell, out List<Bounds> list)) {
                list = new List<Bounds>();
                _boundsByCell.Add(cell, list);
            }

            return list;
        }

        private Vector2Int ToCell(Vector3 point) =>
            new(Mathf.FloorToInt(point.x / _cellSize), Mathf.FloorToInt(point.z / _cellSize));
    }
}
