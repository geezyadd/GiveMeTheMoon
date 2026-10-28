using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Configuration;

namespace Features.FieldAgentModule.Scripts.Steering {
    // One tick of a flying agent: its behaviour's goal through the steering into a new world position.
    public interface IFieldAgentFlightStepService {
        public void Step(int index, FieldAgentServerModel model, FieldAgentProfile profile, in FieldAgentShipFrame ship, float time, float deltaTime);
    }
}
