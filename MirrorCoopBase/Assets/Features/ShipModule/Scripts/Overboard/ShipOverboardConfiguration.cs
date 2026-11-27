using Features.CharacterMovableModule.Scripts.Drift;
using UnityEngine;

namespace Features.ShipModule.Scripts.Overboard {
    // A player who leaves the ship in flight: how it drifts, where the ship takes it back and when the void takes it.
    [CreateAssetMenu(
        fileName = nameof(ShipOverboardConfiguration) + "_Default",
        menuName = "Configurations/ShipModule/" + nameof(ShipOverboardConfiguration))]
    public sealed class ShipOverboardConfiguration : ScriptableObject {
        [SerializeField] private CharacterDriftSettings _drift = new CharacterDriftSettings();

        [Header("Ship volume and capture")]
        [Tooltip("How far beside the deck's walk area the ship's volume reaches, meters: a player who rode the ship and leaves it goes overboard.")]
        [SerializeField, Min(0f)] private float _volumeEdgeMeters = 2.5f;
        [Tooltip("The ship's volume from this height above the deck surface, meters (negative: below the deck).")]
        [SerializeField] private float _volumeMinHeightMeters = -1.5f;
        [Tooltip("The ship's volume up to this height above the deck surface, meters.")]
        [SerializeField] private float _volumeMaxHeightMeters = 8f;
        [Tooltip("A player who just went overboard drifts at least this long before the ship can catch it over the deck, seconds.")]
        [SerializeField, Min(0f)] private float _minDriftSecondsBeforeCapture = 1f;

        [Header("Limit")]
        [Tooltip("A drifting player farther than this from the ship dies, meters.")]
        [SerializeField, Min(0f)] private float _maxDistanceMeters = 400f;
        [Tooltip("A player drifting longer than this dies, seconds.")]
        [SerializeField, Min(0f)] private float _maxDriftSeconds = 240f;

        [Header("Areas on the ground (lobby, stations)")]
        [Tooltip("A player drifting farther than this from the nearest overboard area is put back at its return point, meters.")]
        [SerializeField, Min(0f)] private float _areaMaxDistanceMeters = 100f;
        [Tooltip("A player drifting longer than this away from the areas is put back at the return point, seconds.")]
        [SerializeField, Min(0f)] private float _areaMaxDriftSeconds = 60f;

        [Header("Loose items")]
        [Tooltip("Linear damping of a loose item drifting outside the safe space: its speed fades to a slow drift.")]
        [SerializeField, Min(0f)] private float _itemDriftLinearDamping = 0.6f;
        [Tooltip("Angular damping of a drifting item: its spin fades.")]
        [SerializeField, Min(0f)] private float _itemDriftAngularDamping = 0.4f;
        [Tooltip("A drifting item farther than this from the ship and the nearest overboard area is removed, meters.")]
        [SerializeField, Min(0f)] private float _itemMaxDistanceMeters = 100f;

        public CharacterDriftSettings Drift => _drift;
        public float ItemDriftLinearDamping => _itemDriftLinearDamping;
        public float ItemDriftAngularDamping => _itemDriftAngularDamping;
        public float ItemMaxDistanceMeters => _itemMaxDistanceMeters;
        public float AreaMaxDistanceMeters => _areaMaxDistanceMeters;
        public float AreaMaxDriftSeconds => _areaMaxDriftSeconds;
        public float VolumeEdgeMeters => _volumeEdgeMeters;
        public float VolumeMinHeightMeters => _volumeMinHeightMeters;
        public float VolumeMaxHeightMeters => _volumeMaxHeightMeters;
        public float MinDriftSecondsBeforeCapture => _minDriftSecondsBeforeCapture;
        public float MaxDistanceMeters => _maxDistanceMeters;
        public float MaxDriftSeconds => _maxDriftSeconds;
    }
}
