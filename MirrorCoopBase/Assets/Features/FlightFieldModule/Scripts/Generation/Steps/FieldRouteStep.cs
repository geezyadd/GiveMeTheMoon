using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Noise;
using Features.FlightFieldModule.Scripts.Generation.Routes;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Main routes: the profile's channels per route request, each a winding centerline from the cruise start to
    // station B, shaped by the profile at the route difficulty; the corridor width changes along it by low-frequency noise and never drops below the
    // passage clearance. The corridor cells are reserved, so nothing giant stands in them.
    public sealed class FieldRouteStep : IFieldGenerationStep {
        // Keeps the width noise apart from the other noise layers of the same seed.
        private const int WIDTH_NOISE_SALT = 0x1B873593;
        // The ends stay this far inside the border beyond the passage clearance: the stations stand in the station
        // zones, which the wall leaves open, so the corridor may reach them through the wall band; the swing between
        // them keeps clear of the whole wall, and the corridor narrows near the border so it never cuts the wall band.
        private const int END_INSET_CELLS = 1;
        private const float HALF = 0.5f;

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

        public bool IsUsedBy(FieldLayoutKind layout) =>
            layout == FieldLayoutKind.Corridors;

        public void Execute(FieldGenerationContext context) {
            foreach (FieldRouteRequest routeRequest in context.Request.RouteRequests)
                AddChannels(context, routeRequest.Difficulty);
        }

        // Channels stand side by side, the profile's spread apart, around the straight line.
        private void AddChannels(FieldGenerationContext context, float difficulty) {
            FieldDifficultyProfile profile = context.Profile;
            FieldCorridorShape shape = _fieldRouteShaper.Shape(profile, difficulty);
            int count = Mathf.Max(1, Mathf.RoundToInt(profile.ChannelCount.Evaluate(difficulty)));
            float spread = profile.ChannelSpreadShare.Evaluate(difficulty) * context.Frame.WidthCells;
            for (int channel = 0; channel < count; channel++) {
                float offset = (channel - (count - 1) * HALF) * spread;
                context.AddRoute(CreateRoute(context, context.Routes.Count, difficulty, shape.WithLateralOffset(offset)));
            }
        }

        private FieldRoute CreateRoute(FieldGenerationContext context, int routeId, float difficulty, FieldCorridorShape shape) {
            int widestHalf = shape.HalfWidthCells + Mathf.CeilToInt(shape.WidthVariationCells);
            int inset = widestHalf + context.Profile.WallThicknessCells;
            Vector2Int size = context.Frame.Size;
            int endInset = _flightFieldConfiguration.PassageClearanceCells + END_INSET_CELLS;
            Vector2Int start = _fieldPathRasterizer.ClampInside(context.CruiseStartCell, endInset, size);
            Vector2Int end = _fieldPathRasterizer.ClampInside(context.StationBCell, endInset, size);
            List<Vector2Int> centerline = _fieldCenterlinePlanner.Plan(context, start, end, shape, inset);
            List<int> halfWidths = CreateHalfWidths(context, routeId, centerline, shape);
            foreach (Vector2Int cell in _fieldCorridorMapper.Map(size, centerline, halfWidths)) {
                if (!context.Cells[cell].IsReserved)
                    context.Cells[cell] = context.Cells[cell].WithReserve(FieldReserveKind.RouteCorridor).WithClass(FieldCellClass.None);
            }

            return new FieldRoute(routeId, FieldRouteKind.Main, difficulty, centerline, halfWidths, false);
        }

        private List<int> CreateHalfWidths(FieldGenerationContext context, int routeId, List<Vector2Int> centerline, FieldCorridorShape shape) {
            int seed = unchecked(context.Seed + WIDTH_NOISE_SALT * (routeId + 1));
            float scale = context.Profile.WidthNoiseScaleCells;
            int clearance = _flightFieldConfiguration.PassageClearanceCells;
            List<int> halfWidths = new(centerline.Count);
            for (int i = 0; i < centerline.Count; i++) {
                float swing = (_fieldNoise.Sample(seed, i / scale, 0f) * 2f - 1f) * shape.WidthVariationCells;
                int roomToWall = DistanceToBorder(centerline[i], context.Frame.Size) - context.Profile.WallThicknessCells;
                halfWidths.Add(Mathf.Max(clearance, Mathf.Min(roomToWall, shape.HalfWidthCells + Mathf.RoundToInt(swing))));
            }

            return halfWidths;
        }

        private static int DistanceToBorder(Vector2Int cell, Vector2Int size) =>
            Mathf.Min(Mathf.Min(cell.x, size.x - 1 - cell.x), Mathf.Min(cell.y, size.y - 1 - cell.y));
    }
}
