using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // Tuning of the flight field: frame size, empty zones, routes per loop, obstacle density and the passage check.
    [CreateAssetMenu(menuName = "Game/Flight Field/Configuration", fileName = "FlightFieldConfiguration_Default")]
    public sealed class FlightFieldConfiguration : ScriptableObject {
        [Serializable]
        public sealed class LoopRoutes {
            [Tooltip("The first loop this entry is used for; it holds until an entry with a larger loop.")]
            [SerializeField, Min(0)] private int _fromLoop;
            [Tooltip("One difficulty per route of the field: 0 the easiest, 1 the hardest.")]
            [SerializeField, Range(0f, 1f)] private float[] _routeDifficulties = { 0f };

            public int FromLoop => _fromLoop;
            public IReadOnlyList<float> RouteDifficulties => _routeDifficulties;
        }

        [Serializable]
        public sealed class RouteShapeCurves {
            [Tooltip("Difficulty (0..1) -> cells on each side of the route centerline; the corridor is 2 * value + 1 cells wide.")]
            [SerializeField] private AnimationCurve _corridorHalfWidthCells = AnimationCurve.Linear(0f, 2f, 1f, 1f);
            [Tooltip("Difficulty (0..1) -> how far (cells) a waypoint of the centerline may move sideways.")]
            [SerializeField] private AnimationCurve _maxSideOffsetCells = AnimationCurve.Linear(0f, 1f, 1f, 4f);
            [Tooltip("Difficulty (0..1) -> share (0..1) of free corridor cells filled with obstacles.")]
            [SerializeField] private AnimationCurve _corridorObstacleShare = AnimationCurve.Constant(0f, 1f, 0f);

            public AnimationCurve CorridorHalfWidthCells => _corridorHalfWidthCells;
            public AnimationCurve MaxSideOffsetCells => _maxSideOffsetCells;
            public AnimationCurve CorridorObstacleShare => _corridorObstacleShare;
        }

        [Header("Frame")]
        [Tooltip("Side of one field cell, meters.")]
        [SerializeField, Min(1f)] private float _cellSize = 40f;
        [Tooltip("Cells of the field behind station A.")]
        [SerializeField, Min(1)] private int _marginBehindACells = 5;
        [Tooltip("Cells of the field behind station B.")]
        [SerializeField, Min(1)] private int _marginBehindBCells = 5;
        [Tooltip("The least number of cells on each side of the A -> B axis; a short flight still gets a wide field.")]
        [SerializeField, Min(1)] private int _sideMarginCells = 8;

        [Header("Empty zones")]
        [Tooltip("Radius around station A kept empty, meters.")]
        [SerializeField, Min(0f)] private float _stationAZoneRadius = 80f;
        [Tooltip("Radius around station B kept empty for the approach, meters (the ship arrives within 70 m).")]
        [SerializeField, Min(0f)] private float _stationBZoneRadius = 100f;
        [Tooltip("Half width of the empty takeoff corridor from station A to the cruise start, meters.")]
        [SerializeField, Min(0f)] private float _takeoffCorridorHalfWidth = 60f;

        [Header("Routes")]
        [SerializeField] private LoopRoutes[] _routesByLoop = { new() };
        [SerializeField] private RouteShapeCurves _routeShape = new();
        [Tooltip("Cells between two random waypoints of a route centerline.")]
        [SerializeField, Min(1)] private int _waypointSpacingCells = 4;

        [Header("Content")]
        [Tooltip("Share (0..1) of free cells outside the reserved zones covered by obstacles.")]
        [SerializeField, Range(0f, 1f)] private float _obstacleDensity = 0.3f;
        [Tooltip("Structures of the border wall, tried in this order for every border cell; the last one has to fit a single cell.")]
        [SerializeField] private FieldStructureDefinition[] _wallStructures = Array.Empty<FieldStructureDefinition>();
        [Tooltip("Random spots tried for one structure before it is dropped.")]
        [SerializeField, Min(1)] private int _placementTries = 12;

        [Header("Passage")]
        [Tooltip("Free cells needed on every side of a passage cell, diagonals included (the ship is about 10 x 19 m).")]
        [SerializeField, Min(0)] private int _passageClearanceCells = 1;
        [Tooltip("Rounds of removing the structures that block a route before the field is generated with another seed.")]
        [SerializeField, Min(0)] private int _maxRepairRounds = 2;
        [Tooltip("Seeds tried before the route corridors are cleared by force.")]
        [SerializeField, Min(1)] private int _maxSeedAttempts = 3;

        public float CellSize => _cellSize;
        public int MarginBehindACells => _marginBehindACells;
        public int MarginBehindBCells => _marginBehindBCells;
        public int SideMarginCells => _sideMarginCells;
        public float StationAZoneRadius => _stationAZoneRadius;
        public float StationBZoneRadius => _stationBZoneRadius;
        public float TakeoffCorridorHalfWidth => _takeoffCorridorHalfWidth;
        public IReadOnlyList<LoopRoutes> RoutesByLoop => _routesByLoop;
        public RouteShapeCurves RouteShape => _routeShape;
        public int WaypointSpacingCells => _waypointSpacingCells;
        public float ObstacleDensity => _obstacleDensity;
        public IReadOnlyList<FieldStructureDefinition> WallStructures => _wallStructures;
        public int PlacementTries => _placementTries;
        public int PassageClearanceCells => _passageClearanceCells;
        public int MaxRepairRounds => _maxRepairRounds;
        public int MaxSeedAttempts => _maxSeedAttempts;
    }
}
