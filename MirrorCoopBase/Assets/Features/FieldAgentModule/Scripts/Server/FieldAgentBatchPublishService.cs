using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Network;
using Features.ShipModule.Scripts.Contracts;
using Mirror;

namespace Features.FieldAgentModule.Scripts.Server {
    // Packs the model into one sorted batch at the server's network time (position and the compressed rotation, in
    // the agent's own space) and hands it to the synced FieldAgents model.
    public sealed class FieldAgentBatchPublishService : IFieldAgentBatchPublishService {
        private readonly IReadOnlyComponentRegistry<FieldAgentSync> _fieldAgentSyncRegistry;

        public FieldAgentBatchPublishService(IReadOnlyComponentRegistry<FieldAgentSync> fieldAgentSyncRegistry) =>
            _fieldAgentSyncRegistry = fieldAgentSyncRegistry;

        public void Publish(FieldAgentServerModel model) {
            if (_fieldAgentSyncRegistry.Items.Count == 0)
                return;

            FieldAgentSnapshot[] agents = new FieldAgentSnapshot[model.Count];
            for (int i = 0; i < model.Count; i++) {
                ref FieldAgentState state = ref model.GetState(i);
                agents[i] = new FieldAgentSnapshot(state.Id, state.KindIndex, (byte)state.Mode, state.Position, Compression.CompressQuaternion(state.Rotation));
            }

            FieldAgentFrame frame = new(model.TakeFrameSequence(), NetworkTime.time, agents);
            foreach (FieldAgentSync sync in _fieldAgentSyncRegistry.Items)
                sync.ServerSetFrame(frame);
        }
    }
}
