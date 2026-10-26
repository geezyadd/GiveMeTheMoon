using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Balance numbers of the finished field: the zone shares and the dodges per minute: the debris rocks (laid out
    // exactly as the peers will build them) and the giants a ship flying the main centerline at cruise speed would
    // hit, per minute of that flight.
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
            context.Metrics.DodgesPerMinute = MeasureDodges(context, LayOutLineDebris(context, CollectMainLineCells(context)));
        }

        // The cells the dodges are counted in: the main centerline and its neighbours.
        private static HashSet<Vector2Int> CollectMainLineCells(FieldGenerationContext context) {
            HashSet<Vector2Int> cells = new();
            foreach (Vector2Int cell in context.Routes[0].Centerline)
                AddWithNeighbours(cells, cell);

            return cells;
        }

        private static void AddWithNeighbours(HashSet<Vector2Int> cells, Vector2Int center) {
            for (int y = -1; y <= 1; y++) {
                for (int x = -1; x <= 1; x++)
                    cells.Add(center + new Vector2Int(x, y));
            }
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

        private Dictionary<Vector2Int, List<FieldDebrisSpot>> LayOutLineDebris(FieldGenerationContext context, HashSet<Vector2Int> lineCells) {
            Dictionary<Vector2Int, List<FieldDebrisSpot>> spotsByCell = new();
            MatrixFieldDebrisCells cells = new(context.Cells, _fieldStructureCatalog, context.Profile);
            FieldDebrisWorkspace workspace = new(_fieldRandomFactory.Create(0));
            Vector2Int size = context.Frame.Size;
            for (int y = 0; y < size.y; y++) {
                for (int x = 0; x < size.x; x++) {
                    bool isMeasured = context.Cells[x, y].HasDebris && lineCells.Contains(new Vector2Int(x, y));
                    if (isMeasured)
                        spotsByCell.Add(new Vector2Int(x, y), LayOut(context, new Vector2Int(x, y), cells, workspace));
                }
            }

            return spotsByCell;
        }

        private List<FieldDebrisSpot> LayOut(FieldGenerationContext context, Vector2Int position, IFieldDebrisCells cells, FieldDebrisWorkspace workspace) {
            List<FieldDebrisSpot> spots = new();
            _fieldDebrisLayout.Fill(context.Seed, position, context.Frame.CellSize, cells, workspace, spots);
            return spots;
        }

        // Every rock in the cells around a centerline segment that the ship, flying the segment, would touch; a rock
        // near two segments counts once.
        private float MeasureDodges(FieldGenerationContext context, Dictionary<Vector2Int, List<FieldDebrisSpot>> spotsByCell) {
            IReadOnlyList<Vector2Int> centerline = context.Routes[0].Centerline;
            float cellSize = context.Frame.CellSize;
            float halfShip = _flightFieldConfiguration.ShipWidthMeters * HALF;
            HashSet<(Vector2Int cell, int index)> dodged = new();
            HashSet<int> dodgedGiants = new();
            float length = 0f;
            for (int i = 1; i < centerline.Count; i++) {
                CollectDodges(centerline[i - 1], centerline[i], spotsByCell, cellSize, halfShip, dodged);
                CollectGiantDodges(context, centerline[i], dodgedGiants);
                length += Vector2Int.Distance(centerline[i - 1], centerline[i]) * cellSize;
            }

            float minutes = length / _flightFieldConfiguration.MetricCruiseSpeed / SECONDS_PER_MINUTE;
            return minutes > 0f ? (dodged.Count + dodgedGiants.Count) / minutes : 0f;
        }

        private static void CollectGiantDodges(FieldGenerationContext context, Vector2Int cell, HashSet<int> dodgedGiants) {
            if (context.Cells[cell].IsOccupied)
                dodgedGiants.Add(context.Cells[cell].PlacementId);
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
                if (FieldPlaneGeometry.DistanceToSegment(OnPlane(cell, spots[i], cellSize), start, end) < halfShip + spots[i].Radius)
                    dodged.Add((cell, i));
            }
        }

        // Field plane coordinates: x across (matrix y), y along (matrix x).
        private static Vector2 OnPlane(Vector2Int cell, FieldDebrisSpot spot, float cellSize) =>
            CellCenter(cell, cellSize) + new Vector2(spot.Offset.x, spot.Offset.z);

        private static Vector2 CellCenter(Vector2Int cell, float cellSize) =>
            new((cell.y + CELL_CENTER) * cellSize, (cell.x + CELL_CENTER) * cellSize);
    }
}
