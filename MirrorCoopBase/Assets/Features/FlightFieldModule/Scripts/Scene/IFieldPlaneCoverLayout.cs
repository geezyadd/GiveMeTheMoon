using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Structures;

namespace Features.FlightFieldModule.Scripts.Scene {
    public interface IFieldPlaneCoverLayout {
        // Adds the giant plane rocks of the recipe whose mesh bounds close the footprint at the flight plane, apart
        // from gaps narrower than the recipe's largest gap. Returns the smallest half height of the giants, meters
        // (0 when the recipe has no plane rocks).
        public float Add(FieldClusterRecipe recipe, FieldFootprint footprint, float cellSize, IFieldRandom random, List<FieldClusterRockPose> rocks);
    }
}
