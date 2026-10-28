#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.DebugModule.Contracts;
using Features.ShipModule.Scripts;
using Mirror;

namespace Features.FieldAgentModule.Scripts.Debug {
    // When the agent debug actions can run: on the host with a bound ship.
    public sealed class FieldAgentDebugConditions : IFieldAgentDebugConditions {
        private readonly IShipRunBindingModel _shipRunBindingModel;

        public FieldAgentDebugConditions(IShipRunBindingModel shipRunBindingModel) =>
            _shipRunBindingModel = shipRunBindingModel;

        public string ReadHostWithShipReason() {
            if (NetworkServer.active == false)
                return DebugActionReasons.HOST_ONLY;

            return _shipRunBindingModel.Ship == null ? DebugActionReasons.NO_SHIP : null;
        }
    }
}
#endif
