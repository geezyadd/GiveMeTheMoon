using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Beacons {
    // Headings and turns along a centerline of grid cells, measured over a window of cells so one stair step of the
    // raster does not count as a turn.
    public static class FieldBeaconHeading {
        private static readonly Vector2 _alongField = new(1f, 0f);

        // The heading at a cell: from the cell a window before it to the cell a window after it.
        public static Vector2 At(IReadOnlyList<Vector2Int> centerline, int index, int window) =>
            Between(centerline[Mathf.Max(0, index - window)], centerline[Mathf.Min(centerline.Count - 1, index + window)]);

        // The heading change at a cell, degrees: positive turns right (towards matrix +y), negative left.
        public static float TurnAt(IReadOnlyList<Vector2Int> centerline, int index, int window) {
            if (index - window < 0 || index + window >= centerline.Count)
                return 0f;

            Vector2 before = Between(centerline[index - window], centerline[index]);
            Vector2 after = Between(centerline[index], centerline[index + window]);
            return Vector2.SignedAngle(before, after);
        }

        private static Vector2 Between(Vector2Int from, Vector2Int to) {
            Vector2 delta = to - from;
            return delta.sqrMagnitude > 0f ? delta.normalized : _alongField;
        }
    }
}
