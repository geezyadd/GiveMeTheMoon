using System;
using Features.ShipModule.Scripts.Contracts;
using Features.ShipModule.Scripts.Generated;

namespace Features.ShipDamageModule.Scripts.Damage {
    // The module condition as the synced ShipCondition model has it: a socket in its broken list does not work.
    public sealed class ShipModuleCondition : IShipModuleCondition {
        private readonly IReadOnlyShipConditionModel _shipConditionModel;

        public ShipModuleCondition(IReadOnlyShipConditionModel shipConditionModel) =>
            _shipConditionModel = shipConditionModel;

        public event Action OnConditionChanged {
            add => _shipConditionModel.OnBrokenSocketsChanged += value;
            remove => _shipConditionModel.OnBrokenSocketsChanged -= value;
        }

        public bool IsWorking(string socketId) {
            foreach (string brokenSocketId in _shipConditionModel.BrokenSockets) {
                if (string.Equals(brokenSocketId, socketId, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }
    }
}
