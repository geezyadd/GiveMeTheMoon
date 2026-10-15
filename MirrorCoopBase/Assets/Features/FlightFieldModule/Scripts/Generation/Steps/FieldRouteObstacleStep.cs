using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation.Placement;
using Features.FlightFieldModule.Scripts.Generation.Routes;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Fill inside the routes: obstacles cover the share of every corridor that IFieldRouteShaper gives its difficulty.
    public sealed class FieldRouteObstacleStep : IFieldGenerationStep {
        private readonly IFieldRouteShaper _fieldRouteShaper;
        private readonly IFieldCorridorMapper _fieldCorridorMapper;
        private readonly IFieldObstacleScatterer _fieldObstacleScatterer;

        public FieldRouteObstacleStep(IFieldRouteShaper fieldRouteShaper, IFieldCorridorMapper fieldCorridorMapper, IFieldObstacleScatterer fieldObstacleScatterer) {
            _fieldRouteShaper = fieldRouteShaper;
            _fieldCorridorMapper = fieldCorridorMapper;
            _fieldObstacleScatterer = fieldObstacleScatterer;
        }

        public void Execute(FieldGenerationContext context) {
            foreach (FieldRoute route in context.Routes)
                FillCorridor(context, route);
        }

        private void FillCorridor(FieldGenerationContext context, FieldRoute route) {
            float obstacleShare = _fieldRouteShaper.Shape(route.Difficulty).ObstacleShare;
            if (obstacleShare <= 0f)
                return;

            List<Vector2Int> corridorCells = _fieldCorridorMapper.Map(context.Frame.SideCells, route.Centerline, route.CorridorHalfWidthCells);
            corridorCells.RemoveAll(cell => !IsFreeCorridorCell(context.Cells[cell]));
            int targetCells = Mathf.RoundToInt(corridorCells.Count * obstacleShare);
            _fieldObstacleScatterer.Scatter(context, corridorCells, targetCells, FieldReserveKind.RouteCorridor);
        }

        // Station zones and the takeoff corridor inside a route corridor stay empty: only route corridor cells take obstacles.
        private static bool IsFreeCorridorCell(FieldCell cell) =>
            cell.Reserve == FieldReserveKind.RouteCorridor && !cell.IsOccupied;
    }
}
