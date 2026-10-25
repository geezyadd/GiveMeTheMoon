using Features.GameCoreModule.Contracts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipDamageModule.Scripts.Damage {
    // Server: in flight the open breaches leak hull HP, in the steps of the leak rule.
    public sealed class ShipHullLeakSystem : ITickable {
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IShipBreachCondition _shipBreachCondition;
        private readonly IShipHullLeakRule _shipHullLeakRule;
        private readonly IShipHull _shipHull;

        private float _pendingLeak;

        public ShipHullLeakSystem(
            IShipRunBindingModel shipRunBindingModel,
            IShipBreachCondition shipBreachCondition,
            IShipHullLeakRule shipHullLeakRule,
            IShipHull shipHull) {
            _shipRunBindingModel = shipRunBindingModel;
            _shipBreachCondition = shipBreachCondition;
            _shipHullLeakRule = shipHullLeakRule;
            _shipHull = shipHull;
        }

        public void Tick() {
            ShipBase ship = _shipRunBindingModel.Ship;
            bool isFlyingServer = NetworkServer.active && ship != null && ship.IsFlying;
            int openBreaches = isFlyingServer ? _shipBreachCondition.OpenCount : 0;
            ShipHullLeakStep step = _shipHullLeakRule.Advance(_pendingLeak, openBreaches, Time.deltaTime);
            _pendingLeak = step.PendingLeak;
            if (step.DealtLeak > 0f)
                _shipHull.ServerApplyDamage(new ShipDamage(step.DealtLeak, ShipDamageCause.HullLeak, ship.transform.position));
        }
    }
}
