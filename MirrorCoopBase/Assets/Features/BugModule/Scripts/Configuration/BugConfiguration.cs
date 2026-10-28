using Features.BugModule.Scripts.View;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.SonarModule.Contracts;
using UnityEngine;

namespace Features.BugModule.Scripts.Configuration {
    // The basic bug: which field agent kind it flies as, how it lives and dies, how it comes at the ship and gnaws it,
    // and when it appears. Its flight limits and its look are the kind's FieldAgentProfile.
    [CreateAssetMenu(fileName = nameof(BugConfiguration) + "_Default", menuName = "Configurations/Bug/" + nameof(BugConfiguration))]
    public sealed class BugConfiguration : ScriptableObject {
        [Tooltip("The bug's field agent kind; it must be in FieldAgentConfiguration.Profiles.")]
        [SerializeField] private FieldAgentProfile _profile;
        [SerializeField] private SonarContactKind _sonarKind;
        [SerializeField] private BugDeathEffect _deathEffectPrefab;

        [Header("Life")]
        [Tooltip("Hit points: a weapon hit takes its damage per hit (pistol 12, minigun 1).")]
        [SerializeField, Min(0.1f)] private float _maxHealth = 10f;

        [Header("Approach")]
        [Tooltip("The bugs of a group first gather on a ring round the ship this far from it, each at its own side, metres.")]
        [SerializeField, Min(1f)] private float _approachRingMeters = 22f;
        [Tooltip("The ring's height over the ship, metres.")]
        [SerializeField] private float _approachHeightMeters = 5f;
        [Tooltip("How close to its ring point counts as there, metres.")]
        [SerializeField, Min(0.1f)] private float _approachArriveMeters = 4f;
        [Tooltip("The ring points of a group spread over this arc round the ship, centred on the side the group comes from, degrees: the group parts and comes at the ship from different sides.")]
        [SerializeField, Range(0f, 360f)] private float _approachSpreadDegrees = 200f;

        [Header("Darting flight")]
        [Tooltip("One jerk - freeze - dash cycle, seconds; each bug starts at its own phase.")]
        [SerializeField, Min(0.1f)] private float _dartCycleSeconds = 1.5f;
        [Tooltip("The share of the cycle the bug nearly hangs still.")]
        [SerializeField, Range(0f, 0.9f)] private float _freezeShare = 0.3f;
        [Tooltip("The share of the top speed while it hangs still.")]
        [SerializeField, Range(0f, 1f)] private float _freezeSpeedShare = 0.08f;
        [Tooltip("Every dash throws the goal this far sideways, so the bug darts off its line, metres.")]
        [SerializeField, Min(0f)] private float _dashSidewaysMeters = 5f;
        [Tooltip("The small loops on top of the flight: their radius, metres.")]
        [SerializeField, Min(0f)] private float _loopMeters = 2.5f;
        [Tooltip("One small loop, seconds.")]
        [SerializeField, Min(0.1f)] private float _loopSeconds = 0.9f;
        [Tooltip("The steering aims this far ahead on the way to the goal, so the dashes and loops show at any distance, metres.")]
        [SerializeField, Min(1f)] private float _dartLookAheadMeters = 10f;
        [Tooltip("Closer than this to its goal the bug stops darting and flies straight in, metres.")]
        [SerializeField, Min(0f)] private float _calmNearGoalMeters = 6f;

        [Header("Gnawing")]
        [Tooltip("Seconds between two bites of a bug sitting on the ship.")]
        [SerializeField, Min(0.1f)] private float _biteSeconds = 2f;
        [Tooltip("Hull HP one bite takes (cause Gnaw).")]
        [SerializeField, Min(0f)] private float _biteHull = 0.75f;
        [Tooltip("One bite as a ShipDamageModule impact of this strength (0..1): the impact rules decide whether the module beside it breaks or a breach opens.")]
        [SerializeField, Range(0f, 1f)] private float _biteStrength = 0.05f;
        [Tooltip("How close to its gnaw point the bug lands, metres.")]
        [SerializeField, Min(0.1f)] private float _gnawArriveMeters = 1.2f;

