using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Agents {
    // One agent on the server, one slot of FieldAgentServerModel's array: plain data the movement steps write in
    // place. Position, velocity and rotation are world values in flight and ship-local values in the ship modes.
    public struct FieldAgentState {
        public int Id;
        public byte KindIndex;
        public FieldAgentMode Mode;
        public Vector3 Position;
        public Vector3 Velocity;
        public Quaternion Rotation;
        // A per-agent offset into the wander noise, so two agents never wander alike.
        public float NoisePhase;
        // Landing and take-off: the ship-local start and end and the time spent on the way.
        public Vector3 TransitionFrom;
        public Vector3 TransitionTo;
        public float TransitionElapsed;
        public bool IsBehaviourFinished;

        public bool IsInShipSpace =>
            Mode != FieldAgentMode.Flight;
    }
}
