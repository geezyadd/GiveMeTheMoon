using Features.FieldAgentModule.Scripts.Behaviours;
using Features.FieldAgentModule.Scripts.Cargo;
using UnityEngine;

namespace Features.BugModule.Scripts.Behaviours {
    // The field agent behaviours of a bug's stages, every one flown the bug's darting way.
    public interface IBugBehaviourFactory {
        public IFieldAgentBehaviour CreateApproach(float approachAngleDegrees);
        public IFieldAgentBehaviour CreateSeekCargo(IFieldAgentCargoTarget cargo);
        public IFieldAgentBehaviour CreateCarry(IFieldAgentCargoTarget cargo, Vector3 worldDestination);
        public IFieldAgentBehaviour CreateLand(Vector3 gnawPoint);
        public IFieldAgentBehaviour CreateLeave();
    }
}
