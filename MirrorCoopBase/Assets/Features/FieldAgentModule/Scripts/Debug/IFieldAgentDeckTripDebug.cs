#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.FieldAgentModule.Scripts.Debug {
    // Debug only, on the host: one test agent lands on the deck, walks to the helm module, walks back and takes off.
    // Public for the loop tests; the trip's state is in FieldAgentDeckTripDebugModel.
    public interface IFieldAgentDeckTripDebug {
        public bool DebugStartDeckTrip();
    }
}
#endif
