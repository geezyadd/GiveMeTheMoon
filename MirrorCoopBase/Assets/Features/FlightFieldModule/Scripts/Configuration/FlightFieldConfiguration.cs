using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // Tuning of the flight field: frame size, empty zones, routes and difficulty profiles per loop, the ship gaps the
    // obstacles keep, the passage check, publishing and building. The structure of a field comes from its profile.
    [CreateAssetMenu(menuName = "Game/Flight Field/Configuration", fileName = "FlightFieldConfiguration_Default")]
    public sealed class FlightFieldConfiguration : ScriptableObject {
        [Serializable]
        public sealed class LoopRoutes {
            [Tooltip("The first loop this entry is used for; it holds until an entry with a larger loop.")]
            [SerializeField, Min(0)] private int _fromLoop;
            [Tooltip("One difficulty per route of the field: 0 the easiest, 1 the hardest.")]
            [SerializeField, Range(0f, 1f)] private float[] _routeDifficulties = { 0f };
            [Tooltip("The difficulty profiles a field of these loops may get, picked by weight from the field seed.")]
            [SerializeField] private ProfileOption[] _profiles = Array.Empty<ProfileOption>();

            public int FromLoop => _fromLoop;
            public IReadOnlyList<float> RouteDifficulties => _routeDifficulties;
            public IReadOnlyList<ProfileOption> Profiles => _profiles;
        }

        [Serializable]
        public sealed class ProfileOption : IFieldWeighted {
            [SerializeField] private FieldDifficultyProfile _profile;
            [SerializeField, Min(0f)] private float _weight = 1f;

            public FieldDifficultyProfile Profile => _profile;
            public float Weight => _weight;
        }

        [Header("Frame")]
        [Tooltip("Side of one field cell, meters.")]
        [SerializeField, Min(1f)] private float _cellSize = 40f;
        [Tooltip("Cells of the field behind station A.")]
        [SerializeField, Min(1)] private int _marginBehindACells = 5;
        [Tooltip("Cells of the field behind station B.")]
        [SerializeField, Min(1)] private int _marginBehindBCells = 5;
        [Tooltip("The least number of cells on each side of the A -> B axis, and the room kept beside the cruise start when the ship starts at an angle to the axis.")]
        [SerializeField, Min(1)] private int _sideMarginCells = 8;
        [Tooltip("Cells of the field across the A -> B axis; the length along it follows the flight distance. Widened when the cruise start lies far off the axis.")]
        [SerializeField, Min(1)] private int _fieldWidthCells = 61;

        [Header("Empty zones")]
        [Tooltip("Radius around station A kept empty, meters.")]
        [SerializeField, Min(0f)] private float _stationAZoneRadius = 80f;
        [Tooltip("Radius around station B kept empty for the approach, meters (the ship arrives within 70 m).")]
        [SerializeField, Min(0f)] private float _stationBZoneRadius = 100f;
        [Tooltip("Half width of the empty takeoff corridor from station A along the takeoff heading, meters.")]
        [SerializeField, Min(0f)] private float _takeoffCorridorHalfWidth = 60f;
        [Tooltip("Length of the takeoff corridor past the cruise start along the takeoff heading, meters: the autopilot keeps that heading, and the pilot needs this run-out to find B on the radar and turn (run-up plus margin).")]
        [SerializeField, Min(0f)] private float _takeoffRunOutLength = 200f;

        [Header("Routes and profiles")]
        [SerializeField] private LoopRoutes[] _routesByLoop = { new() };
        [Tooltip("Every difficulty profile a field may get; a peer resolves the profile id of a field here.")]
        [SerializeField] private FieldDifficultyProfile[] _difficultyProfiles = Array.Empty<FieldDifficultyProfile>();
        [Tooltip("The visual-only volume of big rocks above and below the flight plane.")]
        [SerializeField] private FieldVolumeConfiguration _volume;

        [Header("Ship gaps")]
        [Tooltip("Width of the ship, meters (the ship is about 10 x 19 m).")]
        [SerializeField, Min(1f)] private float _shipWidthMeters = 10f;
        [Tooltip("Room kept on top of the ship width between two debris rocks, meters.")]
        [SerializeField, Min(0f)] private float _debrisGapMarginMeters = 6f;
        [Tooltip("Spacing of the grid the rocks of passable cells are drawn on, meters: finer gives more candidate spots.")]
        [SerializeField, Min(1f)] private float _debrisGridSpacingMeters = 25f;
        [Tooltip("Speed the dodges-per-minute metric flies the main route at, m/s (the base cruise speed).")]
        [SerializeField, Min(0.1f)] private float _metricCruiseSpeed = 14f;

        [Header("Publishing")]
        [Tooltip("Placements, routes and route cells written into the live field model per batch; one placement is about 23 bytes, so a batch stays well below the 16 KB Telepathy message limit.")]
        [SerializeField, Min(1)] private int _publishEntriesPerBatch = 300;
        [Tooltip("Seconds between two publish batches; several network broadcasts long, so one Mirror delta never carries two batches.")]
        [SerializeField, Min(0.01f)] private float _publishBatchIntervalSeconds = 0.1f;

        [Header("Building")]
        [Tooltip("Local structures every peer builds per frame from the live field.")]
        [SerializeField, Min(1)] private int _structuresBuiltPerFrame = 16;
        [Tooltip("A local structure is built when its placement is within this horizontal distance of the local ship, meters.")]
        [SerializeField, Min(1f)] private float _buildRadius = 1300f;
        [Tooltip("A built local structure goes back to the pool beyond this horizontal distance of the local ship, meters; above the build radius so a structure on the edge does not flicker.")]
        [SerializeField, Min(1f)] private float _releaseRadius = 1500f;
        [Tooltip("Debris cells every peer builds per frame.")]
        [SerializeField, Min(1)] private int _debrisCellsBuiltPerFrame = 24;
        [Tooltip("The small debris of a cell is built within this horizontal distance of the viewpoint, meters.")]
        [SerializeField, Min(1f)] private float _debrisBuildRadius = 900f;
        [Tooltip("Built debris goes back to the pool beyond this horizontal distance, meters; above the debris build radius.")]
        [SerializeField, Min(1f)] private float _debrisReleaseRadius = 1050f;

        public float CellSize => _cellSize;
        public int MarginBehindACells => _marginBehindACells;
        public int MarginBehindBCells => _marginBehindBCells;
        public int SideMarginCells => _sideMarginCells;
        public int FieldWidthCells => _fieldWidthCells;
        public float StationAZoneRadius => _stationAZoneRadius;
        public float StationBZoneRadius => _stationBZoneRadius;
        public float TakeoffCorridorHalfWidth => _takeoffCorridorHalfWidth;
        public float TakeoffRunOutLength => _takeoffRunOutLength;
        public IReadOnlyList<LoopRoutes> RoutesByLoop => _routesByLoop;
        public IReadOnlyList<FieldDifficultyProfile> DifficultyProfiles => _difficultyProfiles;
        public FieldVolumeConfiguration Volume => _volume;
        public float ShipWidthMeters => _shipWidthMeters;
        public float DebrisMinGapMeters => _shipWidthMeters + _debrisGapMarginMeters;
        public float DebrisGridSpacingMeters => _debrisGridSpacingMeters;
        public float MetricCruiseSpeed => _metricCruiseSpeed;
        public int PublishEntriesPerBatch => _publishEntriesPerBatch;
        public float PublishBatchIntervalSeconds => _publishBatchIntervalSeconds;
        public int StructuresBuiltPerFrame => _structuresBuiltPerFrame;
        public float BuildRadius => _buildRadius;
        public float ReleaseRadius => _releaseRadius;
        public int DebrisCellsBuiltPerFrame => _debrisCellsBuiltPerFrame;
        public float DebrisBuildRadius => _debrisBuildRadius;
        public float DebrisReleaseRadius => _debrisReleaseRadius;
    }
}
