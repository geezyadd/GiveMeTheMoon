using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // A preset of the obstacle field (sparse, medium, dense, giants ...): every number that makes a field easy or hard
    // to cross, each read at the field's difficulty 0..1. A new preset is a new asset in the configuration's list.
    [CreateAssetMenu(menuName = "Game/Flight Field/Difficulty Profile", fileName = "FieldDifficultyProfile")]
    public sealed class FieldDifficultyProfile : ScriptableObject {
        [Tooltip("Stable id sent with the field to every peer: never rename it once it is used.")]
        [SerializeField] private string _id;
        [Tooltip("The volume of rocks above and below the flight plane for this profile; empty = the configuration's volume.")]
        [SerializeField] private FieldVolumeConfiguration _volume;

        [Header("Density map")]
        [Tooltip("Size of the dense and thin areas, cells: the period of the low-frequency noise.")]
        [SerializeField] private FieldDifficultyRange _noiseScaleCells = new(14f, 10f);
        [Tooltip("Stretch of the noise across the course (> 1: dense areas lie across the A -> B axis, < 1: along it).")]
        [SerializeField, Min(0.1f)] private float _noiseStretchAcross = 1f;
        [Tooltip("How sharply a dense area turns into a thin one: 1 = the plain noise, higher = harder edges.")]
        [SerializeField] private FieldDifficultyRange _contrast = new(1.5f, 2f);
        [Tooltip("Large empty areas per flight.")]
        [SerializeField] private FieldDifficultyRange _voidCount = new(3f, 1f);
        [Tooltip("Radius of a large empty area, cells, min and max.")]
        [SerializeField] private Vector2 _voidRadiusCells = new(3f, 6f);

        [Header("Obstacle field")]
        [Tooltip("Debris recipes of the field, one picked by weight per field.")]
        [SerializeField] private FieldDebrisOption[] _debrisTypes = Array.Empty<FieldDebrisOption>();
        [Tooltip("Chance that a point of the debris grid holds a rock, before the density noise and the edge ramp.")]
        [SerializeField] private FieldDifficultyRange _obstacleDensity = new(0.3f, 0.45f);
        [Tooltip("How far the density noise moves the density either way, as a share of it: 0 = even everywhere.")]
        [SerializeField, Range(0f, 1f)] private float _obstacleDensityVariation = 0.6f;
        [Tooltip("Cells from the field border over which the density ramps up to the full one; 0 = no ramp.")]
        [SerializeField, Min(0)] private int _edgeRampCells;
        [Tooltip("Share of the density (debris and giants) right at the border when the ramp is on.")]
        [SerializeField, Range(0f, 1f)] private float _edgeDensityShare = 0.5f;
        [Tooltip("Size classes of the rocks on the debris grid (small, medium), laid out locally by every peer from the field seed.")]
        [SerializeField] private FieldRockSizeClass[] _debrisSizeClasses = Array.Empty<FieldRockSizeClass>();
        [Tooltip("Giant rocks per square kilometer of field the generator tries to place.")]
        [SerializeField] private FieldDifficultyRange _giantsPerSquareKilometer = new(4f, 6f);
        [Tooltip("Size classes of the giant rocks (large, massive): synced placements in the matrix.")]
        [SerializeField] private FieldGiantClass[] _giantClasses = Array.Empty<FieldGiantClass>();
        [Tooltip("Free cells kept between the footprints of two giants: at least the ship gap.")]
        [SerializeField, Min(1)] private int _giantGapCells = 1;
        [Tooltip("Random spots tried per giant before it is dropped.")]
        [SerializeField, Min(1)] private int _giantPlacementTries = 12;

        public string Id => _id;
        public bool HasOwnVolume => _volume != null;
        public FieldVolumeConfiguration Volume => _volume;
        public FieldDifficultyRange NoiseScaleCells => _noiseScaleCells;
        public float NoiseStretchAcross => _noiseStretchAcross;
        public FieldDifficultyRange Contrast => _contrast;
        public FieldDifficultyRange VoidCount => _voidCount;
        public Vector2 VoidRadiusCells => _voidRadiusCells;
        public IReadOnlyList<FieldDebrisOption> DebrisTypes => _debrisTypes;
        public FieldDifficultyRange ObstacleDensity => _obstacleDensity;
        public float ObstacleDensityVariation => _obstacleDensityVariation;
        public int EdgeRampCells => _edgeRampCells;
        public float EdgeDensityShare => _edgeDensityShare;
        public IReadOnlyList<FieldRockSizeClass> DebrisSizeClasses => _debrisSizeClasses;
        public FieldDifficultyRange GiantsPerSquareKilometer => _giantsPerSquareKilometer;
        public IReadOnlyList<FieldGiantClass> GiantClasses => _giantClasses;
        public int GiantGapCells => _giantGapCells;
        public int GiantPlacementTries => _giantPlacementTries;
    }
}
