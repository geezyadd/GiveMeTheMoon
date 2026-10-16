using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Structures {
    // How a structure looks: one or two giant plane rocks that close the footprint at the flight plane and rise far
    // above and below it (without plane rocks, one main rock over the middle of the footprint), satellites standing
    // out of the giant's top and bottom, and sometimes a cloud of debris. Sizes are shares of the cell (satellites,
    // debris) or of the footprint's shorter side (main rock). Satellites and debris stay inside the footprint; a giant
    // reaches past it at most by its protrusion. A new look is a new recipe asset.
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
        [Tooltip("Debris, and satellites of a recipe without plane rocks, stand up to this many cells above or below the flight plane; with plane rocks the satellites stand out of the giant's top and bottom.")]
        [SerializeField, Range(0f, 3f)] private float _heightSpreadCells = 2f;
        [Tooltip("Chance that a cluster gets debris.")]
        [SerializeField, Range(0f, 1f)] private float _debrisChance = 0.5f;
        [SerializeField] private Vector2Int _debrisCount = new(3, 10);
        [Tooltip("Debris size as a share of the cell, min and max.")]
        [SerializeField] private Vector2 _debrisSize = new(0.03f, 0.08f);

        [Header("Closed flight plane")]
        [Tooltip("Chunky rocks that close the footprint at the flight plane: one or two giants per footprint, turned by quarter turns plus a little jitter and stretched to the footprint. Empty = no plane cover.")]
        [SerializeField] private FieldClusterRockOption[] _planeRocks = Array.Empty<FieldClusterRockOption>();
        [Tooltip("The smallest one-sided horizontal extent of the mesh bounds of any plane rock along its own x and z at size 1, from the pivot.")]
        [SerializeField, Range(0.1f, 0.5f)] private float _planeHalfExtentShare = 0.41f;
        [Tooltip("The largest horizontal extent of any plane rock along its x or z at size 1: what a yaw jitter swings into the other axis.")]
        [SerializeField, Range(0.1f, 1f)] private float _planeReachShare = 0.5f;
        [Tooltip("The smallest one-sided vertical extent of the mesh bounds of any plane rock at size 1, from the pivot.")]
        [SerializeField, Range(0.1f, 0.5f)] private float _planeHalfHeightShare = 0.31f;
        [Tooltip("A plane rock turns by quarter turns plus up to this many degrees either way.")]
        [SerializeField, Range(0f, 15f)] private float _planeYawJitterDegrees = 4f;
        [Tooltip("Gap a plane rock leaves inside the edge of its block, meters, min and max; two neighbours leave at most the max between them, which must stay below the configured plane gap.")]
        [SerializeField] private Vector2 _planeGapMeters = new(0f, 5f);
        [Tooltip("How far a plane rock may stick out past its block, meters, min and max: some giants stand out, some sit back.")]
        [SerializeField] private Vector2 _planeProtrusionMeters = new(0f, 12f);
        [Tooltip("A plane rock's middle moves up to this many meters sideways from the middle of its block.")]
        [SerializeField, Min(0f)] private float _planeJitterMeters = 3f;
        [Tooltip("Half height of a plane rock per meter of its block's shorter side, before the clamp below.")]
        [SerializeField, Min(0f)] private float _planeHalfHeightPerMeter = 1.2f;
        [Tooltip("Half height of a plane rock above and below the flight plane, meters, min and max.")]
        [SerializeField] private Vector2 _planeHalfHeightMeters = new(60f, 200f);
        [Tooltip("A plane rock's half height stays below this many times its block's shorter side: a small block makes a boulder, not a pillar.")]
        [SerializeField, Min(0.1f)] private float _planeMaxHeightToWidth = 1.5f;
        [Tooltip("Random change of a plane rock's half height, as a share either way: an uneven top and bottom.")]
        [SerializeField, Range(0f, 0.5f)] private float _planeHeightVariation = 0.25f;
        [Tooltip("A plane rock's middle moves up to this many meters up or down; it stays far inside the rock's height.")]
        [SerializeField, Min(0f)] private float _planeHeightJitterMeters = 25f;
        [Tooltip("Chance that a footprint of at least the split size is closed by two giants side by side instead of one.")]
        [SerializeField, Range(0f, 1f)] private float _planeSplitChance = 0.5f;
        [Tooltip("Cells of the longer side from which a footprint may be split into two giants.")]
        [SerializeField, Min(2)] private int _planeSplitMinCells = 3;

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
        public IReadOnlyList<FieldClusterRockOption> PlaneRocks => _planeRocks;
        public bool HasPlaneCover => _planeRocks.Length > 0;
        public float PlaneHalfExtentShare => _planeHalfExtentShare;
        public float PlaneReachShare => _planeReachShare;
        public float PlaneHalfHeightShare => _planeHalfHeightShare;
        public float PlaneYawJitterDegrees => _planeYawJitterDegrees;
        public Vector2 PlaneGapMeters => _planeGapMeters;
        public Vector2 PlaneProtrusionMeters => _planeProtrusionMeters;
        public float PlaneJitterMeters => _planeJitterMeters;
        public float PlaneHalfHeightPerMeter => _planeHalfHeightPerMeter;
        public Vector2 PlaneHalfHeightMeters => _planeHalfHeightMeters;
        public float PlaneMaxHeightToWidth => _planeMaxHeightToWidth;
        public float PlaneHeightVariation => _planeHeightVariation;
        public float PlaneHeightJitterMeters => _planeHeightJitterMeters;
        public float PlaneSplitChance => _planeSplitChance;
        public int PlaneSplitMinCells => _planeSplitMinCells;
    }
}
