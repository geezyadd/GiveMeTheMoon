using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // What the rock query needs to know of a field's cells: which placement stands on a cell and which debris lies in
    // it, whether the cells come from the generator's matrix (server) or from the published field (every peer).
    public interface IFieldRockCells {
        public bool IsInField(Vector2Int cell);
        public bool TryGetPlacementId(Vector2Int cell, out int placementId);
        public bool TryGetDebrisId(Vector2Int cell, out string debrisId);
    }
}
