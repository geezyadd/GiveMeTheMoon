using System.Collections.Generic;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Client {
    // The agents this peer shows now, by id: what a player sees (the coop test compares it with the server).
    public interface IFieldAgentShownAgents {
        public int ShownCount { get; }
        public bool TryGetShownWorldPosition(int agentId, out Vector3 worldPosition);
        public void CollectShownIds(List<int> ids);
    }
}
