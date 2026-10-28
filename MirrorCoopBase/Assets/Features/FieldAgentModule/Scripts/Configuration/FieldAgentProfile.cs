using Features.FieldAgentModule.Scripts.Client;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Configuration {
    // One kind of field agent (a bug, later a drone or a fish): how it flies and walks on the server and how its view
    // feels on every peer. A new kind is a new profile asset in FieldAgentConfiguration.Profiles, not code.
    [CreateAssetMenu(fileName = nameof(FieldAgentProfile), menuName = "Configurations/FieldAgent/" + nameof(FieldAgentProfile))]
    public sealed class FieldAgentProfile : ScriptableObject {
        [SerializeField] private string _id;
        [SerializeField] private FieldAgentView _viewPrefab;

        [Header("Flight")]
        [Tooltip("Top flight speed, m/s.")]
        [SerializeField] private float _maxSpeed = 16f;
        [Tooltip("How fast the speed changes, m/s squared.")]
        [SerializeField] private float _acceleration = 18f;
        [Tooltip("How fast the flight direction turns, degrees per second.")]
        [SerializeField] private float _turnRateDegrees = 200f;
        [Tooltip("The agent's own sphere, metres: it never comes into a rock sphere with it.")]
        [SerializeField] private float _radius = 0.6f;
        [Tooltip("Distance to a target at which the agent starts to slow down, metres.")]
        [SerializeField] private float _slowingMeters = 8f;

        [Header("Rocks")]
        [Tooltip("How far ahead along its velocity the agent looks for rock spheres, seconds of flight.")]
        [SerializeField] private float _lookAheadSeconds = 1.5f;
        [Tooltip("The shortest look-ahead, metres, so a slow agent still sees the rock in front of it.")]
        [SerializeField] private float _minLookAheadMeters = 6f;
        [Tooltip("The gap the agent keeps to a rock sphere beyond its own radius, metres.")]
        [SerializeField] private float _rockClearance = 2f;

        [Header("Flock")]
        [Tooltip("Agents closer than this push each other apart, metres.")]
        [SerializeField] private float _separationRadius = 3f;
        [Tooltip("The light wander on top of the steering, a share of the top speed.")]
        [SerializeField] private float _noiseStrength = 0.15f;
        [Tooltip("How fast the wander changes, cycles per second.")]
        [SerializeField] private float _noiseFrequency = 0.35f;

        [Header("Deck")]
        [Tooltip("Walking speed on the deck NavMesh, m/s.")]
        [SerializeField] private float _deckWalkSpeed = 1.8f;
        [Tooltip("How fast the agent turns while walking, degrees per second.")]
        [SerializeField] private float _deckTurnRateDegrees = 360f;
        [Tooltip("The landing starts from this height above its deck point, metres.")]
        [SerializeField] private float _landingApproachHeight = 3f;
        [Tooltip("How close to the approach point the landing starts, metres.")]
        [SerializeField] private float _landingStartMeters = 2.5f;
        [Tooltip("The landing from the approach point onto the deck, seconds.")]
        [SerializeField] private float _landingSeconds = 1.2f;
        [Tooltip("The take-off from the deck up to flight, seconds.")]
        [SerializeField] private float _takeOffSeconds = 0.8f;
        [Tooltip("How high above the deck the take-off ends, metres.")]
        [SerializeField] private float _takeOffHeight = 2.5f;
        [Tooltip("The upward speed the agent leaves the take-off with, m/s.")]
        [SerializeField] private float _takeOffSpeed = 5f;

        [Header("View feel (local, not synced)")]
        [Tooltip("Bank in degrees per degree per second of turn.")]
        [SerializeField] private float _bankPerTurnRate = 0.2f;
        [SerializeField] private float _maxBankDegrees = 40f;
        [Tooltip("Pitch in degrees per m/s squared of forward acceleration.")]
        [SerializeField] private float _tiltPerAcceleration = 1.5f;
        [SerializeField] private float _maxTiltDegrees = 20f;
        [Tooltip("The light up and down bob, metres.")]
        [SerializeField] private float _bobAmplitude = 0.12f;
        [Tooltip("Bob cycles per second.")]
        [SerializeField] private float _bobFrequency = 1.6f;
        [Tooltip("How fast bank and tilt follow the motion, seconds.")]
        [SerializeField] private float _feelSmoothSeconds = 0.15f;

        public string Id => _id;
        public FieldAgentView ViewPrefab => _viewPrefab;
        public float MaxSpeed => _maxSpeed;
        public float Acceleration => _acceleration;
        public float TurnRateDegrees => _turnRateDegrees;
        public float Radius => _radius;
        public float SlowingMeters => _slowingMeters;
        public float LookAheadSeconds => _lookAheadSeconds;
        public float MinLookAheadMeters => _minLookAheadMeters;
        public float RockClearance => _rockClearance;
        public float SeparationRadius => _separationRadius;
        public float NoiseStrength => _noiseStrength;
        public float NoiseFrequency => _noiseFrequency;
        public float DeckWalkSpeed => _deckWalkSpeed;
        public float DeckTurnRateDegrees => _deckTurnRateDegrees;
        public float LandingApproachHeight => _landingApproachHeight;
        public float LandingStartMeters => _landingStartMeters;
        public float LandingSeconds => _landingSeconds;
        public float TakeOffSeconds => _takeOffSeconds;
        public float TakeOffHeight => _takeOffHeight;
        public float TakeOffSpeed => _takeOffSpeed;
        public float BankPerTurnRate => _bankPerTurnRate;
        public float MaxBankDegrees => _maxBankDegrees;
        public float TiltPerAcceleration => _tiltPerAcceleration;
        public float MaxTiltDegrees => _maxTiltDegrees;
        public float BobAmplitude => _bobAmplitude;
        public float BobFrequency => _bobFrequency;
        public float FeelSmoothSeconds => _feelSmoothSeconds;
    }
}
