using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Obstacles;
using Features.FlightFieldModule.Scripts.Generation.Placement;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // The rocks between the giants of an obstacle field: every cell no giant stands on and nothing keeps empty (station
    // zones, takeoff corridor, voids) gets debris at the obstacle density, one debris kind for the whole field. Every
    // peer lays the rocks out from the field seed (FieldDebrisLayout): the network carries the cells, not the rocks.
    public sealed class FieldObstacleDebrisStep : IFieldGenerationStep {
        private readonly IFieldDebrisPicker _fieldDebrisPicker;
        private readonly IFieldObstacleDensity _fieldObstacleDensity;

        public FieldObstacleDebrisStep(IFieldDebrisPicker fieldDebrisPicker, IFieldObstacleDensity fieldObstacleDensity) {
            _fieldDebrisPicker = fieldDebrisPicker;
            _fieldObstacleDensity = fieldObstacleDensity;
        }

        public bool IsUsedBy(FieldLayoutKind layout) =>
            layout == FieldLayoutKind.Obstacles;

        public void Execute(FieldGenerationContext context) {
            string debris = _fieldDebrisPicker.Pick(context.Profile, context.Random);
            Vector2Int size = context.Frame.Size;
            for (int y = 0; y < size.y; y++) {
                for (int x = 0; x < size.x; x++)
                    FillCell(context, new Vector2Int(x, y), debris);
            }
        }

        private void FillCell(FieldGenerationContext context, Vector2Int cell, string debris) {
            FieldCell fieldCell = context.Cells[cell];
            if (fieldCell.IsOccupied)
                return;

            float density = _fieldObstacleDensity.GetDebrisDensity(context, cell);
            bool isKeptEmpty = fieldCell.IsReserved || context.Density.IsVoid(cell) || density <= 0f;
            context.Cells[cell] = isKeptEmpty ? fieldCell.WithClass(FieldCellClass.Empty) : fieldCell.WithDebris(density, debris);
        }
    }
}
