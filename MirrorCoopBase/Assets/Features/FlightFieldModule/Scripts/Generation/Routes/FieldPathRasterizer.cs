using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    // Turns points of a curve into a chain of side-connected cells. Side moves only: with a diagonal step the side
    // cell of the corner may lack the ship clearance even when the whole corridor is free.
    public sealed class FieldPathRasterizer : IFieldPathRasterizer {
        public void Append(List<Vector2Int> path, Vector2Int cell) {
            Vector2Int current = path[path.Count - 1];
            while (current != cell) {
                Vector2Int delta = cell - current;
                current += Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? new Vector2Int(Math.Sign(delta.x), 0) : new Vector2Int(0, Math.Sign(delta.y));
                path.Add(current);
            }
        }

        public Vector2Int ClampInside(Vector2 point, int inset, Vector2Int size) =>
            new(Mathf.Clamp(Mathf.RoundToInt(point.x), inset, size.x - 1 - inset), Mathf.Clamp(Mathf.RoundToInt(point.y), inset, size.y - 1 - inset));
    }
}
