using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Agents {
    // The rocks around one flying agent from its last field query: a query covers where the agent can get until the
    // next one, so most ticks read this list instead of the field. One per model slot, reused.
    public sealed class FieldAgentRockCache {
        public List<FieldRockDescription> Rocks { get; } = new();
        public Vector3 Center { get; private set; }
        public float Radius { get; private set; }
        public float Age { get; private set; } = float.MaxValue;

        public void Refill(Vector3 center, float radius) {
            Center = center;
            Radius = radius;
            Age = 0f;
        }

        public void AddAge(float seconds) =>
            Age += seconds;

        public void Invalidate() =>
            Age = float.MaxValue;
    }
}
