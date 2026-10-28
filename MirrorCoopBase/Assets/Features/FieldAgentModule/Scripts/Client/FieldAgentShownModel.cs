using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Client {
    // What this peer shows of the field agents: the kept batches it interpolates between and the look of every shown
    // agent by id with its kind. Written by FieldAgentClientSystem, read by whoever asks what a player sees. Bound in
    // DataInstaller.
    public sealed class FieldAgentShownModel : IFieldAgentShownAgents {
        private readonly Dictionary<int, FieldAgentView> _views = new();
        private readonly Dictionary<int, byte> _viewKinds = new();

        public FieldAgentFrameBuffer FrameBuffer { get; }
        public int ShownCount => _views.Count;

        public FieldAgentShownModel(FieldAgentConfiguration fieldAgentConfiguration) =>
            FrameBuffer = new FieldAgentFrameBuffer(fieldAgentConfiguration.FrameBufferSize);

        public bool TryGetView(int agentId, out FieldAgentView view) =>
            _views.TryGetValue(agentId, out view) && view != null;

        public void AddView(int agentId, byte kindIndex, FieldAgentView view) {
            _views[agentId] = view;
            _viewKinds[agentId] = kindIndex;
        }

        public byte GetViewKind(int agentId) =>
            _viewKinds[agentId];

        public void RemoveView(int agentId) {
            _views.Remove(agentId);
            _viewKinds.Remove(agentId);
        }

        public bool TryGetShownWorldPosition(int agentId, out Vector3 worldPosition) {
            worldPosition = Vector3.zero;
            if (TryGetView(agentId, out FieldAgentView view) == false)
                return false;

            worldPosition = view.transform.position;
            return true;
        }

        public void CollectShownIds(List<int> ids) {
            ids.Clear();
            ids.AddRange(_views.Keys);
        }
    }
}
