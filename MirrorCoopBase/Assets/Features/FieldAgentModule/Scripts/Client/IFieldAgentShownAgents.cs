using System.Collections.Generic;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Client {
    // The agents this peer shows now, by id: what a player sees (the coop test compares it with the server), and the
    // look of a shown agent for a feature that plays something on it (a bug's death).
    public interface IFieldAgentShownAgents {
        public int ShownCount { get; }
        public bool TryGetView(int agentId, out FieldAgentView view);
        public bool TryGetShownWorldPosition(int agentId, out Vector3 worldPosition);
        public void CollectShownIds(List<int> ids);
    }
}
