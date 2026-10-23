using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation.Placement;
using Features.FlightFieldModule.Scripts.Generation.Routes;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Passage {
    // Takes out the structures standing on a band around a route centerline.
    public sealed class FieldCorridorClearer : IFieldCorridorClearer {
        private readonly IFieldCorridorMapper _fieldCorridorMapper;
        private readonly IFieldStructurePlacer _fieldStructurePlacer;

        public FieldCorridorClearer(IFieldCorridorMapper fieldCorridorMapper, IFieldStructurePlacer fieldStructurePlacer) {
            _fieldCorridorMapper = fieldCorridorMapper;
            _fieldStructurePlacer = fieldStructurePlacer;
        }

        public void Clear(FieldGenerationContext context, FieldRoute route, int radiusCells) {
            List<int> placementIds = new();
            foreach (Vector2Int cell in _fieldCorridorMapper.Map(context.Frame.Size, route.Centerline, radiusCells)) {
                int placementId = context.Cells[cell].PlacementId;
                if (context.Cells[cell].IsOccupied && !placementIds.Contains(placementId))
                    placementIds.Add(placementId);
            }

            foreach (int placementId in placementIds)
                _fieldStructurePlacer.Remove(context, placementId);
        }
    }
}
