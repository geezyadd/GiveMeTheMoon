using System;
using UnityEngine;

namespace Game.Connection
{
    // One simulated network condition for this process's outgoing messages (debug builds only).
    [Serializable]
    public sealed class ConnectionLatencyProfile
    {
        [SerializeField] string label = "Off";
        [Tooltip("Delay of every outgoing message, milliseconds.")]
        [SerializeField, Min(0f)] float latencyMilliseconds;
        [Tooltip("Extra random delay per message, from 0 up to this many milliseconds. Each message draws its own, so the delay jumps between messages as on a real link.")]
        [SerializeField, Min(0f)] float jitterMilliseconds;
        [Tooltip("Share of messages lost, percent. A lost unreliable message never arrives; a lost reliable one arrives a round trip later and holds back the reliable messages behind it.")]
        [SerializeField, Range(0f, 100f)] float lossPercent;

        public string Label => label;
        public float LatencyMilliseconds => latencyMilliseconds;
        public float JitterMilliseconds => jitterMilliseconds;
        public float LossPercent => lossPercent;
    }
}
