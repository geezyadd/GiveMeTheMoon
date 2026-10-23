using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    // Puts a structure into the field matrix whole or not at all and takes it out again. The cells are checked and
    // written directly: a long field gets thousands of placements, and a shape matrix per placement cost more than
    // the rest of the generation.
    public sealed class FieldStructurePlacer : IFieldStructurePlacer {
        private readonly IFieldFootprintReader _fieldFootprintReader;
        private readonly FieldStructureCatalog _fieldStructureCatalog;

        public FieldStructurePlacer(IFieldFootprintReader fieldFootprintReader, FieldStructureCatalog fieldStructureCatalog) {
            _fieldFootprintReader = fieldFootprintReader;
            _fieldStructureCatalog = fieldStructureCatalog;
        }

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

        public void Remove(FieldGenerationContext context, int placementId) {
            FieldPlacement placement = context.RemovePlacement(placementId);
            FieldStructureDefinition structure = _fieldStructureCatalog.GetRequired(placement.StructureId);
            foreach (Vector2Int cell in _fieldFootprintReader.Read(structure, placement.Rotation).Cells) {
                Vector2Int position = placement.Anchor + cell;
                context.Cells[position] = context.Cells[position].WithoutPlacement();
            }
        }

        // A structure stands only on cells of the reserve it is placed for and never on another structure.
        private static bool IsOpenFor(FieldCell cell, FieldReserveKind reserve) =>
            cell.Reserve == reserve && !cell.IsOccupied;
    }
}
