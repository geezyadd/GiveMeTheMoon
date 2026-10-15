using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using Features.MatrixModule.Scripts;
using Features.MatrixModule.Scripts.MatrixFactory;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    // Puts a structure into the field matrix whole or not at all (TryInsertFullShapeMatrix) and takes it out again.
    public sealed class FieldStructurePlacer : IFieldStructurePlacer {
        private readonly IMatrixFactory _matrixFactory;
        private readonly IFieldFootprintReader _fieldFootprintReader;
        private readonly FieldStructureCatalog _fieldStructureCatalog;

        public FieldStructurePlacer(IMatrixFactory matrixFactory, IFieldFootprintReader fieldFootprintReader, FieldStructureCatalog fieldStructureCatalog) {
            _matrixFactory = matrixFactory;
            _fieldFootprintReader = fieldFootprintReader;
            _fieldStructureCatalog = fieldStructureCatalog;
        }

        public bool TryPlace(FieldGenerationContext context, FieldStructureDefinition structure, Vector2Int anchor, FieldRotation rotation, FieldReserveKind reserve) {
            FieldFootprint footprint = _fieldFootprintReader.Read(structure, rotation);
            int placementId = context.NextPlacementId;
            IMatrix<FieldCell> shape = CreateShape(footprint, new FieldCell(reserve, placementId));
            bool isPlaced = context.Cells.TryInsertFullShapeMatrix(shape, anchor.x, anchor.y, out _, cell => IsOpenFor(cell, reserve), IsStructureCell);
            if (isPlaced)
                context.AddPlacement(new FieldPlacement(placementId, structure.Id, anchor, rotation));

            return isPlaced;
        }

        public void Remove(FieldGenerationContext context, int placementId) {
            FieldPlacement placement = context.RemovePlacement(placementId);
            FieldStructureDefinition structure = _fieldStructureCatalog.GetRequired(placement.StructureId);
            foreach (Vector2Int cell in _fieldFootprintReader.Read(structure, placement.Rotation).Cells) {
                Vector2Int position = placement.Anchor + cell;
                context.Cells[position] = context.Cells[position].WithoutPlacement();
            }
        }

        private IMatrix<FieldCell> CreateShape(FieldFootprint footprint, FieldCell structureCell) {
            IMatrix<FieldCell> shape = _matrixFactory.Create(footprint.Width, footprint.Height, default(FieldCell));
            foreach (Vector2Int cell in footprint.Cells)
                shape[cell] = structureCell;

            return shape;
        }

        // A structure stands only on cells of the reserve it is placed for and never on another structure.
        private static bool IsOpenFor(FieldCell cell, FieldReserveKind reserve) =>
            cell.Reserve == reserve && !cell.IsOccupied;

        private static bool IsStructureCell(FieldCell cell) =>
            cell.IsOccupied;
    }
}
