#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.DebugModule.Contracts;
using Mirror;

namespace Features.ShipModule.Scripts.Debug {
    public sealed class ShipDebugActionConditions : IShipDebugActionConditions {
        private readonly IShipRunBindingModel _shipRunBindingModel;

        public ShipDebugActionConditions(IShipRunBindingModel shipRunBindingModel) =>
            _shipRunBindingModel = shipRunBindingModel;

        public string ReadHostReason() =>
            NetworkServer.active ? null : DebugActionReasons.HOST_ONLY;

        public string ReadHostWithShipReason() {
            if (NetworkServer.active == false)
                return DebugActionReasons.HOST_ONLY;

            return _shipRunBindingModel.Ship == null ? DebugActionReasons.NO_SHIP : null;
        }

        public string ReadHostInFlightReason() {
            string reason = ReadHostWithShipReason();
            if (reason != null)
                return reason;

            return _shipRunBindingModel.Ship.IsFlying ? null : DebugActionReasons.NOT_FLYING;
        }
    }
}
#endif
