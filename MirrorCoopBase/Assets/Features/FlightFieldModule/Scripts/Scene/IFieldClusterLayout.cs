using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Structures;

namespace Features.FlightFieldModule.Scripts.Scene {
    public interface IFieldClusterLayout {
        // The main rock comes first. The list is cleared and refilled, so a caller can keep one list.
        public void Fill(FieldClusterRecipe recipe, FieldFootprint footprint, float cellSize, IFieldRandom random, List<FieldClusterRockPose> rocks);
    }
}
