using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    public interface IFieldStructurePlacer {
        // Places the whole footprint on cells with the given reserve and no placement, or nothing; the cells take the class.
        public bool TryPlace(FieldGenerationContext context, FieldStructureDefinition structure, Vector2Int anchor, FieldRotation rotation, FieldReserveKind reserve,
                             FieldCellClass cellClass);
    }
}
