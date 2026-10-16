using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Structures {
    // How the small debris of a passable cell looks: rocks a few meters to tens of meters across at free spots inside
    // the cell, near the flight plane, so the ship has to fly around them. Every peer lays it out from the field seed
    // and the cell; the gap between two rocks never drops below the configured ship gap.
    [CreateAssetMenu(menuName = "Game/Flight Field/Debris Recipe", fileName = "FieldDebrisRecipe")]
    public sealed class FieldDebrisRecipe : ScriptableObject {
        [SerializeField] private FieldClusterRockOption[] _rocks = Array.Empty<FieldClusterRockOption>();
        [Tooltip("Shared rock materials, picked with equal chance per rock.")]
        [SerializeField] private Material[] _materials = Array.Empty<Material>();
        [Tooltip("Rock diameter, meters, min and max.")]
        [SerializeField] private Vector2 _diameterMeters = new(5f, 20f);
        [Tooltip("Rocks in a cell of density 1; a cell of density d tries d times as many.")]
        [SerializeField, Min(1)] private int _maxRocksPerCell = 3;
        [Tooltip("Random spots tried for one rock before it is dropped.")]
        [SerializeField, Min(1)] private int _placementTries = 8;
        [Tooltip("A rock's middle stands up to this many meters above or below the flight plane.")]
        [SerializeField, Min(0f)] private float _heightJitterMeters = 3f;
        [Tooltip("Random per-axis scale of every rock (non-uniform), min and max.")]
        [SerializeField] private Vector2 _axisScale = new(0.8f, 1.2f);

        public IReadOnlyList<FieldClusterRockOption> Rocks => _rocks;
        public IReadOnlyList<Material> Materials => _materials;
        public Vector2 DiameterMeters => _diameterMeters;
        public int MaxRocksPerCell => _maxRocksPerCell;
        public int PlacementTries => _placementTries;
        public float HeightJitterMeters => _heightJitterMeters;
        public Vector2 AxisScale => _axisScale;
    }
}
