using System.Collections.Generic;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Configuration {
    // The shared field agent tuning: the kinds, the steering weights, the network batch, the deck NavMesh, the hidden
    // spawn and the debug flock.
    [CreateAssetMenu(fileName = nameof(FieldAgentConfiguration) + "_Default", menuName = "Configurations/FieldAgent/" + nameof(FieldAgentConfiguration))]
    public sealed class FieldAgentConfiguration : ScriptableObject {
        [Tooltip("Every agent kind; the index in this list is the kind number on the network, so only append.")]
        [SerializeField] private List<FieldAgentProfile> _profiles = new();

        [Header("Steering weights")]
        [SerializeField] private float _seekWeight = 1f;
        [SerializeField] private float _avoidanceWeight = 1.5f;
        [SerializeField] private float _separationWeight = 1.2f;
        [Tooltip("A flying agent asks the field for the rocks around it about this often, seconds; between two queries it reads its cached list.")]
        [SerializeField] private float _rockQuerySeconds = 0.25f;

        [Header("Network")]
        [Tooltip("Agent batches sent to the clients per second.")]
        [SerializeField] private float _sendRate = 12f;
        [Tooltip("Clients show the agents this far in the past, seconds, so they always interpolate between two batches.")]
        [SerializeField] private float _interpolationDelaySeconds = 0.25f;
        [Tooltip("How many batches a client keeps for the interpolation.")]
        [SerializeField] private int _frameBufferSize = 6;

        [Header("Deck")]
        [Tooltip("Where the server places the ship's deck NavMesh in its ship space: far from everything, so a deck query never meets the world.")]
        [SerializeField] private Vector3 _deckNavMeshAnchor = new(0f, -20000f, 0f);
        [Tooltip("How far from a wanted point a deck point is searched on the NavMesh, metres.")]
        [SerializeField] private float _deckSampleMeters = 3f;
        [Tooltip("A deck path is computed again after this time, seconds.")]
        [SerializeField] private float _deckRepathSeconds = 1f;
        [Tooltip("A deck path is computed again when its goal moved this far, metres.")]
        [SerializeField] private float _deckRepathMeters = 0.5f;
        [Tooltip("An agent counts as at a deck corner within this distance, metres.")]
        [SerializeField] private float _deckCornerMeters = 0.1f;

        [Header("Hidden spawn")]
        [SerializeField] private float _spawnMinMeters = 90f;
        [SerializeField] private float _spawnMaxMeters = 220f;
        [Tooltip("Up and down spread of the spawn candidates around the centre, metres.")]
        [SerializeField] private float _spawnHeightSpreadMeters = 30f;
        [SerializeField] private int _spawnTries = 64;
        [Tooltip("A spawn point beyond the haze lies this far past its hidden distance from every player, metres.")]
        [SerializeField] private float _spawnHazeMarginMeters = 30f;
        [Tooltip("A spawn point keeps this far from any rock sphere, metres.")]
        [SerializeField] private float _spawnRockClearance = 3f;
        [Tooltip("Height of a player's view above the player's position, metres.")]
        [SerializeField] private float _viewpointEyeHeight = 1.6f;
        [Tooltip("A spawned group scatters this far around its hidden point, metres.")]
        [SerializeField] private float _groupScatterMeters = 5f;

        [Header("Debug flock")]
        [SerializeField] private int _debugFlockSize = 20;
        [SerializeField] private float _debugSeekArriveMeters = 26f;
        [SerializeField] private float _debugOrbitRadius = 18f;
        [SerializeField] private float _debugOrbitHeight = 5f;
        [SerializeField] private float _debugOrbitSpeedShare = 0.6f;
        [Tooltip("How far ahead on the orbit circle the agent aims, degrees.")]
        [SerializeField] private float _debugOrbitLeadDegrees = 25f;
        [Tooltip("The deck trip stands at the module this long, seconds.")]
        [SerializeField] private float _debugDeckPauseSeconds = 1.5f;
        [Tooltip("The deck trip counts a deck point as reached this close, metres.")]
        [SerializeField] private float _debugDeckArriveMeters = 0.35f;
        [Tooltip("The deck trip lands at this point of the deck, ship-local.")]
        [SerializeField] private Vector3 _debugDeckLandingPoint = new(0f, 0.3f, -5f);
        [Tooltip("The deck trip walks to the helm socket and stops this far in front of it, metres.")]
        [SerializeField] private float _debugModuleStandOffMeters = 1.2f;

        public IReadOnlyList<FieldAgentProfile> Profiles => _profiles;
        public float SeekWeight => _seekWeight;
        public float AvoidanceWeight => _avoidanceWeight;
        public float SeparationWeight => _separationWeight;
        public float RockQuerySeconds => _rockQuerySeconds;
        public float SendRate => _sendRate;
        public float InterpolationDelaySeconds => _interpolationDelaySeconds;
        public int FrameBufferSize => _frameBufferSize;
        public Vector3 DeckNavMeshAnchor => _deckNavMeshAnchor;
        public float DeckSampleMeters => _deckSampleMeters;
        public float DeckRepathSeconds => _deckRepathSeconds;
        public float DeckRepathMeters => _deckRepathMeters;
        public float DeckCornerMeters => _deckCornerMeters;
        public float SpawnMinMeters => _spawnMinMeters;
        public float SpawnMaxMeters => _spawnMaxMeters;
        public float SpawnHeightSpreadMeters => _spawnHeightSpreadMeters;
        public int SpawnTries => _spawnTries;
        public float SpawnHazeMarginMeters => _spawnHazeMarginMeters;
        public float SpawnRockClearance => _spawnRockClearance;
        public float ViewpointEyeHeight => _viewpointEyeHeight;
        public int DebugFlockSize => _debugFlockSize;
        public float GroupScatterMeters => _groupScatterMeters;
        public float DebugSeekArriveMeters => _debugSeekArriveMeters;
        public float DebugOrbitRadius => _debugOrbitRadius;
        public float DebugOrbitHeight => _debugOrbitHeight;
        public float DebugOrbitSpeedShare => _debugOrbitSpeedShare;
        public float DebugOrbitLeadDegrees => _debugOrbitLeadDegrees;
        public float DebugDeckPauseSeconds => _debugDeckPauseSeconds;
        public float DebugDeckArriveMeters => _debugDeckArriveMeters;
        public Vector3 DebugDeckLandingPoint => _debugDeckLandingPoint;
        public float DebugModuleStandOffMeters => _debugModuleStandOffMeters;
    }
}
