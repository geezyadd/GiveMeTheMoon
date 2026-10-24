using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    // Puts a structure into the field matrix whole or not at all. The cells are checked and
    // written directly: a long field gets thousands of placements, and a shape matrix per placement cost more than
    // the rest of the generation.
    public sealed class FieldStructurePlacer : IFieldStructurePlacer {
        private readonly IFieldFootprintReader _fieldFootprintReader;

        public FieldStructurePlacer(IFieldFootprintReader fieldFootprintReader) =>
            _fieldFootprintReader = fieldFootprintReader;

        public bool TryPlace(FieldGenerationContext context, FieldStructureDefinition structure, Vector2Int anchor, FieldRotation rotation, FieldReserveKind reserve,
                             FieldCellClass cellClass) {
            FieldFootprint footprint = _fieldFootprintReader.Read(structure, rotation);
            foreach (Vector2Int cell in footprint.Cells) {
                Vector2Int position = anchor + cell;
                if (!context.Cells.ValidateIndex(position) || !IsOpenFor(context.Cells[position], reserve))
                    return false;
            }

            int placementId = context.NextPlacementId;
            FieldCell structureCell = new(reserve, placementId, cellClass);
            foreach (Vector2Int cell in footprint.Cells)
                context.Cells[anchor + cell] = structureCell;

            context.AddPlacement(new FieldPlacement(placementId, structure.Id, anchor, rotation));
            return true;
        }

        // A structure stands only on cells of the reserve it is placed for and never on another structure.
        private static bool IsOpenFor(FieldCell cell, FieldReserveKind reserve) =>
            cell.Reserve == reserve && !cell.IsOccupied;
    }
}
