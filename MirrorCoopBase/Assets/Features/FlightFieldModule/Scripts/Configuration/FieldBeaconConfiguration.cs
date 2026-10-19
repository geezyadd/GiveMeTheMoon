using Features.FlightFieldModule.Scripts.Beacons;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // The beacons that show the crew the passable lanes from far away: lights on the walls beside every open main and
    // alternative route, at its entrance, every few hundred meters along it, at sharp turns and where lanes fork or
    // merge. Every peer lays them out from the live field's routes; visual only, never in a lane.
    [CreateAssetMenu(menuName = "Game/Flight Field/Beacon Configuration", fileName = "FieldBeaconConfiguration_Default")]
    public sealed class FieldBeaconConfiguration : ScriptableObject {
        [Header("Placement")]
        [Tooltip("Distance between two beacons along a lane's centerline, meters; their side alternates.")]
        [SerializeField, Min(1f)] private float _beaconSpacingMeters = 320f;
        [Tooltip("Centerline cells after the start of a main route where its entrance beacons stand, one on each side.")]
        [SerializeField, Min(0)] private int _entranceOffsetCells = 3;
        [Tooltip("A turn sharper than this, degrees (heading change over the turn window on both sides), gets a beacon on its outer side.")]
        [SerializeField, Range(1f, 180f)] private float _turnAngleDegrees = 35f;
        [Tooltip("Centerline cells before and after a point that its heading and turn are measured over.")]
        [SerializeField, Min(1)] private int _turnWindowCells = 3;
        [Tooltip("Two lanes count as one while their centerlines are within this many cells: where that ends a fork, where it starts a merge, each with a beacon on both sides.")]
        [SerializeField, Min(0)] private int _laneJoinCells = 2;
        [Tooltip("How far beyond the corridor's half width the wall beside it is searched for, cells; with no wall or void that close the side gets no beacon.")]
        [SerializeField, Min(0.5f)] private float _edgeSearchCells = 3f;
        [Tooltip("Step of the wall search, meters.")]
        [SerializeField, Min(0.5f)] private float _edgeSearchStepMeters = 5f;
        [Tooltip("How far a beacon stands inside the wall past its edge, meters.")]
        [SerializeField, Min(0f)] private float _wallInsetMeters = 4f;
        [Tooltip("Height of a beacon above the flight plane, meters.")]
        [SerializeField] private float _heightMeters = 55f;
        [Tooltip("Room a beacon keeps from the corridor of every route (main, alternative, dead end), beyond the beacon's own radius, meters.")]
        [SerializeField, Min(0f)] private float _laneClearanceMeters = 8f;
        [Tooltip("Radius of the beacon's look, meters: the room it takes.")]
        [SerializeField, Min(0f)] private float _beaconRadiusMeters = 15f;
        [Tooltip("The least distance between two beacons, meters: a later one closer to a kept one is dropped.")]
        [SerializeField, Min(0f)] private float _minBeaconGapMeters = 120f;

        [Header("Look")]
        [Tooltip("The beacon prefab: emissive light parts, no collider.")]
        [SerializeField] private FieldBeaconVisual _prefab;
        [Tooltip("Colour of the beacons of the main route (HDR: above 1 glows).")]
        [SerializeField, ColorUsage(false, true)] private Color _mainColor = new(0.25f, 2.2f, 0.5f, 1f);
        [Tooltip("Colour of the beacons of the other channels and the alternatives (HDR: above 1 glows).")]
        [SerializeField, ColorUsage(false, true)] private Color _alternativeColor = new(2.4f, 0.9f, 0.15f, 1f);
        [Tooltip("Seconds of one blink: bright, dim and bright again.")]
        [SerializeField, Min(0.1f)] private float _blinkPeriodSeconds = 2.4f;
        [Tooltip("Brightness of the colour at the dim and at the bright point of a blink.")]
        [SerializeField] private Vector2 _blinkIntensity = new(0.35f, 1.6f);

        public float BeaconSpacingMeters => _beaconSpacingMeters;
        public int EntranceOffsetCells => _entranceOffsetCells;
        public float TurnAngleDegrees => _turnAngleDegrees;
        public int TurnWindowCells => _turnWindowCells;
        public int LaneJoinCells => _laneJoinCells;
        public float EdgeSearchCells => _edgeSearchCells;
        public float EdgeSearchStepMeters => _edgeSearchStepMeters;
        public float WallInsetMeters => _wallInsetMeters;
        public float HeightMeters => _heightMeters;
        public float LaneClearanceMeters => _laneClearanceMeters;
        public float BeaconRadiusMeters => _beaconRadiusMeters;
        public float MinBeaconGapMeters => _minBeaconGapMeters;
        public FieldBeaconVisual Prefab => _prefab;
        public Color MainColor => _mainColor;
        public Color AlternativeColor => _alternativeColor;
        public float BlinkPeriodSeconds => _blinkPeriodSeconds;
        public Vector2 BlinkIntensity => _blinkIntensity;
    }
}
