using Features.FieldAgentModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Steering {
    // The light wander noise on top of the steering.
    public interface IFieldAgentWander {
        public Vector3 CalculateWander(float noisePhase, float time, FieldAgentProfile profile);
    }
}
