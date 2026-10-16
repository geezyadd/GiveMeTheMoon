using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // A preset of the field's structure (Open, Belt, Canyon, Maze, ...): every number that makes a field easy or hard
    // to cross, each read at the field's difficulty 0..1. A new preset is a new asset in the configuration's list.
    [CreateAssetMenu(menuName = "Game/Flight Field/Difficulty Profile", fileName = "FieldDifficultyProfile")]
    public sealed class FieldDifficultyProfile : ScriptableObject {
        [Tooltip("Stable id sent with the field to every peer: never rename it once it is used.")]
        [SerializeField] private string _id;

        [Header("Zone shares (of all field cells)")]
        [Tooltip("Share of cells closed by giant rocks: the border wall, ridges, corridor shoulders.")]
        [SerializeField] private FieldDifficultyRange _blockedShare = new(0.25f, 0.3f);
        [Tooltip("Share of cells with small debris: corridors and glades.")]
        [SerializeField] private FieldDifficultyRange _debrisShare = new(0.15f, 0.2f);

        [Header("Density map")]
        [Tooltip("Size of ridges and gaps, cells: the period of the low-frequency noise.")]
        [SerializeField] private FieldDifficultyRange _noiseScaleCells = new(14f, 10f);
        [Tooltip("Stretch of the noise across the course (> 1: ridges lie across the A -> B axis, < 1: along it).")]
        [SerializeField, Min(0.1f)] private float _noiseStretchAcross = 1f;
        [Tooltip("How sharply a ridge turns into a gap: 1 = the plain noise, higher = harder edges.")]
        [SerializeField] private FieldDifficultyRange _contrast = new(1.5f, 2f);
        [Tooltip("A cell whose density is at least this is always closed, even past the blocked share.")]
        [SerializeField] private FieldDifficultyRange _ridgeThreshold = new(0.95f, 0.9f);
        [Tooltip("Glade layer: a cell whose glade noise is below this is a glade (never closed, light debris).")]
        [SerializeField] private FieldDifficultyRange _gladeThreshold = new(0.25f, 0.15f);
        [Tooltip("Size of the glades, cells.")]
        [SerializeField, Min(1f)] private float _gladeNoiseScaleCells = 8f;
        [Tooltip("Debris density (0..1) in glades.")]
        [SerializeField] private FieldDifficultyRange _gladeDebrisDensity = new(0.2f, 0.4f);
        [Tooltip("Large empty areas per flight.")]
        [SerializeField] private FieldDifficultyRange _voidCount = new(3f, 1f);
        [Tooltip("Radius of a large empty area, cells, min and max.")]
        [SerializeField] private Vector2 _voidRadiusCells = new(3f, 6f);

        [Header("Main route")]
        [Tooltip("Cells on each side of the main centerline; the corridor is 2 * value + 1 cells wide.")]
        [SerializeField] private FieldDifficultyRange _corridorHalfWidthCells = new(2f, 1f);
        [Tooltip("How much the half width changes along the corridor, cells either way.")]
        [SerializeField] private FieldDifficultyRange _widthVariationCells = new(1f, 1f);
        [Tooltip("Cells along the corridor over which its width changes.")]
        [SerializeField, Min(1f)] private float _widthNoiseScaleCells = 12f;
        [Tooltip("Sideways swing of the winding corridor, cells (two sines, the second half as long, 75/25).")]
        [SerializeField] private FieldDifficultyRange _meanderAmplitudeCells = new(8f, 8f);
        [Tooltip("Length of one full swing of the main sine, cells.")]
        [SerializeField] private FieldDifficultyRange _meanderPeriodCells = new(20f, 20f);
        [Tooltip("Cells near each station over which the swing fades in and out.")]
        [SerializeField, Min(0f)] private float _meanderFadeCells = 8f;
        [Tooltip("Debris density (0..1) inside the main corridor.")]
        [SerializeField] private FieldDifficultyRange _corridorDebrisDensity = new(0.15f, 0.5f);
        [Tooltip("Cells beside a corridor whose density rises: the shoulder of rocks along it.")]
        [SerializeField] private FieldDifficultyRange _shoulderWidthCells = new(2f, 2f);
        [Tooltip("How much the shoulder adds to the density right at the corridor edge.")]
        [SerializeField] private FieldDifficultyRange _shoulderStrength = new(0.3f, 0.5f);

        [Header("Alternative routes")]
        [Tooltip("Branches per main route that leave it and come back.")]
        [SerializeField] private FieldDifficultyRange _alternativeRoutes = new(2f, 2f);
        [SerializeField] private FieldDifficultyRange _alternativeHalfWidthCells = new(1f, 1f);
        [Tooltip("How far an alternative swings away from the main route, cells.")]
        [SerializeField] private FieldDifficultyRange _alternativeOffsetCells = new(8f, 10f);
        [Tooltip("Main centerline cells an alternative spans, min and max.")]
        [SerializeField] private Vector2 _alternativeLengthCells = new(20f, 40f);
        [SerializeField] private FieldDifficultyRange _alternativeDebrisDensity = new(0.3f, 0.7f);
        [Tooltip("Chance that an alternative is closed by a ridge in its middle.")]
        [SerializeField] private FieldDifficultyRange _alternativeClosedChance = new(0f, 0.5f);

        [Header("Dead ends")]
        [Tooltip("Branches per main route that end in a ridge.")]
        [SerializeField] private FieldDifficultyRange _deadEnds = new(2f, 2f);
        [SerializeField] private FieldDifficultyRange _deadEndHalfWidthCells = new(1f, 1f);
        [Tooltip("Length of a dead end, cells, min and max.")]
        [SerializeField] private Vector2 _deadEndLengthCells = new(8f, 16f);
        [Tooltip("Cells of ridge around the tip of a dead end.")]
        [SerializeField, Min(1)] private int _deadEndCapCells = 2;

        [Header("Edge")]
        [Tooltip("Thickness of the border wall, cells.")]
        [SerializeField, Min(1)] private int _wallThicknessCells = 3;

        [Header("Path matters")]
        [Tooltip("The shortest way outside every corridor must be at least this many times longer than the way along the route.")]
        [SerializeField] private FieldDifficultyRange _minFreeToRouteRatio = new(1.3f, 1.8f);
        [Tooltip("No way to B outside the corridors may exist at all.")]
        [SerializeField] private bool _requiresNoFreePath;
        [Tooltip("Rounds of closing the free way with ridges before the field is taken as it is.")]
        [SerializeField, Min(0)] private int _maxPathRepairRounds = 6;
        [Tooltip("Radius of a ridge a repair round puts on the free way, cells.")]
        [SerializeField, Min(0)] private int _pathRepairRadiusCells = 2;

        [Header("Obstacle types per zone")]
        [Tooltip("Giant rocks of the border wall; one option must take a single cell, so the wall has no holes.")]
        [SerializeField] private FieldObstacleOption[] _wallObstacles = Array.Empty<FieldObstacleOption>();
        [Tooltip("Giant rocks of ridges, shoulders and caps; the largest footprints are laid first, one option must take a single cell.")]
        [SerializeField] private FieldObstacleOption[] _ridgeObstacles = Array.Empty<FieldObstacleOption>();
        [Tooltip("Small debris of corridors and glades.")]
        [SerializeField] private FieldDebrisOption[] _debrisTypes = Array.Empty<FieldDebrisOption>();

        public string Id => _id;
        public FieldDifficultyRange BlockedShare => _blockedShare;
        public FieldDifficultyRange DebrisShare => _debrisShare;
        public FieldDifficultyRange NoiseScaleCells => _noiseScaleCells;
        public float NoiseStretchAcross => _noiseStretchAcross;
        public FieldDifficultyRange Contrast => _contrast;
        public FieldDifficultyRange RidgeThreshold => _ridgeThreshold;
        public FieldDifficultyRange GladeThreshold => _gladeThreshold;
        public float GladeNoiseScaleCells => _gladeNoiseScaleCells;
        public FieldDifficultyRange GladeDebrisDensity => _gladeDebrisDensity;
        public FieldDifficultyRange VoidCount => _voidCount;
        public Vector2 VoidRadiusCells => _voidRadiusCells;
        public FieldDifficultyRange CorridorHalfWidthCells => _corridorHalfWidthCells;
        public FieldDifficultyRange WidthVariationCells => _widthVariationCells;
        public float WidthNoiseScaleCells => _widthNoiseScaleCells;
        public FieldDifficultyRange MeanderAmplitudeCells => _meanderAmplitudeCells;
        public FieldDifficultyRange MeanderPeriodCells => _meanderPeriodCells;
        public float MeanderFadeCells => _meanderFadeCells;
        public FieldDifficultyRange CorridorDebrisDensity => _corridorDebrisDensity;
        public FieldDifficultyRange ShoulderWidthCells => _shoulderWidthCells;
        public FieldDifficultyRange ShoulderStrength => _shoulderStrength;
        public FieldDifficultyRange AlternativeRoutes => _alternativeRoutes;
        public FieldDifficultyRange AlternativeHalfWidthCells => _alternativeHalfWidthCells;
        public FieldDifficultyRange AlternativeOffsetCells => _alternativeOffsetCells;
        public Vector2 AlternativeLengthCells => _alternativeLengthCells;
        public FieldDifficultyRange AlternativeDebrisDensity => _alternativeDebrisDensity;
        public FieldDifficultyRange AlternativeClosedChance => _alternativeClosedChance;
        public FieldDifficultyRange DeadEnds => _deadEnds;
        public FieldDifficultyRange DeadEndHalfWidthCells => _deadEndHalfWidthCells;
        public Vector2 DeadEndLengthCells => _deadEndLengthCells;
        public int DeadEndCapCells => _deadEndCapCells;
        public int WallThicknessCells => _wallThicknessCells;
        public FieldDifficultyRange MinFreeToRouteRatio => _minFreeToRouteRatio;
        public bool RequiresNoFreePath => _requiresNoFreePath;
        public int MaxPathRepairRounds => _maxPathRepairRounds;
        public int PathRepairRadiusCells => _pathRepairRadiusCells;
        public IReadOnlyList<FieldObstacleOption> WallObstacles => _wallObstacles;
        public IReadOnlyList<FieldObstacleOption> RidgeObstacles => _ridgeObstacles;
        public IReadOnlyList<FieldDebrisOption> DebrisTypes => _debrisTypes;
    }
}
