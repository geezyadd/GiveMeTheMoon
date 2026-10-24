using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // The rock cells as a peer has them: the footprint cells of the published placements (placement id = its index
    // + 1, as FieldRockFieldData reads it back) and the cells of the debris runs. Read once per field.
    public sealed class PublishedFieldRockCells : IFieldRockCells {
        // FieldCell.NO_PLACEMENT is 0: the first published placement gets id 1.
        public const int PLACEMENT_ID_OFFSET = 1;

        private readonly Dictionary<Vector2Int, int> _placementIdByCell = new();
        private readonly Dictionary<Vector2Int, string> _debrisIdByCell = new();

        private Vector2Int _size;

        public void Read(Vector2Int size, IReadOnlyList<FieldPlacementEntry> placements, IReadOnlyList<FieldDebrisRunEntry> debrisRuns, FieldStructureCatalog catalog,
                         IFieldFootprintReader footprintReader) {
            _size = size;
            _placementIdByCell.Clear();
            _debrisIdByCell.Clear();
            for (int i = 0; i < placements.Count; i++)
                AddPlacement(placements[i], i + PLACEMENT_ID_OFFSET, catalog, footprintReader);

            foreach (FieldDebrisRunEntry run in debrisRuns)
                AddRun(run);
        }

        public bool IsInField(Vector2Int cell) =>
            cell.x >= 0 && cell.y >= 0 && cell.x < _size.x && cell.y < _size.y;

        public bool TryGetPlacementId(Vector2Int cell, out int placementId) =>
            _placementIdByCell.TryGetValue(cell, out placementId);

        public bool TryGetDebrisId(Vector2Int cell, out string debrisId) =>
            _debrisIdByCell.TryGetValue(cell, out debrisId);

        private void AddPlacement(FieldPlacementEntry placement, int placementId, FieldStructureCatalog catalog, IFieldFootprintReader footprintReader) {
            FieldFootprint footprint = footprintReader.Read(catalog.GetRequired(placement.StructureId), placement.Rotation);
            foreach (Vector2Int cell in footprint.Cells)
                _placementIdByCell[placement.Anchor + cell] = placementId;
        }

        private void AddRun(FieldDebrisRunEntry run) {
            for (int x = run.Start.x; x < run.Start.x + run.Length; x++)
                _debrisIdByCell[new Vector2Int(x, run.Start.y)] = run.DebrisId;
        }
    }
}
