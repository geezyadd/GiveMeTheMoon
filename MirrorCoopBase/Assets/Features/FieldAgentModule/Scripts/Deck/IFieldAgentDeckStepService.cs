using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Configuration;

namespace Features.FieldAgentModule.Scripts.Deck {
    // One tick of an agent walking on the deck NavMesh.
    public interface IFieldAgentDeckStepService {
        public void Step(int index, FieldAgentServerModel model, FieldAgentProfile profile, in FieldAgentShipFrame ship, float time, float deltaTime);
    }
}
