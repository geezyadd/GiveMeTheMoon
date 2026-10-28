namespace Features.FieldAgentModule.Scripts.Client {
    // A component of an agent look that sets itself up for the agent the pooled look now shows and resets when the
    // look goes back to the pool (a kind's weapon target, its death look). FieldAgentView finds them on its children.
    public interface IFieldAgentViewLifetime {
        public void Begin(int agentId);
        public void End();
    }
}
