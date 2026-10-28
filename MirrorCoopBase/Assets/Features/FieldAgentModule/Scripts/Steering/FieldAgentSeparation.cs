using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Steering {
    // Every other flying agent inside the separation radius pushes this one away, harder the closer it is. A plain
    // pass over the model: 50 agents are 2 500 cheap distance checks per tick.
    public sealed class FieldAgentSeparation : IFieldAgentSeparation {
        private const float MIN_DISTANCE_SQR = 0.0001f;

        public Vector3 CalculateSeparation(int selfIndex, FieldAgentServerModel model, FieldAgentProfile profile) {
            Vector3 position = model.GetState(selfIndex).Position;
            float radius = profile.SeparationRadius;
            float radiusSqr = radius * radius;
            Vector3 push = Vector3.zero;
            for (int i = 0; i < model.Count; i++) {
                ref FieldAgentState other = ref model.GetState(i);
                if (i == selfIndex || other.Mode != FieldAgentMode.Flight)
                    continue;

                Vector3 offset = position - other.Position;
                float distanceSqr = offset.sqrMagnitude;
                if (distanceSqr >= radiusSqr || distanceSqr < MIN_DISTANCE_SQR)
                    continue;

                float distance = Mathf.Sqrt(distanceSqr);
                push += offset / distance * (1f - distance / radius);
            }

            return push * profile.MaxSpeed;
        }
    }
}
