using System.Collections.Generic;
using UnityEngine;

namespace Game.Connection
{
    [CreateAssetMenu(fileName = "ConnectionConfig_Default", menuName = "Game/Connection Config")]
    public sealed class ConnectionConfig : ScriptableObject
    {
        [SerializeField] int maxConnections = 4;
        [SerializeField] float everyoneReadyDelay = 0.5f;

        [Tooltip("Debug builds only: the network conditions the debug window cycles through. The first one is the start.")]
        [SerializeField] ConnectionLatencyProfile[] latencyProfiles = { new ConnectionLatencyProfile() };

        public int MaxConnections => maxConnections;
        public float EveryoneReadyDelay => everyoneReadyDelay;
        public IReadOnlyList<ConnectionLatencyProfile> LatencyProfiles => latencyProfiles;
    }
}
