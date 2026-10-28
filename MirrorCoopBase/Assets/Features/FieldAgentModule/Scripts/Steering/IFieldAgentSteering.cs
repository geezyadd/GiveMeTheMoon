using Features.FieldAgentModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Steering {
    // The seek force and the profile's limits on a velocity change.
    public interface IFieldAgentSteering {
        public Vector3 CalculateSeek(Vector3 position, Vector3 target, float speedShare, FieldAgentProfile profile);
        public Vector3 LimitVelocity(Vector3 current, Vector3 desired, FieldAgentProfile profile, float deltaTime);
    }
}
