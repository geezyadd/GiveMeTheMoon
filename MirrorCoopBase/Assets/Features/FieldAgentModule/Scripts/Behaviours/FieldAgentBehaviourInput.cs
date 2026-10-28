using Features.FieldAgentModule.Scripts.Agents;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Behaviours {
    // An agent's situation for its behaviour this tick: where it is in both spaces, its mode, the ship and the time.
    public readonly struct FieldAgentBehaviourInput {
        public int AgentId { get; }
        public FieldAgentMode Mode { get; }
        public Vector3 WorldPosition { get; }
        public Vector3 ShipLocalPosition { get; }
        public FieldAgentShipFrame Ship { get; }
        public float Time { get; }

        public FieldAgentBehaviourInput(int agentId, FieldAgentMode mode, Vector3 worldPosition, Vector3 shipLocalPosition, FieldAgentShipFrame ship, float time) {
            AgentId = agentId;
            Mode = mode;
            WorldPosition = worldPosition;
            ShipLocalPosition = shipLocalPosition;
            Ship = ship;
            Time = time;
        }
    }
}
