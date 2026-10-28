#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.FieldAgentModule.Scripts.Debug {
    // The availability check shared by the agent debug actions.
    public interface IFieldAgentDebugConditions {
        public string ReadHostWithShipReason();
    }
}
#endif