        [Header("Cargo")]
        [Tooltip("How close to a cargo on board the bug grabs it, metres.")]
        [SerializeField, Min(0.1f)] private float _cargoGrabMeters = 1.5f;
        [Tooltip("The share of the top speed a bug carrying a cargo flies with.")]
        [SerializeField, Range(0.05f, 1f)] private float _carrySpeedShare = 0.6f;
        [Tooltip("A bug with a cargo flies this far away from the ship and is gone, metres.")]
        [SerializeField, Min(1f)] private float _carryAwayMeters = 400f;
        [Tooltip("How close to the carry-away point counts as gone, metres.")]
        [SerializeField, Min(0.1f)] private float _carryArriveMeters = 10f;

        [Header("Leaving")]
        [Tooltip("When the ship no longer flies the bugs fly off this far and are gone, metres.")]
        [SerializeField, Min(1f)] private float _fleeMeters = 300f;

        [Header("Random spawns in flight")]
        [SerializeField] private bool _isRandomSpawnEnabled = true;
        [Tooltip("Seconds between two random spawn rolls while the ship flies.")]
        [SerializeField, Min(1f)] private float _randomSpawnIntervalSeconds = 60f;
        [Tooltip("The chance of a group on one roll.")]
        [SerializeField, Range(0f, 1f)] private float _randomSpawnChance = 0.1f;
        [Tooltip("The roll's chance is multiplied by this while a cargo is on board.")]
        [SerializeField, Min(0f)] private float _cargoSpawnChanceMultiplier = 3f;
        [SerializeField, Min(1)] private int _groupMinSize = 3;
        [SerializeField, Min(1)] private int _groupMaxSize = 5;

        [Header("Debug")]
        [SerializeField, Min(1)] private int _debugGroupSize = 4;

        public FieldAgentProfile Profile => _profile;
        public SonarContactKind SonarKind => _sonarKind;
        public BugDeathEffect DeathEffectPrefab => _deathEffectPrefab;
        public float MaxHealth => _maxHealth;
        public float ApproachRingMeters => _approachRingMeters;
        public float ApproachHeightMeters => _approachHeightMeters;
        public float ApproachArriveMeters => _approachArriveMeters;
        public float ApproachSpreadDegrees => _approachSpreadDegrees;
        public float DartCycleSeconds => _dartCycleSeconds;
        public float FreezeShare => _freezeShare;
        public float FreezeSpeedShare => _freezeSpeedShare;
        public float DashSidewaysMeters => _dashSidewaysMeters;
        public float LoopMeters => _loopMeters;
        public float LoopSeconds => _loopSeconds;
        public float CalmNearGoalMeters => _calmNearGoalMeters;
        public float DartLookAheadMeters => _dartLookAheadMeters;
        public float BiteSeconds => _biteSeconds;
        public float BiteHull => _biteHull;
        public float BiteStrength => _biteStrength;
        public float GnawArriveMeters => _gnawArriveMeters;
        public float CargoGrabMeters => _cargoGrabMeters;
        public float CarrySpeedShare => _carrySpeedShare;
        public float CarryAwayMeters => _carryAwayMeters;
        public float CarryArriveMeters => _carryArriveMeters;
        public float FleeMeters => _fleeMeters;
        public bool IsRandomSpawnEnabled => _isRandomSpawnEnabled;
        public float RandomSpawnIntervalSeconds => _randomSpawnIntervalSeconds;
        public float RandomSpawnChance => _randomSpawnChance;
        public float CargoSpawnChanceMultiplier => _cargoSpawnChanceMultiplier;
        public int GroupMinSize => _groupMinSize;
        public int GroupMaxSize => _groupMaxSize;
        public int DebugGroupSize => _debugGroupSize;
    }
}
