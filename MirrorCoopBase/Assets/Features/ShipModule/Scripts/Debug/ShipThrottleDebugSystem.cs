#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.DebugModule.Contracts;
using Zenject;

namespace Features.ShipModule.Scripts.Debug {
    // Debug only, on the host: the debug window's "Throttle = N" action. It goes through the same server move as the
    // holder's push, so the launch rule and the synced throttle see it.
    public sealed class ShipThrottleDebugSystem : IInitializable, IDisposable, IShipThrottleDebug {
        private const string THROTTLE_LABEL = "Throttle = N";
        private const string THROTTLE_DESCRIPTION = "Puts the throttle lever at N (0 = Stop, 1 = Full) as if pushed there: at a station past the launch mark the ship takes off.";
        private const string THROTTLE_VALUE_LABEL = "N";
        private const float DEFAULT_THROTTLE = 1f;

        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IDebugActionRegistry _debugActionRegistry;
        private readonly IShipDebugActionConditions _shipDebugActionConditions;

        private IDisposable _registration;

        public ShipThrottleDebugSystem(
            IShipRunBindingModel shipRunBindingModel,
            IDebugActionRegistry debugActionRegistry,
            IShipDebugActionConditions shipDebugActionConditions) {
            _shipRunBindingModel = shipRunBindingModel;
            _debugActionRegistry = debugActionRegistry;
            _shipDebugActionConditions = shipDebugActionConditions;
        }

        public void Initialize() {
            DebugActionValue throttle = new(THROTTLE_VALUE_LABEL, DEFAULT_THROTTLE);
            _registration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.SHIP, THROTTLE_LABEL, THROTTLE_DESCRIPTION, _shipDebugActionConditions.ReadHostWithShipReason, throttle, DebugSetThrottle));
        }

        public void Dispose() =>
            _registration.Dispose();

        public void DebugSetThrottle(float position) =>
            _shipRunBindingModel.Ship.Throttle.ServerMove(position);
    }
}
#endif
