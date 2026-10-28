#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Debug {
    // Debug only, on the host: one test agent lands on the deck, walks to the helm module, walks back and takes off.
    // Public for the loop tests.
    public interface IFieldAgentDeckTripDebug {
        public FieldAgentDeckTripStage Stage { get; }
        public int AgentId { get; }
        public Vector3 LandingPoint { get; }
        public Vector3 ModulePoint { get; }
        public bool DebugStartDeckTrip();
    }
}
#endif
