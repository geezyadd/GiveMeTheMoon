using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor {
    // The colors of the debug field map, shared by the painter and the legend of the window.
    public static class FlightFieldMapLegend {
        public static readonly Color Free = new(0.12f, 0.13f, 0.16f);
        public static readonly Color StationZone = new(0.16f, 0.45f, 0.24f);
        public static readonly Color TakeoffCorridor = new(0.15f, 0.38f, 0.5f);
        public static readonly Color RouteCorridor = new(0.42f, 0.36f, 0.14f);
        public static readonly Color Structure = new(0.62f, 0.6f, 0.58f);
        public static readonly Color StructureOutline = new(0.3f, 0.29f, 0.28f);
        public static readonly Color Centerline = new(0.9f, 0.2f, 0.2f);
        public static readonly Color StationA = new(0.3f, 0.6f, 1f);
        public static readonly Color StationB = new(1f, 0.35f, 0.9f);
        public static readonly Color CruiseStart = Color.white;
        public static readonly Color Ship = new(1f, 0.85f, 0.1f);
    }
}
