using Features.FieldAgentModule.Scripts.Agents;
using Features.ShipModule.Scripts;

namespace Features.BugModule.Scripts.Brain {
    public interface IBugBrainService {
        // Server: moves the bug on to its next stage when its behaviour is done, and bites while it gnaws.
        public void ServerStep(BugAgent bug, in FieldAgentState state, ShipBase ship, float deltaTime);
    }
}
