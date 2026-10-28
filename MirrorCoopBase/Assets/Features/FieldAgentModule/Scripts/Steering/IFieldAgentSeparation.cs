using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Steering {
    // Keeping distance from the other flying agents.
    public interface IFieldAgentSeparation {
        public Vector3 CalculateSeparation(int selfIndex, FieldAgentServerModel model, FieldAgentProfile profile);
    }
}
