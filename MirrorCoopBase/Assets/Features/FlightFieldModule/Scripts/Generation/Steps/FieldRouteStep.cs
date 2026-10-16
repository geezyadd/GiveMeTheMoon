using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation.Routes;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Routes: one centerline and reserved corridor per route request, shaped by the route difficulty.
    public sealed class FieldRouteStep : IFieldGenerationStep {
        private readonly IFieldRouteShaper _fieldRouteShaper;
        private readonly IFieldCenterlinePlanner _fieldCenterlinePlanner;
        private readonly IFieldCorridorMapper _fieldCorridorMapper;

        public FieldRouteStep(IFieldRouteShaper fieldRouteShaper, IFieldCenterlinePlanner fieldCenterlinePlanner, IFieldCorridorMapper fieldCorridorMapper) {
            _fieldRouteShaper = fieldRouteShaper;
            _fieldCenterlinePlanner = fieldCenterlinePlanner;
            _fieldCorridorMapper = fieldCorridorMapper;
        }

        public void Execute(FieldGenerationContext context) {
            IReadOnlyList<FieldRouteRequest> routeRequests = context.Request.RouteRequests;
            for (int routeId = 0; routeId < routeRequests.Count; routeId++)
                context.AddRoute(CreateRoute(context, routeId, routeRequests[routeId].Difficulty));
        }

        private FieldRoute CreateRoute(FieldGenerationContext context, int routeId, float difficulty) {
            FieldCorridorShape shape = _fieldRouteShaper.Shape(difficulty);
            List<Vector2Int> centerline = _fieldCenterlinePlanner.Plan(context, shape);
            foreach (Vector2Int cell in _fieldCorridorMapper.Map(context.Frame.Size, centerline, shape.HalfWidthCells)) {
                if (!context.Cells[cell].IsReserved)
                    context.Cells[cell] = context.Cells[cell].WithReserve(FieldReserveKind.RouteCorridor);
            }

            return new FieldRoute(routeId, difficulty, centerline, shape.WidthCells);
        }
    }
}
