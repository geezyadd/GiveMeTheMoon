using Mirror;
using UnityEngine;

namespace Game.Connection
{
    // Debug builds only: puts a ConnectionLagTransport between the network manager and the chosen transport, so the
    // debug window can add delay, jitter and loss to this process's outgoing messages. It starts with none of them,
    // which passes messages through unchanged within the same frame.
    public sealed class ConnectionLatencySimulation
    {
        readonly ConnectionLagTransport lagTransport;

        public ConnectionLatencySimulation(Transform parent, ConnectionLagTransport prefab)
        {
            lagTransport = Object.Instantiate(prefab, parent, false);
            lagTransport.name = prefab.name;
            Apply(new ConnectionLatencyProfile());
        }

        // The transport the network manager uses: the lag transport, wrapping the chosen one. Only while no session runs.
        public Transport Wrap(Transport chosen)
        {
            lagTransport.SetWrapped(chosen);
            return lagTransport;
        }

        public void Apply(ConnectionLatencyProfile profile) =>
            lagTransport.ApplySettings(ConnectionLagSettings.From(profile));
    }
}
