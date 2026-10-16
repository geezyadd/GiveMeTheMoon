using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Balance numbers of the finished field: the zone shares, the smallest gap between two debris rocks in the main
    // and alternative corridors (laid out exactly as the peers will build them) and the dodges per minute: the debris
    // rocks a ship flying the first main centerline at cruise speed would hit, per minute of that flight.
    public sealed class FieldBalanceMetricStep : IFieldGenerationStep {
        private const float SECONDS_PER_MINUTE = 60f;
        private const float HALF = 0.5f;
        private const float CELL_CENTER = 0.5f;

        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly IFieldDebrisLayout _fieldDebrisLayout;
        private readonly IFieldRandomFactory _fieldRandomFactory;

        public FieldBalanceMetricStep(FlightFieldConfiguration flightFieldConfiguration, FieldStructureCatalog fieldStructureCatalog, IFieldDebrisLayout fieldDebrisLayout,
                                      IFieldRandomFactory fieldRandomFactory) {
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldStructureCatalog = fieldStructureCatalog;
            _fieldDebrisLayout = fieldDebrisLayout;
            _fieldRandomFactory = fieldRandomFactory;
        }

        public void Execute(FieldGenerationContext context) {
            MeasureShares(context);
            Dictionary<Vector2Int, List<FieldDebrisSpot>> corridorDebris = LayOutCorridorDebris(context);
            context.Metrics.MinCorridorGapMeters = MeasureMinGap(corridorDebris, context.Frame.CellSize);
            context.Metrics.DodgesPerMinute = MeasureDodges(context, corridorDebris);
        }

        private static void MeasureShares(FieldGenerationContext context) {
            int blocked = 0;
            int debris = 0;
            int total = 0;
            foreach (FieldCell cell in context.Cells) {
                total++;
                if (cell.IsOccupied)
                    blocked++;
                else if (cell.HasDebris)
                    debris++;
            }

            context.Metrics.BlockedShare = blocked / (float)total;
            context.Metrics.DebrisShare = debris / (float)total;
            context.Metrics.EmptyShare = 1f - context.Metrics.BlockedShare - context.Metrics.DebrisShare;
        }

        private Dictionary<Vector2Int, List<FieldDebrisSpot>> LayOutCorridorDebris(FieldGenerationContext context) {
            Dictionary<Vector2Int, List<FieldDebrisSpot>> spotsByCell = new();
            IFieldRandom random = _fieldRandomFactory.Create(0);
            Vector2Int size = context.Frame.Size;
            for (int y = 0; y < size.y; y++) {
                for (int x = 0; x < size.x; x++) {
                    FieldCell cell = context.Cells[x, y];
                    bool isCorridorDebris = cell.HasDebris && cell.Reserve is FieldReserveKind.RouteCorridor or FieldReserveKind.AlternativeCorridor;
                    if (isCorridorDebris)
                        spotsByCell.Add(new Vector2Int(x, y), LayOut(context, cell, new Vector2Int(x, y), random));
                }
            }

            return spotsByCell;
        }

        private List<FieldDebrisSpot> LayOut(FieldGenerationContext context, FieldCell cell, Vector2Int position, IFieldRandom random) {
            List<FieldDebrisSpot> spots = new();
            FieldDebrisRecipe recipe = _fieldStructureCatalog.GetRequiredDebris(cell.DebrisId).Recipe;
            _fieldDebrisLayout.Fill(recipe, cell.DebrisDensity, context.Seed, position, context.Frame.CellSize, _flightFieldConfiguration.DebrisMinGapMeters, random, spots);
            return spots;
        }

        // Every pair of rocks in one cell or two touching cells; rocks farther apart are more than a cell apart.
        private static float MeasureMinGap(Dictionary<Vector2Int, List<FieldDebrisSpot>> spotsByCell, float cellSize) {
            float minGap = float.PositiveInfinity;
            foreach (KeyValuePair<Vector2Int, List<FieldDebrisSpot>> entry in spotsByCell) {
                for (int y = -1; y <= 1; y++) {
                    for (int x = -1; x <= 1; x++)
                        minGap = Mathf.Min(minGap, MinGapBetween(entry.Key, entry.Value, entry.Key + new Vector2Int(x, y), spotsByCell, cellSize));
                }
            }

            return minGap;
        }

        private static float MinGapBetween(Vector2Int cell, List<FieldDebrisSpot> spots, Vector2Int otherCell, Dictionary<Vector2Int, List<FieldDebrisSpot>> spotsByCell,
                                           float cellSize) {
            if (!spotsByCell.TryGetValue(otherCell, out List<FieldDebrisSpot> others))
                return float.PositiveInfinity;

            float minGap = float.PositiveInfinity;
            for (int i = 0; i < spots.Count; i++) {
                for (int j = 0; j < others.Count; j++) {
                    bool isSameRock = cell == otherCell && i == j;
                    if (!isSameRock)
                        minGap = Mathf.Min(minGap, Vector2.Distance(OnPlane(cell, spots[i], cellSize), OnPlane(otherCell, others[j], cellSize)) - spots[i].Radius - others[j].Radius);
                }
            }

            return minGap;
        }

        // Every rock in the cells around a centerline segment that the ship, flying the segment, would touch; a rock
        // near two segments counts once.
        private float MeasureDodges(FieldGenerationContext context, Dictionary<Vector2Int, List<FieldDebrisSpot>> spotsByCell) {
            IReadOnlyList<Vector2Int> centerline = context.Routes[0].Centerline;
            float cellSize = context.Frame.CellSize;
            float halfShip = _flightFieldConfiguration.ShipWidthMeters * HALF;
            HashSet<(Vector2Int cell, int index)> dodged = new();
            for (int i = 1; i < centerline.Count; i++)
                CollectDodges(centerline[i - 1], centerline[i], spotsByCell, cellSize, halfShip, dodged);

            float minutes = (centerline.Count - 1) * cellSize / _flightFieldConfiguration.MetricCruiseSpeed / SECONDS_PER_MINUTE;
            return minutes > 0f ? dodged.Count / minutes : 0f;
        }

        private static void CollectDodges(Vector2Int from, Vector2Int to, Dictionary<Vector2Int, List<FieldDebrisSpot>> spotsByCell, float cellSize, float halfShip,
                                          HashSet<(Vector2Int cell, int index)> dodged) {
            Vector2 start = CellCenter(from, cellSize);
            Vector2 end = CellCenter(to, cellSize);
            for (int y = -1; y <= 1; y++) {
                for (int x = -1; x <= 1; x++)
                    CollectCellDodges(to + new Vector2Int(x, y), spotsByCell, start, end, cellSize, halfShip, dodged);
            }
        }

        private static void CollectCellDodges(Vector2Int cell, Dictionary<Vector2Int, List<FieldDebrisSpot>> spotsByCell, Vector2 start, Vector2 end, float cellSize, float halfShip,
                                              HashSet<(Vector2Int cell, int index)> dodged) {
            if (!spotsByCell.TryGetValue(cell, out List<FieldDebrisSpot> spots))
                return;

            for (int i = 0; i < spots.Count; i++) {
                if (DistanceToSegment(OnPlane(cell, spots[i], cellSize), start, end) < halfShip + spots[i].Radius)
                    dodged.Add((cell, i));
            }
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end) {
            Vector2 segment = end - start;
            float along = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
            return Vector2.Distance(point, start + segment * along);
        }

        // Field plane coordinates: x across (matrix y), y along (matrix x).
        private static Vector2 OnPlane(Vector2Int cell, FieldDebrisSpot spot, float cellSize) =>
            CellCenter(cell, cellSize) + new Vector2(spot.Offset.x, spot.Offset.z);

        private static Vector2 CellCenter(Vector2Int cell, float cellSize) =>
            new((cell.y + CELL_CENTER) * cellSize, (cell.x + CELL_CENTER) * cellSize);
    }
}
