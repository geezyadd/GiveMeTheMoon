using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Model {
    // How the generated field plays: does the route matter (L_route against L_free outside all corridors), the zone
    // shares and how the debris of the main route feels. Filled by the metric steps, read by the debug map and tests.
    public sealed class FieldMetrics {
        public const int NO_PATH = -1;

        // Cells of the shortest way with the ship clearance from the start of the first main route to B.
        public int RouteLength { get; set; } = NO_PATH;
        // The same way with every corridor cell forbidden; NO_PATH when there is none.
        public int FreeLength { get; set; } = NO_PATH;
        public IReadOnlyList<Vector2Int> RoutePath { get; set; } = new List<Vector2Int>();
        public IReadOnlyList<Vector2Int> FreePath { get; set; } = new List<Vector2Int>();
        public bool IsStraightBlocked { get; set; }
        public bool IsPathMetricMet { get; set; }
        public int PathRepairRounds { get; set; }
        public float BlockedShare { get; set; }
        public float DebrisShare { get; set; }
        public float EmptyShare { get; set; }
        // The smallest gap between two debris rocks in any main or alternative corridor, meters; infinity without debris.
        public float MinCorridorGapMeters { get; set; } = float.PositiveInfinity;
        // Debris rocks on the main centerline the ship has to turn away from per minute at cruise speed.
        public float DodgesPerMinute { get; set; }

        public bool HasFreePath => FreeLength != NO_PATH;
        public float FreeToRouteRatio => HasFreePath && RouteLength > 0 ? FreeLength / (float)RouteLength : float.PositiveInfinity;
    }
}
