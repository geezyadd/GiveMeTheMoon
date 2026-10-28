using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Behaviours;
using Features.FieldAgentModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Server {
    // Server: how other features (bugs, the nest, the debug actions) create agents and steer them from outside. Every
    // call returns false for an id that is gone.
    public interface IFieldAgentControl {
        public int Count { get; }
        public int Spawn(FieldAgentProfile profile, Vector3 worldPosition, IFieldAgentBehaviour behaviour);
        public bool Despawn(int id);
        public void DespawnAll();
        public bool SetBehaviour(int id, IFieldAgentBehaviour behaviour);
        public bool TakeOff(int id);
        public bool KnockOff(int id, Vector3 worldVelocity);
        public bool TryGetAgent(int id, out FieldAgentState state);
        public bool TryGetWorldPosition(int id, out Vector3 worldPosition);
    }
}
