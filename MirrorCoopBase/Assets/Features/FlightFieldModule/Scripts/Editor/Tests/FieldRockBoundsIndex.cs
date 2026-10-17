using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // Rock bounds in field space, filed under every field cell they reach (with a margin), so a point is checked
    // against the rocks near its own cell only.
    public sealed class FieldRockBoundsIndex {
        private readonly Dictionary<Vector2Int, List<Bounds>> _boundsByCell = new();
        private readonly float _cellSize;
        private readonly float _margin;

        // Margin: the farthest a point may lie from bounds and still be found.
        public FieldRockBoundsIndex(float cellSize, float margin) {
            _cellSize = cellSize;
            _margin = margin;
        }

        public void Add(Bounds bounds) {
            Vector3 margin = new(_margin, 0f, _margin);
            Vector2Int min = ToCell(bounds.min - margin);
            Vector2Int max = ToCell(bounds.max + margin);
            for (int x = min.x; x <= max.x; x++) {
                for (int y = min.y; y <= max.y; y++)
                    GetOrCreate(new Vector2Int(x, y)).Add(bounds);
            }
        }

        // Inside the bounds of a rock, or at most the distance from them along the plane (x, z).
        public bool IsWithin(Vector3 point, float distance) {
            if (!_boundsByCell.TryGetValue(ToCell(point), out List<Bounds> candidates))
                return false;

            Vector3 widening = new(distance * 2f, 0f, distance * 2f);
            foreach (Bounds bounds in candidates) {
                Bounds widened = new(bounds.center, bounds.size + widening);
                if (widened.Contains(point))
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
