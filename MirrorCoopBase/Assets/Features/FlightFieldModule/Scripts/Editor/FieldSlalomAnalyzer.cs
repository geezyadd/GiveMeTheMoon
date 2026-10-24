using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Requests;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor {
    // Lays out every debris rock of a generated field exactly as the peers build them and measures the slalom: the way
    // from the cruise start to B for a ship that needs half the debris gap (ship width + margin) on every side, its
    // length against the straight line, the narrowest gap along it and the rocks of the flight plane. The giants are
    // one rock inside their footprint, so their cells count as they are.
    public sealed class FieldSlalomAnalyzer {
        private const float HALF = 0.5f;
        private const float CELL_CENTER = 0.5f;
        // Gaps wider than twice this are reported as this: only the narrow ones matter.
        private const float CLEARANCE_LIMIT_METERS = 100f;

        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly IFieldDebrisLayout _fieldDebrisLayout;
        private readonly IFieldRandomFactory _fieldRandomFactory;
        private readonly IFieldProfileLookup _fieldProfileLookup;
        private readonly IFieldFootprintReader _fieldFootprintReader;
        private readonly FieldSlalomPathFinder _fieldSlalomPathFinder = new();

        public FieldSlalomAnalyzer(FlightFieldEditorComposition composition) {
            _flightFieldConfiguration = composition.Configuration;
            _fieldStructureCatalog = composition.Catalog;
            _fieldDebrisLayout = composition.DebrisLayout;
            _fieldRandomFactory = composition.RandomFactory;
            _fieldProfileLookup = composition.ProfileLookup;
            _fieldFootprintReader = composition.FootprintReader;
        }

        public FieldSlalomReport Analyze(FlightField field) {
            float cellSize = field.Frame.CellSize;
            FieldDifficultyProfile profile = _fieldProfileLookup.GetRequired(field.ProfileId);
            FieldSlalomObstacles obstacles = new(field.Cells, field.Frame.Size, cellSize);
            List<FieldSlalomRock> rocks = new();
            LayOutRocks(field, profile, obstacles, rocks);
            float clearance = _flightFieldConfiguration.DebrisMinGapMeters * HALF;
            Vector2 start = CellCenter(field.CruiseStartCell, cellSize);
            Vector2 goal = CellCenter(field.StationBCell, cellSize);
            List<Vector2> path = new();
            bool isFound = _fieldSlalomPathFinder.TryFind(obstacles, start, goal, clearance, path);
            float minGap = isFound ? _fieldSlalomPathFinder.MinClearance(obstacles, path, CLEARANCE_LIMIT_METERS) * 2f : 0f;
            FieldSlalomPlane plane = new(field.Frame.LengthCells * field.Frame.WidthCells * cellSize * cellSize, GiantDiameters(field));
            return new FieldSlalomReport(isFound, path, rocks, Length(path), Vector2.Distance(start, goal), minGap, plane);
        }

        // The diameter the main rock of every giant stays within: the footprint's shorter side.
        private List<float> GiantDiameters(FlightField field) {
            List<float> diameters = new();
            foreach (FieldPlacement placement in field.Placements) {
                FieldFootprint footprint = _fieldFootprintReader.Read(_fieldStructureCatalog.GetRequired(placement.StructureId), placement.Rotation);
                diameters.Add(Mathf.Min(footprint.Width, footprint.Height) * field.Frame.CellSize);
            }

            return diameters;
        }

        private void LayOutRocks(FlightField field, FieldDifficultyProfile profile, FieldSlalomObstacles obstacles, List<FieldSlalomRock> rocks) {
            MatrixFieldDebrisCells cells = new(field.Cells, _fieldStructureCatalog, profile);
            FieldDebrisWorkspace workspace = new(_fieldRandomFactory.Create(0));
            List<FieldDebrisSpot> spots = new();
            foreach (Vector2Int cell in field.Cells.GetIndexes(HasDebris)) {
                _fieldDebrisLayout.Fill(field.Seed, cell, field.Frame.CellSize, cells, workspace, spots);
                foreach (FieldDebrisSpot spot in spots)
                    AddRock(CellCenter(cell, field.Frame.CellSize), spot, obstacles, rocks);
            }
        }

        private static void AddRock(Vector2 cellCenter, FieldDebrisSpot spot, FieldSlalomObstacles obstacles, List<FieldSlalomRock> rocks) {
            FieldSlalomRock rock = new(cellCenter + new Vector2(spot.Offset.x, spot.Offset.z), spot.Radius, Mathf.Abs(spot.Offset.y) < spot.Radius);
            rocks.Add(rock);
            obstacles.Add(rock);
        }

        private static float Length(IReadOnlyList<Vector2> path) {
            float length = 0f;
            for (int i = 1; i < path.Count; i++)
                length += Vector2.Distance(path[i - 1], path[i]);

            return length;
        }

        private static bool HasDebris(FieldCell cell) =>
            cell.HasDebris;

        // Field plane: x across (matrix y), y along (matrix x).
        private static Vector2 CellCenter(Vector2Int cell, float cellSize) =>
            new((cell.y + CELL_CENTER) * cellSize, (cell.x + CELL_CENTER) * cellSize);
    }
}
