using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Placement;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Fill: obstacles cover the configured share of the free cells outside every reserved zone.
    public sealed class FieldObstacleFillStep : IFieldGenerationStep {
        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly IFieldObstacleScatterer _fieldObstacleScatterer;

        public FieldObstacleFillStep(FlightFieldConfiguration flightFieldConfiguration, IFieldObstacleScatterer fieldObstacleScatterer) {
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldObstacleScatterer = fieldObstacleScatterer;
        }

        public void Execute(FieldGenerationContext context) {
            List<Vector2Int> freeCells = context.Cells.GetIndexes(IsFree);
            int targetCells = Mathf.RoundToInt(freeCells.Count * _flightFieldConfiguration.ObstacleDensity);
            _fieldObstacleScatterer.Scatter(context, freeCells, targetCells, FieldReserveKind.None);
        }

        private static bool IsFree(FieldCell cell) =>
            cell.IsFree;
    }
}
