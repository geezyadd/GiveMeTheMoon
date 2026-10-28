namespace Features.FieldAgentModule.Scripts.Client {
    // The pooled agent looks of this peer, one pool per agent kind.
    public interface IFieldAgentViewPool {
        public FieldAgentView Take(byte kindIndex, int agentId);
        public void Return(byte kindIndex, FieldAgentView view);
    }
}
