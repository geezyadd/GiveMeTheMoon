using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Structures;

namespace Features.FlightFieldModule.Scripts.Scene {
    public interface IFieldPlaneCoverLayout {
        // Adds the plane rocks of the recipe whose mesh bounds cover every footprint cell at the flight plane.
        public void Add(FieldClusterRecipe recipe, FieldFootprint footprint, float cellSize, IFieldRandom random, List<FieldClusterRockPose> rocks);
    }
}
