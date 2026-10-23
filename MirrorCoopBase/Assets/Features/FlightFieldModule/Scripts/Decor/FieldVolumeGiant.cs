using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // A giant of an obstacle field as the volume sees it: the sphere its rock stays within, its middle on the flight
    // plane (field space: x across, y along). No volume rock comes into it.
    public readonly struct FieldVolumeGiant {
        private const float HALF = 0.5f;

        public Vector2 Center { get; }
        public float Radius { get; }

        public FieldVolumeGiant(Vector2 center, float radius) {
            Center = center;
            Radius = radius;
        }

        // The main rock stays within half the footprint's shorter side of its middle, which moves off the plane by up
        // to its recipe's height jitter of its size. The pose is the placement's field pose (FieldStructureLayout).
        public static FieldVolumeGiant CreateFromPlacement(FieldStructureDefinition structure, FieldFootprint footprint, Pose fieldPose, float cellSize) {
            float radius = Mathf.Min(footprint.Width, footprint.Height) * cellSize * HALF;
            float heightJitter = structure.HasClusterRecipe ? structure.ClusterRecipe.MainHeightJitter : 0f;
            return new FieldVolumeGiant(new Vector2(fieldPose.position.x, fieldPose.position.z), radius * (1f + 2f * heightJitter));
        }
    }
}
