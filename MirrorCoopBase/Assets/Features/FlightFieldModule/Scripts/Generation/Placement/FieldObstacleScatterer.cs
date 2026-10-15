using System;
using System.Collections.Generic;
using System.Linq;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    // Draws obstacles by weight until they cover the target number of cells, then places the large ones first at
    // random anchor cells; an obstacle that finds no room in its tries is dropped.
    public sealed class FieldObstacleScatterer : IFieldObstacleScatterer {
        private const int ROTATION_COUNT = 4;

        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly IFieldStructurePicker _fieldStructurePicker;
        private readonly IFieldStructurePlacer _fieldStructurePlacer;
        private readonly IFieldFootprintReader _fieldFootprintReader;

        public FieldObstacleScatterer(FieldStructureCatalog fieldStructureCatalog, FlightFieldConfiguration flightFieldConfiguration, IFieldStructurePicker fieldStructurePicker,
                                      IFieldStructurePlacer fieldStructurePlacer, IFieldFootprintReader fieldFootprintReader) {
            _fieldStructureCatalog = fieldStructureCatalog;
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldStructurePicker = fieldStructurePicker;
            _fieldStructurePlacer = fieldStructurePlacer;
            _fieldFootprintReader = fieldFootprintReader;
        }

        public void Scatter(FieldGenerationContext context, IReadOnlyList<Vector2Int> anchorCells, int targetCells, FieldReserveKind reserve) {
            if (targetCells <= 0 || anchorCells.Count == 0)
                return;

            List<FieldStructureDefinition> obstacles = _fieldStructureCatalog.Structures.Where(IsRandomObstacle).ToList();
            if (obstacles.Count == 0)
                throw new InvalidOperationException($"{_fieldStructureCatalog.name} has no obstacle with a positive weight to fill the field with.");

            foreach (FieldStructureDefinition obstacle in Draw(obstacles, targetCells, context.Random).OrderByDescending(CountCells))
                TryPlaceAnywhere(context, obstacle, anchorCells, reserve);
        }

        private List<FieldStructureDefinition> Draw(IReadOnlyList<FieldStructureDefinition> obstacles, int targetCells, IFieldRandom random) {
            List<FieldStructureDefinition> drawn = new();
            for (int coveredCells = 0; coveredCells < targetCells;) {
                FieldStructureDefinition obstacle = _fieldStructurePicker.PickByWeight(obstacles, random);
                drawn.Add(obstacle);
                coveredCells += CountCells(obstacle);
            }

            return drawn;
        }

        private void TryPlaceAnywhere(FieldGenerationContext context, FieldStructureDefinition obstacle, IReadOnlyList<Vector2Int> anchorCells, FieldReserveKind reserve) {
            for (int attempt = 0; attempt < _flightFieldConfiguration.PlacementTries; attempt++) {
                Vector2Int anchor = anchorCells[context.Random.Range(0, anchorCells.Count)];
                FieldRotation rotation = obstacle.IsRotatable ? (FieldRotation)context.Random.Range(0, ROTATION_COUNT) : FieldRotation.None;
                if (_fieldStructurePlacer.TryPlace(context, obstacle, anchor, rotation, reserve))
                    return;
            }
        }

        private int CountCells(FieldStructureDefinition structure) =>
            _fieldFootprintReader.Read(structure, FieldRotation.None).Cells.Count;

        private static bool IsRandomObstacle(FieldStructureDefinition structure) =>
            structure.IsRandomObstacle;
    }
}
