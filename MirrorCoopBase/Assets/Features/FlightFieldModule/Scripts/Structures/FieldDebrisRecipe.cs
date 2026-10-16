using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Structures {
    // How the small debris of passable cells looks: rocks ten to thirty meters across on a jittered grid near the flight
    // plane, so the ship has to fly around them. Every peer lays it out from the field seed and the field position;
    // the gap between two rocks never drops below the configured ship gap.
    [CreateAssetMenu(menuName = "Game/Flight Field/Debris Recipe", fileName = "FieldDebrisRecipe")]
    public sealed class FieldDebrisRecipe : ScriptableObject {
        [SerializeField] private FieldClusterRockOption[] _rocks = Array.Empty<FieldClusterRockOption>();
        [Tooltip("Shared rock materials, picked with equal chance per rock.")]
        [SerializeField] private Material[] _materials = Array.Empty<Material>();
        [Tooltip("Rock diameter, meters, min and max.")]
        [SerializeField] private Vector2 _diameterMeters = new(10f, 30f);
        [Tooltip("Spacing of the jittered grid the rocks stand on, meters: at least the ship gap plus the largest diameter; whatever is left over is the jitter. A cell of density d keeps a share d of its grid points.")]
        [SerializeField, Min(1f)] private float _spacingMeters = 60f;
        [Tooltip("A rock's middle stands up to this many meters above or below the flight plane.")]
        [SerializeField, Min(0f)] private float _heightJitterMeters = 15f;
        [Tooltip("Random per-axis scale of every rock (non-uniform), min and max.")]
        [SerializeField] private Vector2 _axisScale = new(0.8f, 1.2f);

        public IReadOnlyList<FieldClusterRockOption> Rocks => _rocks;
        public IReadOnlyList<Material> Materials => _materials;
        public Vector2 DiameterMeters => _diameterMeters;
        public float SpacingMeters => _spacingMeters;
        public float HeightJitterMeters => _heightJitterMeters;
        public Vector2 AxisScale => _axisScale;
    }
}
