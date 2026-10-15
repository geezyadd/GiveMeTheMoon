using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Structures {
    // The occupied cells of a structure for one rotation, relative to the corner of its box.
    public sealed class FieldFootprint {
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<Vector2Int> Cells { get; }

        public FieldFootprint(int width, int height, IReadOnlyList<Vector2Int> cells) {
            Width = width;
            Height = height;
            Cells = cells;
        }
    }
}
