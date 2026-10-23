using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Requests;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // Keeps the debris cells near the viewpoint built: the debris runs of the live field are unrolled into cells once
    // per field, then every frame the cells within the debris build radius are built, a few per frame, and the built
    // ones beyond the release radius go back to the pool. Only small rocks near the ship ever stand.
    public sealed class FieldDebrisStreamer : IFieldDebrisStreamer {
        private const float CELL_CENTER = 0.5f;

        private readonly IFieldDebrisSceneBuilder _fieldDebrisSceneBuilder;
        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly LiveFieldDebrisCells _debrisCells = new();
        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly IFieldFootprintReader _fieldFootprintReader;
        private readonly IFieldProfileLookup _fieldProfileLookup;
        private readonly List<Vector2Int> _cells = new();
        private readonly List<bool> _isBuilt = new();

        public FieldDebrisStreamer(IFieldDebrisSceneBuilder fieldDebrisSceneBuilder, FlightFieldConfiguration flightFieldConfiguration, FieldStructureCatalog fieldStructureCatalog,
                                   IFieldFootprintReader fieldFootprintReader, IFieldProfileLookup fieldProfileLookup) {
            _fieldDebrisSceneBuilder = fieldDebrisSceneBuilder;
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldStructureCatalog = fieldStructureCatalog;
            _fieldFootprintReader = fieldFootprintReader;
            _fieldProfileLookup = fieldProfileLookup;
        }

        public void Restart(IReadOnlyLiveFieldModel liveFieldModel) {
            Clear();
            _debrisCells.Read(liveFieldModel.DebrisRuns, liveFieldModel.Placements, _fieldStructureCatalog, _fieldFootprintReader,
                _fieldProfileLookup.GetRequired(liveFieldModel.ProfileId));
            foreach (FieldDebrisRunEntry run in liveFieldModel.DebrisRuns)
                AddRunCells(run);
        }

        public void Stream(Transform root, IReadOnlyLiveFieldModel liveFieldModel, Vector2 viewpointOnPlane) {
            float cellSize = liveFieldModel.CellSize;
            float buildRadiusSquared = _flightFieldConfiguration.DebrisBuildRadius * _flightFieldConfiguration.DebrisBuildRadius;
            float releaseRadiusSquared = _flightFieldConfiguration.DebrisReleaseRadius * _flightFieldConfiguration.DebrisReleaseRadius;
            int budget = _flightFieldConfiguration.DebrisCellsBuiltPerFrame;
            for (int i = 0; i < _cells.Count; i++) {
                float distanceSquared = (CellOnPlane(_cells[i], cellSize) - viewpointOnPlane).sqrMagnitude;
                if (_isBuilt[i] && distanceSquared > releaseRadiusSquared) {
                    _fieldDebrisSceneBuilder.ReleaseCell(_cells[i]);
                    _isBuilt[i] = false;
                }
                else if (!_isBuilt[i] && distanceSquared <= buildRadiusSquared && budget > 0) {
                    budget--;
                    _fieldDebrisSceneBuilder.BuildCell(root, liveFieldModel.Seed, cellSize, _cells[i], _debrisCells);
                    _isBuilt[i] = true;
                }
            }
        }

        public void Clear() {
            _fieldDebrisSceneBuilder.ReleaseAll();
            _cells.Clear();
            _isBuilt.Clear();
        }

        private void AddRunCells(FieldDebrisRunEntry run) {
            for (int x = run.Start.x; x < run.Start.x + run.Length; x++) {
                _cells.Add(new Vector2Int(x, run.Start.y));
                _isBuilt.Add(false);
            }
        }

        // Root space: x along Right (matrix y), z along Forward (matrix x).
        private static Vector2 CellOnPlane(Vector2Int cell, float cellSize) =>
            new((cell.y + CELL_CENTER) * cellSize, (cell.x + CELL_CENTER) * cellSize);
    }
}
