using System;
using Features.ShipModule.Scripts.Contracts;
using Features.ShipModule.Scripts.Generated;

namespace Features.ShipDamageModule.Scripts.Damage {
    // The breaches as the synced ShipCondition model has them.
    public sealed class ShipBreachCondition : IShipBreachCondition {
        private readonly IReadOnlyShipConditionModel _shipConditionModel;

        public ShipBreachCondition(IReadOnlyShipConditionModel shipConditionModel) =>
            _shipConditionModel = shipConditionModel;

        public event Action OnBreachesChanged {
            add => _shipConditionModel.OnOpenBreachesChanged += value;
            remove => _shipConditionModel.OnOpenBreachesChanged -= value;
        }

        public int OpenCount => _shipConditionModel.OpenBreaches.Count;

        public bool IsOpen(string pointId) {
            foreach (string openPointId in _shipConditionModel.OpenBreaches) {
                if (string.Equals(openPointId, pointId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
