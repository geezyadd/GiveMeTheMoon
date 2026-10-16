using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Noise;
using Features.FlightFieldModule.Scripts.Generation.Routes;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Main routes: one winding centerline from the cruise start to station B per route request, shaped by the profile
    // at the route difficulty; the corridor width changes along it by low-frequency noise and never drops below the
    // passage clearance. The corridor cells are reserved, so nothing giant stands in them.
    public sealed class FieldRouteStep : IFieldGenerationStep {
        // Keeps the width noise apart from the other noise layers of the same seed.
        private const int WIDTH_NOISE_SALT = 0x1B873593;
        // The ends stay this far inside the border beyond the corridor's half width: the stations stand in the station
        // zones, which the wall leaves open, so the corridor may reach them through the wall band; the swing between
        // them keeps clear of the whole wall.
        private const int END_INSET_CELLS = 1;

        private readonly IFieldRouteShaper _fieldRouteShaper;
        private readonly IFieldCenterlinePlanner _fieldCenterlinePlanner;
        private readonly IFieldCorridorMapper _fieldCorridorMapper;
        private readonly IFieldPathRasterizer _fieldPathRasterizer;
        private readonly IFieldNoise _fieldNoise;
        private readonly FlightFieldConfiguration _flightFieldConfiguration;

        public FieldRouteStep(IFieldRouteShaper fieldRouteShaper, IFieldCenterlinePlanner fieldCenterlinePlanner, IFieldCorridorMapper fieldCorridorMapper,
                              IFieldPathRasterizer fieldPathRasterizer, IFieldNoise fieldNoise, FlightFieldConfiguration flightFieldConfiguration) {
            _fieldRouteShaper = fieldRouteShaper;
            _fieldCenterlinePlanner = fieldCenterlinePlanner;
            _fieldCorridorMapper = fieldCorridorMapper;
            _fieldPathRasterizer = fieldPathRasterizer;
            _fieldNoise = fieldNoise;
            _flightFieldConfiguration = flightFieldConfiguration;
        }

        public void Execute(FieldGenerationContext context) {
            IReadOnlyList<FieldRouteRequest> routeRequests = context.Request.RouteRequests;
            for (int routeId = 0; routeId < routeRequests.Count; routeId++)
                context.AddRoute(CreateRoute(context, routeId, routeRequests[routeId].Difficulty));
        }

        private FieldRoute CreateRoute(FieldGenerationContext context, int routeId, float difficulty) {
            FieldCorridorShape shape = _fieldRouteShaper.Shape(context.Profile, difficulty);
            int widestHalf = shape.HalfWidthCells + Mathf.CeilToInt(shape.WidthVariationCells);
            int inset = widestHalf + context.Profile.WallThicknessCells;
            Vector2Int size = context.Frame.Size;
            Vector2Int start = _fieldPathRasterizer.ClampInside(context.CruiseStartCell, widestHalf + END_INSET_CELLS, size);
            Vector2Int end = _fieldPathRasterizer.ClampInside(context.StationBCell, widestHalf + END_INSET_CELLS, size);
            List<Vector2Int> centerline = _fieldCenterlinePlanner.Plan(context, start, end, shape, inset);
            List<int> halfWidths = CreateHalfWidths(context, routeId, centerline.Count, shape);
            foreach (Vector2Int cell in _fieldCorridorMapper.Map(size, centerline, halfWidths)) {
                if (!context.Cells[cell].IsReserved)
                    context.Cells[cell] = context.Cells[cell].WithReserve(FieldReserveKind.RouteCorridor).WithClass(FieldCellClass.None);
            }

            return new FieldRoute(routeId, FieldRouteKind.Main, difficulty, centerline, halfWidths, false);
        }

        private List<int> CreateHalfWidths(FieldGenerationContext context, int routeId, int count, FieldCorridorShape shape) {
            int seed = unchecked(context.Seed + WIDTH_NOISE_SALT * (routeId + 1));
            float scale = context.Profile.WidthNoiseScaleCells;
            int clearance = _flightFieldConfiguration.PassageClearanceCells;
            List<int> halfWidths = new(count);
            for (int i = 0; i < count; i++) {
                float swing = (_fieldNoise.Sample(seed, i / scale, 0f) * 2f - 1f) * shape.WidthVariationCells;
                halfWidths.Add(Mathf.Max(clearance, shape.HalfWidthCells + Mathf.RoundToInt(swing)));
            }

            return halfWidths;
        }
    }
}
