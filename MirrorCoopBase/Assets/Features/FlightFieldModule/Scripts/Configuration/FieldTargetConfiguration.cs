using Features.FlightFieldModule.Scripts.Targets;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // How many target balloons a flight field gets and where (passable cells, clear of rocks, more of them away from
    // the main course), how a weapon hit on one is checked and how they bob, squash and pop (every peer builds them
    // within the field's own build radius, IFieldStreamingRule). The kinds are in the FieldTargetCatalog; a stage preset may scale the count
    // (FieldTargetPresetBlock).
    [CreateAssetMenu(menuName = "Game/Flight Field/Target Configuration", fileName = "FieldTargetConfiguration_Default")]
    public sealed class FieldTargetConfiguration : ScriptableObject {
        [Header("Generation")]
        [Tooltip("Balloons per flight on loop 0: a random count in this range, both ends included.")]
        [SerializeField] private Vector2Int _firstLoopCount = new(6, 10);
        [Tooltip("Balloons added per loop after the first.")]
        [SerializeField, Min(0f)] private float _countPerLoop = 1.5f;
        [Tooltip("At most this many balloons per flight, after the stage preset's scale.")]
        [SerializeField, Min(0)] private int _maxCount = 30;
        [Tooltip("How far below (x) and above (y) the flight plane a balloon's middle stands, meters.")]
        [SerializeField] private Vector2 _heightMeters = new(-3f, 6f);
        [Tooltip("Free space between a balloon's sphere and the nearest rock, meters.")]
        [SerializeField, Min(0f)] private float _rockClearanceMeters = 8f;
        [Tooltip("Two balloons stand at least this far apart, meters.")]
        [SerializeField, Min(0f)] private float _minSpacingMeters = 60f;
        [Tooltip("A balloon stands at most this far from the main course, meters.")]
        [SerializeField, Min(1f)] private float _maxCourseDistanceMeters = 450f;
        [Tooltip("How much more often a cell at the largest distance from the main course is picked than one on it (weight 1 + this times the distance share).")]
        [SerializeField, Min(0f)] private float _awayFromCourseWeight = 3f;
        [Tooltip("Tries to find a free spot per balloon before the generator gives that balloon up.")]
        [SerializeField, Min(1)] private int _placementTriesPerTarget = 20;

        [Header("Weapon hits")]
        [Tooltip("How far, meters, a reported hit may lie outside the balloon's sphere and still count (aim and network delay).")]
        [SerializeField, Min(0f)] private float _hitPointToleranceMeters = 3f;

        [Header("Feel")]
        [Tooltip("How far a balloon bobs up and down, meters (look only, its data stays put).")]
        [SerializeField, Min(0f)] private float _bobAmplitudeMeters = 0.6f;
        [Tooltip("One bob up and down, seconds.")]
        [SerializeField, Min(0.1f)] private float _bobPeriodSeconds = 3.2f;
        [Tooltip("How long a struck balloon squashes after a hit, seconds.")]
        [SerializeField, Min(0.01f)] private float _hitFeedbackSeconds = 0.12f;
        [Tooltip("How much a struck balloon squashes at the start of the feedback, as a share of its size.")]
        [SerializeField, Range(0f, 0.5f)] private float _hitSquashShare = 0.15f;
        [Tooltip("Played where a balloon pops, on every peer: confetti and scraps in the kind's colour, coins to the ship, the '+N' text, the pop and coin sound hooks.")]
        [SerializeField] private FieldTargetPopEffect _popEffect;
        [SerializeField] private FieldTargetPopFeel _popFeel = new() {
            CoinCount = 8,
            CoinBurstSpeed = 7f,
            CoinRings = 3,
            CoinRingSpreadDegrees = 60f,
            CoinShipHeightMeters = 2f,
            CoinBurstSeconds = 0.35f,
            CoinFlySeconds = 0.8f,
            RewardTextStartHeightShare = 1.2f,
            RewardTextRiseMeters = 5f,
            RewardTextSeconds = 1.6f,
            RewardTextSizeMeters = 1.6f,
        };

        public Vector2Int FirstLoopCount => _firstLoopCount;
        public float CountPerLoop => _countPerLoop;
        public int MaxCount => _maxCount;
        public Vector2 HeightMeters => _heightMeters;
        public float RockClearanceMeters => _rockClearanceMeters;
        public float MinSpacingMeters => _minSpacingMeters;
        public float MaxCourseDistanceMeters => _maxCourseDistanceMeters;
        public float AwayFromCourseWeight => _awayFromCourseWeight;
        public int PlacementTriesPerTarget => _placementTriesPerTarget;
        public float HitPointToleranceMeters => _hitPointToleranceMeters;
        public float BobAmplitudeMeters => _bobAmplitudeMeters;
        public float BobPeriodSeconds => _bobPeriodSeconds;
        public float HitFeedbackSeconds => _hitFeedbackSeconds;
        public float HitSquashShare => _hitSquashShare;
        public FieldTargetPopEffect PopEffect => _popEffect;
        public FieldTargetPopFeel PopFeel => _popFeel;
    }
}
