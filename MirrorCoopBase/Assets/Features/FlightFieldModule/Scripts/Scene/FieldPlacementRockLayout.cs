using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // The one place a placement's look is drawn: the scene builder of every peer and the server's rock queries both
    // read it, so a rock index names the same rock everywhere.
    public sealed class FieldPlacementRockLayout : IFieldPlacementRockLayout {
        // A large prime keeps the visual sequences of neighbouring placements apart.
        private const int VISUAL_SEED_STEP = 486187739;

        private readonly IFieldClusterLayout _fieldClusterLayout;
        // One sequence reseeded per placement: building and queries run every frame and allocate nothing.
        private readonly IFieldRandom _visualRandom;

        public FieldPlacementRockLayout(IFieldClusterLayout fieldClusterLayout, IFieldRandomFactory fieldRandomFactory) {
            _fieldClusterLayout = fieldClusterLayout;
            _visualRandom = fieldRandomFactory.Create(0);
        }

        public FieldStructureJitter Fill(int fieldSeed, int placementIndex, FieldStructureDefinition structure, FieldFootprint footprint, float cellSize,
                                         List<FieldClusterRockPose> rocks) {
            _visualRandom.Reseed(unchecked(fieldSeed + placementIndex * VISUAL_SEED_STEP));
            float yaw = Mathf.Lerp(-structure.VisualYawJitterDegrees, structure.VisualYawJitterDegrees, _visualRandom.Value());
            float scale = 1f + Mathf.Lerp(-structure.VisualScaleJitter, structure.VisualScaleJitter, _visualRandom.Value());
            rocks.Clear();
            if (structure.HasClusterRecipe)
                _fieldClusterLayout.Fill(structure.ClusterRecipe, footprint, cellSize, _visualRandom, rocks);

            return new FieldStructureJitter(yaw, scale);
        }
    }
}
