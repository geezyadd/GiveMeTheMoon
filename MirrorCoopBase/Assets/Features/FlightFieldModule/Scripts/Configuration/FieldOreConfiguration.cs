using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // How the ore of broken rocks moves and looks: the drift away from the break, the fade before it is gone, where a
    // rope pulls it to on the ship, how far a rope hit may lie off it and the debug drop. What drops from which rock
    // is in FieldRockDurabilityConfiguration; the kinds are in FieldOreCatalog.
    [CreateAssetMenu(menuName = "Game/Flight Field/Ore Configuration", fileName = "FieldOreConfiguration_Default")]
    public sealed class FieldOreConfiguration : ScriptableObject {
        [Tooltip("Slowest drift of a dropped piece away from the break point, meters per second.")]
        [SerializeField, Min(0f)] private float _minDriftSpeedMetersPerSecond = 0.6f;
        [Tooltip("Fastest drift of a dropped piece away from the break point, meters per second.")]
        [SerializeField, Min(0f)] private float _maxDriftSpeedMetersPerSecond = 1.6f;
        [Tooltip("How far from the rock's middle a piece appears, as a share of the rock's radius.")]
        [SerializeField, Range(0f, 1f)] private float _dropScatterShare = 0.4f;
        [Tooltip("The last seconds of a piece's lifetime it fades out.")]
        [SerializeField, Min(0f)] private float _fadeSeconds = 4f;
        [Tooltip("How fast a piece turns while it drifts, degrees per second.")]
        [SerializeField, Min(0f)] private float _spinDegreesPerSecond = 40f;
        [Tooltip("A hooked piece is pulled to this point above the ship's middle, meters.")]
        [SerializeField, Min(0f)] private float _pullTargetHeightMeters = 1.5f;
        [Tooltip("How far, meters, a reported rope hit may lie outside the piece's sphere and still count (aim and network delay).")]
        [SerializeField, Min(0f)] private float _hitPointToleranceMeters = 3f;
        [Tooltip("Debug: how many pieces the debug window's drop puts before the ship.")]
        [SerializeField, Min(1)] private int _debugDropCount = 3;
        [Tooltip("Debug: how far ahead of the ship along the field the debug drop puts them, meters.")]
        [SerializeField, Min(0f)] private float _debugDropDistanceMeters = 30f;
        [Tooltip("Debug: how high above the ship the debug drop puts them, meters.")]
        [SerializeField] private float _debugDropHeightMeters = 3f;

        public float MinDriftSpeedMetersPerSecond => _minDriftSpeedMetersPerSecond;
        public float MaxDriftSpeedMetersPerSecond => _maxDriftSpeedMetersPerSecond;
        public float DropScatterShare => _dropScatterShare;
        public float FadeSeconds => _fadeSeconds;
        public float SpinDegreesPerSecond => _spinDegreesPerSecond;
        public float PullTargetHeightMeters => _pullTargetHeightMeters;
        public float HitPointToleranceMeters => _hitPointToleranceMeters;
        public int DebugDropCount => _debugDropCount;
        public float DebugDropDistanceMeters => _debugDropDistanceMeters;
        public float DebugDropHeightMeters => _debugDropHeightMeters;
    }
}
