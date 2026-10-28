using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Configuration;
using Mirror;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Spawn {
    // The server's view of each connected player: its player object's position plus the eye height. The camera itself
    // is not synced; a third-person camera sits a few metres off this point.
    public sealed class FieldAgentPlayerViewpointSource : IFieldAgentViewpointSource {
        private readonly FieldAgentConfiguration _fieldAgentConfiguration;

        public FieldAgentPlayerViewpointSource(FieldAgentConfiguration fieldAgentConfiguration) =>
            _fieldAgentConfiguration = fieldAgentConfiguration;

        public void CollectViewpoints(List<Vector3> results) {
            results.Clear();
            Vector3 eye = Vector3.up * _fieldAgentConfiguration.ViewpointEyeHeight;
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values) {
                if (connection.identity != null)
                    results.Add(connection.identity.transform.position + eye);
            }
        }
    }
}
