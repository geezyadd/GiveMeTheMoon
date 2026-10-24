using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Structures;

namespace Features.FlightFieldModule.Scripts.Scene {
    public interface IFieldPlacementRockLayout {
        // The look of one placement from the field seed and its index: the trigger's jitter and the rocks of its
        // cluster recipe (none without a recipe). The list is cleared and refilled; the rock index is the list index.
        public FieldStructureJitter Fill(int fieldSeed, int placementIndex, FieldStructureDefinition structure, FieldFootprint footprint, float cellSize,
                                         List<FieldClusterRockPose> rocks);
    }
}
