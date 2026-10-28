using UnityEngine;

namespace Features.BugModule.Scripts.View {
    // The look numbers of a bug, local on every peer: the wings, the legs, the tail's twitch, the gnawing sway and the
    // flinch when a shot strikes it. Referenced by the bug's view prefab.
    [CreateAssetMenu(fileName = nameof(BugViewFeel), menuName = "Configurations/Bug/" + nameof(BugViewFeel))]
    public sealed class BugViewFeel : ScriptableObject {
        [Header("Wings")]
        [Tooltip("Wing beats per second in flight.")]
        [SerializeField, Min(0f)] private float _wingBeatsPerSecond = 18f;
        [Tooltip("How far a wing swings up and down in flight, degrees.")]
        [SerializeField, Min(0f)] private float _wingBeatDegrees = 50f;
        [Tooltip("The wings fold back by this much while the bug sits, degrees.")]
        [SerializeField] private float _wingFoldDegrees = 60f;
        [Tooltip("The small buzz of the folded wings, degrees.")]
        [SerializeField, Min(0f)] private float _wingBuzzDegrees = 4f;

        [Header("Legs")]
        [Tooltip("How far a leg swings, degrees.")]
        [SerializeField, Min(0f)] private float _legSwingDegrees = 14f;
        [Tooltip("Leg swings per second in flight (they dangle).")]
        [SerializeField, Min(0f)] private float _flightLegSwingsPerSecond = 2f;
        [Tooltip("Leg swings per second on the ship (they scrabble at it).")]
        [SerializeField, Min(0f)] private float _deckLegSwingsPerSecond = 9f;

        [Header("Tail")]
        [Tooltip("How far the tail's base twitches in flight, degrees; the chain behind it trails on the rig.")]
        [SerializeField, Min(0f)] private float _flightTailTwitchDegrees = 18f;
        [Tooltip("How far the tail's base twitches while the bug gnaws, degrees.")]
        [SerializeField, Min(0f)] private float _gnawTailTwitchDegrees = 40f;
        [Tooltip("How fast the twitch changes, cycles per second.")]
        [SerializeField, Min(0f)] private float _tailTwitchFrequency = 2.5f;

        [Header("Gnawing")]
        [Tooltip("The bite nod down and up, degrees.")]
        [SerializeField, Min(0f)] private float _gnawNodDegrees = 14f;
        [Tooltip("Bite nods per second.")]
        [SerializeField, Min(0f)] private float _gnawNodsPerSecond = 4f;
        [Tooltip("The sway from side to side, degrees.")]
        [SerializeField, Min(0f)] private float _gnawSwayDegrees = 10f;
        [Tooltip("Sways per second.")]
        [SerializeField, Min(0f)] private float _gnawSwaysPerSecond = 1.3f;

        [Header("Struck")]
        [Tooltip("How long a struck bug flinches, seconds.")]
        [SerializeField, Min(0.01f)] private float _flinchSeconds = 0.15f;
        [Tooltip("How much a struck bug squashes, a share of its size.")]
        [SerializeField, Range(0f, 0.9f)] private float _flinchSquash = 0.35f;

        public float WingBeatsPerSecond => _wingBeatsPerSecond;
        public float WingBeatDegrees => _wingBeatDegrees;
        public float WingFoldDegrees => _wingFoldDegrees;
        public float WingBuzzDegrees => _wingBuzzDegrees;
        public float LegSwingDegrees => _legSwingDegrees;
        public float FlightLegSwingsPerSecond => _flightLegSwingsPerSecond;
        public float DeckLegSwingsPerSecond => _deckLegSwingsPerSecond;
        public float FlightTailTwitchDegrees => _flightTailTwitchDegrees;
        public float GnawTailTwitchDegrees => _gnawTailTwitchDegrees;
        public float TailTwitchFrequency => _tailTwitchFrequency;
        public float GnawNodDegrees => _gnawNodDegrees;
        public float GnawNodsPerSecond => _gnawNodsPerSecond;
        public float GnawSwayDegrees => _gnawSwayDegrees;
        public float GnawSwaysPerSecond => _gnawSwaysPerSecond;
        public float FlinchSeconds => _flinchSeconds;
        public float FlinchSquash => _flinchSquash;
    }
}
