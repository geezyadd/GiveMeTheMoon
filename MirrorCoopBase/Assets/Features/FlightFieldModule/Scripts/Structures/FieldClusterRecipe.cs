using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Structures {
    // How a structure looks: a dense field of rocks at random positions, sizes and 3D turns that closes the footprint
    // at the flight plane (every gap narrower than the ship), with extra rocks above and below it; a recipe without
    // dense rocks gets one main rock over the middle of the footprint, satellites and sometimes a cloud of debris.
    // Sizes are shares of the cell (satellites, debris) or of the footprint's shorter side (main rock). A dense-field
    // rock reaches past the footprint at most by the spill. A new look is a new recipe asset.
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
        [Tooltip("Satellites and debris (of a recipe without dense rocks) stand up to this many cells above or below the flight plane.")]
        [SerializeField, Range(0f, 3f)] private float _heightSpreadCells = 2f;
        [Tooltip("Chance that a cluster gets debris.")]
        [SerializeField, Range(0f, 1f)] private float _debrisChance = 0.5f;
        [SerializeField] private Vector2Int _debrisCount = new(3, 10);
        [Tooltip("Debris size as a share of the cell, min and max.")]
        [SerializeField] private Vector2 _debrisSize = new(0.03f, 0.08f);

        [Header("Dense rock field (walls and ridges)")]
        [Tooltip("Chunky rocks that close the footprint at the flight plane, at random positions, sizes and 3D turns. Empty = no dense field (the main rock and satellites instead).")]
        [SerializeField] private FieldClusterRockOption[] _planeRocks = Array.Empty<FieldClusterRockOption>();
        [Tooltip("The smallest one-sided extent of the mesh bounds of any of these rocks along any axis at size 1, over every 3D turn: rocks are sized from it so their bounds close their part of the footprint.")]
        [SerializeField, Range(0.1f, 0.5f)] private float _planeHalfExtentShare = 0.3f;
        [Tooltip("The farthest vertex of any of these rocks from its pivot at size 1.")]
        [SerializeField, Range(0.1f, 1f)] private float _planeReachShare = 0.65f;
        [Tooltip("A rock grows by up to this share on top of the size its part needs: small to big rocks.")]
        [SerializeField, Range(0f, 1f)] private float _planeSizeVariation = 0.3f;
        [Tooltip("Gap a rock may leave inside the edge of its part, meters, min and max; two neighbours leave at most the max between them, which must stay below the configured plane gap.")]
        [SerializeField] private Vector2 _planeGapMeters = new(0f, 5f);
        [Tooltip("A rock's middle moves up to this many meters sideways from the middle of its part.")]
        [SerializeField, Min(0f)] private float _planeJitterMeters = 3f;
        [Tooltip("A rock's middle moves up to this many meters up or down.")]
        [SerializeField, Min(0f)] private float _planeHeightJitterMeters = 8f;
        [Tooltip("The smallest half height of a rock's bounds above and below its middle, meters: it closes the plane slab whatever its height jitter.")]
        [SerializeField, Min(0f)] private float _planeMinHalfHeightMeters = 16f;
        [Tooltip("A rock may reach this far past the footprint, meters; a part whose rock would reach farther is split into smaller parts (down to the smallest part).")]
        [SerializeField, Min(0f)] private float _planeMaxSpillMeters = 35f;
        [Tooltip("Side of the smallest part one rock closes, meters.")]
        [SerializeField, Min(1f)] private float _planeMinPartMeters = 20f;
        [Tooltip("Extra rocks above and below the plane per footprint cell (the fraction by chance): the field's volume around a wall.")]
        [SerializeField, Min(0f)] private float _extraRocksPerCell = 0.6f;
        [Tooltip("Height of an extra rock's middle above or below the plane, meters, min and max.")]
        [SerializeField] private Vector2 _extraHeightMeters = new(20f, 120f);
        [Tooltip("Diameter of an extra rock, meters, min and max.")]
        [SerializeField] private Vector2 _extraDiameterMeters = new(15f, 70f);

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
        public float PlaneSizeVariation => _planeSizeVariation;
        public Vector2 PlaneGapMeters => _planeGapMeters;
        public float PlaneJitterMeters => _planeJitterMeters;
        public float PlaneHeightJitterMeters => _planeHeightJitterMeters;
        public float PlaneMinHalfHeightMeters => _planeMinHalfHeightMeters;
        public float PlaneMaxSpillMeters => _planeMaxSpillMeters;
        public float PlaneMinPartMeters => _planeMinPartMeters;
        public float ExtraRocksPerCell => _extraRocksPerCell;
        public Vector2 ExtraHeightMeters => _extraHeightMeters;
        public Vector2 ExtraDiameterMeters => _extraDiameterMeters;
    }
}
