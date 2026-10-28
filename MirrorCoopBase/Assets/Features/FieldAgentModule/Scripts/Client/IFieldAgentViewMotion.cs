using Features.FieldAgentModule.Scripts.Agents;

namespace Features.FieldAgentModule.Scripts.Client {
    // A component of an agent look that animates itself every shown frame from the agent's movement mode (a bug's
    // wings and tail), local on every peer. FieldAgentView finds them on its children.
    public interface IFieldAgentViewMotion {
        public void Show(FieldAgentMode mode, float deltaTime, float time);
    }
}
