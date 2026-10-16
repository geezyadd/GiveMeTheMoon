using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Structures {
    // How a structure looks: one main rock over the middle of the footprint, satellites over its cells and sometimes
    // a cloud of debris. Sizes are shares of the cell (satellites, debris) or of the footprint's shorter side (main
    // rock); heights are in cells from the flight plane. Every rock stays inside the footprint: the client does not
    // know the corridor cells, so no rock may reach into a neighbouring cell. A new look is a new recipe asset.
    [CreateAssetMenu(menuName = "Game/Flight Field/Cluster Recipe", fileName = "FieldClusterRecipe")]
    public sealed class FieldClusterRecipe : ScriptableObject {
        [SerializeField] private FieldClusterRockOption[] _rocks = Array.Empty<FieldClusterRockOption>();
        [Tooltip("Shared rock materials, picked with equal chance per rock: the tint spread of the cluster.")]
        [SerializeField] private Material[] _materials = Array.Empty<Material>();
        [Tooltip("Main rock size as a share of the footprint's shorter side; the layout shrinks it so it stays inside the footprint.")]
        [SerializeField, Range(0.3f, 1.2f)] private float _mainSize = 0.95f;
        [Tooltip("Main rock height offset as a share of its size, either way. The thinnest rock is about 0.13 of its size thick on each side of its middle, so at most 0.1 keeps the flight plane inside it; 0.08 is about 60 % of that.")]
        [SerializeField, Range(0f, 0.1f)] private float _mainHeightJitter = 0.05f;
        [Tooltip("Random per-axis scale of every rock (non-uniform), min and max.")]
        [SerializeField] private Vector2 _axisScale = new(0.8f, 1.2f);
        [SerializeField] private Vector2Int _satelliteCount = new(2, 4);
        [Tooltip("Satellite size as a share of the cell, min and max.")]
        [SerializeField] private Vector2 _satelliteSize = new(0.2f, 0.45f);
        [Tooltip("Satellites and debris stand up to this many cells above or below the flight plane; a wide spread breaks the flat line of the field.")]
        [SerializeField, Range(0f, 3f)] private float _heightSpreadCells = 2f;
        [Tooltip("Chance that a cluster gets debris.")]
        [SerializeField, Range(0f, 1f)] private float _debrisChance = 0.5f;
        [SerializeField] private Vector2Int _debrisCount = new(3, 10);
        [Tooltip("Debris size as a share of the cell, min and max.")]
        [SerializeField] private Vector2 _debrisSize = new(0.03f, 0.08f);

        public IReadOnlyList<FieldClusterRockOption> Rocks => _rocks;
        public IReadOnlyList<Material> Materials => _materials;
        public float MainSize => _mainSize;
        public float MainHeightJitter => _mainHeightJitter;
        public Vector2 AxisScale => _axisScale;
        public Vector2Int SatelliteCount => _satelliteCount;
        public Vector2 SatelliteSize => _satelliteSize;
        public float HeightSpreadCells => _heightSpreadCells;
        public float DebrisChance => _debrisChance;
        public Vector2Int DebrisCount => _debrisCount;
        public Vector2 DebrisSize => _debrisSize;
    }
}
