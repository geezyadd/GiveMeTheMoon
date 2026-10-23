using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Debris {
    // The debris cells as a peer sees them: the debris runs, the placements and the profile of the live field, read
    // once per field.
    public sealed class LiveFieldDebrisCells : IFieldDebrisCells {
        private readonly Dictionary<Vector2Int, (float density, FieldDebrisRecipe recipe)> _debrisByCell = new();
        private readonly HashSet<Vector2Int> _blockedCells = new();

        public FieldDifficultyProfile Profile { get; private set; }

        public void Read(IReadOnlyLiveFieldModel liveFieldModel, FieldStructureCatalog catalog, IFieldFootprintReader footprintReader, FieldDifficultyProfile profile) {
            Profile = profile;
            _debrisByCell.Clear();
            _blockedCells.Clear();
            foreach (FieldDebrisRunEntry run in liveFieldModel.DebrisRuns)
                AddRun(run, catalog.GetRequiredDebris(run.DebrisId).Recipe);

            foreach (FieldPlacementEntry placement in liveFieldModel.Placements)
                AddPlacement(placement, catalog, footprintReader);
        }

        public bool TryGetDebris(Vector2Int cell, out float density, out FieldDebrisRecipe recipe) {
            bool isFound = _debrisByCell.TryGetValue(cell, out (float density, FieldDebrisRecipe recipe) entry);
            density = entry.density;
            recipe = entry.recipe;
            return isFound;
        }

        public bool IsBlocked(Vector2Int cell) =>
            _blockedCells.Contains(cell);

        public bool IsEmpty(Vector2Int cell) =>
            !_blockedCells.Contains(cell) && !_debrisByCell.ContainsKey(cell);

        private void AddRun(FieldDebrisRunEntry run, FieldDebrisRecipe recipe) {
            for (int x = run.Start.x; x < run.Start.x + run.Length; x++)
                _debrisByCell[new Vector2Int(x, run.Start.y)] = (run.Density, recipe);
        }

        private void AddPlacement(FieldPlacementEntry placement, FieldStructureCatalog catalog, IFieldFootprintReader footprintReader) {
            FieldFootprint footprint = footprintReader.Read(catalog.GetRequired(placement.StructureId), placement.Rotation);
            foreach (Vector2Int cell in footprint.Cells)
                _blockedCells.Add(placement.Anchor + cell);
        }
    }
}
