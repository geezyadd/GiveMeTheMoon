using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Structures;

namespace Features.FlightFieldModule.Scripts.Scene {
    public interface IFieldPlaneCoverLayout {
        // Adds the dense rock field of the recipe: rocks whose mesh bounds close the footprint at the flight plane,
        // apart from gaps narrower than the recipe's largest gap, and extra rocks above and below it.
        public void Add(FieldClusterRecipe recipe, FieldFootprint footprint, float cellSize, IFieldRandom random, List<FieldClusterRockPose> rocks);
    }
}
