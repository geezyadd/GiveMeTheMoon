using Features.CameraModule.Contracts;
using Features.CharacterMovableModule.Scripts.Drift;
using UnityEngine;

namespace Features.ShipModule.Scripts.Overboard {
    // A player who leaves the ship in flight: how it drifts, where the ship takes it back, when the void takes it, how
    // a rope pulls it in and the debug actions' numbers.
    [CreateAssetMenu(
        fileName = nameof(ShipOverboardConfiguration) + "_Default",
        menuName = "Configurations/ShipModule/" + nameof(ShipOverboardConfiguration))]
    public sealed class ShipOverboardConfiguration : ScriptableObject {
        [SerializeField] private CharacterDriftSettings _drift = new CharacterDriftSettings();
        [Tooltip("The drifting owner's view sways with this slow continuous shake; the body tumbles, the horizon does not.")]
        [SerializeField] private CameraShakeProfile _driftViewSway;

        [Header("Ship volume and capture")]
        [Tooltip("How far beside the deck's walk area the ship's volume reaches, meters: inside it a player is still on the ship and a drifting one is taken back.")]
        [SerializeField, Min(0f)] private float _volumeEdgeMeters = 2.5f;
        [Tooltip("The ship's volume from this height above the deck surface, meters (negative: below the deck).")]
        [SerializeField] private float _volumeMinHeightMeters = -1.5f;
        [Tooltip("The ship's volume up to this height above the deck surface, meters.")]
        [SerializeField] private float _volumeMaxHeightMeters = 8f;
        [Tooltip("A player who just went overboard drifts at least this long before the ship can take it back, seconds.")]
        [SerializeField, Min(0f)] private float _minDriftSecondsBeforeCapture = 1f;

        [Header("Limit")]
        [Tooltip("A drifting player farther than this from the ship dies, meters.")]
        [SerializeField, Min(0f)] private float _maxDistanceMeters = 400f;
        [Tooltip("A player drifting longer than this dies, seconds.")]
        [SerializeField, Min(0f)] private float _maxDriftSeconds = 240f;

        [Header("Rope")]
        [Tooltip("A hooked player never comes in faster than this relative to the ship, meters per second, whatever the rope's pull.")]
        [SerializeField, Min(0f)] private float _maxPullSpeedMetersPerSecond = 9f;
        [Tooltip("A hooked player is pulled to this height above the deck's middle, meters.")]
        [SerializeField] private float _pullTargetHeightMeters = 2.5f;
        [Tooltip("A reported rope hit counts when it lies this close to the drifting player, meters.")]
        [SerializeField, Min(0f)] private float _hookReachMeters = 2.5f;

        [Header("Debug")]
        [Tooltip("Throw me overboard: the player is put this far beside the deck, meters.")]
        [SerializeField, Min(0f)] private float _debugThrowSideMeters = 9f;
        [Tooltip("Throw me overboard: the speed away from the ship it gets on top of the ship's, meters per second.")]
        [SerializeField, Min(0f)] private float _debugThrowSpeedMetersPerSecond = 2f;
        [Tooltip("Bring the ship: the drifting player ends up this high above the deck's middle, meters.")]
        [SerializeField] private float _debugBringHeightMeters = 3f;

        public CharacterDriftSettings Drift => _drift;
        public CameraShakeProfile DriftViewSway => _driftViewSway;
        public float VolumeEdgeMeters => _volumeEdgeMeters;
        public float VolumeMinHeightMeters => _volumeMinHeightMeters;
        public float VolumeMaxHeightMeters => _volumeMaxHeightMeters;
        public float MinDriftSecondsBeforeCapture => _minDriftSecondsBeforeCapture;
        public float MaxDistanceMeters => _maxDistanceMeters;
        public float MaxDriftSeconds => _maxDriftSeconds;
        public float MaxPullSpeedMetersPerSecond => _maxPullSpeedMetersPerSecond;
        public float PullTargetHeightMeters => _pullTargetHeightMeters;
        public float HookReachMeters => _hookReachMeters;
        public float DebugThrowSideMeters => _debugThrowSideMeters;
        public float DebugThrowSpeedMetersPerSecond => _debugThrowSpeedMetersPerSecond;
        public float DebugBringHeightMeters => _debugBringHeightMeters;
    }
}
