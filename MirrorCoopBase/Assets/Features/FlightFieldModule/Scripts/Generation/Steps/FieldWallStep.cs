using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Placement;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Wall: every free border cell is closed by the first wall structure from the configuration that fits there;
    // reserved border cells stay open.
    public sealed class FieldWallStep : IFieldGenerationStep {
        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly IFieldStructurePlacer _fieldStructurePlacer;
        private readonly IFieldFootprintReader _fieldFootprintReader;

        public FieldWallStep(FlightFieldConfiguration flightFieldConfiguration, IFieldStructurePlacer fieldStructurePlacer, IFieldFootprintReader fieldFootprintReader) {
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldStructurePlacer = fieldStructurePlacer;
            _fieldFootprintReader = fieldFootprintReader;
        }

        public void Execute(FieldGenerationContext context) {
            foreach (Vector2Int cell in GetBorderCells(context.Frame.SideCells)) {
                if (context.Cells[cell].IsFree)
                    CloseWithFirstFitting(context, cell);
            }
        }

        private void CloseWithFirstFitting(FieldGenerationContext context, Vector2Int cell) {
            foreach (FieldStructureDefinition structure in _flightFieldConfiguration.WallStructures) {
                if (TryClose(context, structure, cell))
                    return;
            }
        }

        // The footprint box is pushed inside the field so that it covers the border cell.
        private bool TryClose(FieldGenerationContext context, FieldStructureDefinition structure, Vector2Int cell) {
            FieldFootprint footprint = _fieldFootprintReader.Read(structure, FieldRotation.None);
            int side = context.Frame.SideCells;
            Vector2Int anchor = new(Mathf.Clamp(cell.x, 0, side - footprint.Width), Mathf.Clamp(cell.y, 0, side - footprint.Height));
            bool isCovered = ContainsCell(footprint, cell - anchor);
            return isCovered && _fieldStructurePlacer.TryPlace(context, structure, anchor, FieldRotation.None, FieldReserveKind.None);
        }

        private static bool ContainsCell(FieldFootprint footprint, Vector2Int cell) {
            foreach (Vector2Int footprintCell in footprint.Cells) {
                if (footprintCell == cell)
                    return true;
            }

            return false;
        }

        private static List<Vector2Int> GetBorderCells(int side) {
            List<Vector2Int> cells = new();
            for (int x = 0; x < side; x++) {
                cells.Add(new Vector2Int(x, 0));
                cells.Add(new Vector2Int(x, side - 1));
            }

            for (int y = 1; y < side - 1; y++) {
                cells.Add(new Vector2Int(0, y));
                cells.Add(new Vector2Int(side - 1, y));
            }

            return cells;
        }
    }
}
