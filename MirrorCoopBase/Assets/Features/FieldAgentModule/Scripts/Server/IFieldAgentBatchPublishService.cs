using Features.FieldAgentModule.Scripts.Agents;

namespace Features.FieldAgentModule.Scripts.Server {
    // Server: sends every agent of the model to the clients as one batch.
    public interface IFieldAgentBatchPublishService {
        public void Publish(FieldAgentServerModel model);
    }
}
