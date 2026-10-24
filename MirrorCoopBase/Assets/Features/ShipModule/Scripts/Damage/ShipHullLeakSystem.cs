using Features.GameCoreModule.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Damage {
    // Server: in flight every open breach leaks hull HP. The leak is gathered and dealt in whole HP steps, so the
    // hull bar flashes about once per lost point instead of every frame.
    public sealed class ShipHullLeakSystem : ITickable {
        private const float LEAK_STEP_HULL = 1f;

        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IShipBreachCondition _shipBreachCondition;
        private readonly IShipHull _shipHull;
        private readonly ShipDamageConfiguration _shipDamageConfiguration;

        private float _pendingLeak;

        public ShipHullLeakSystem(
            IShipRunBindingModel shipRunBindingModel,
            IShipBreachCondition shipBreachCondition,
            IShipHull shipHull,
            ShipDamageConfiguration shipDamageConfiguration) {
            _shipRunBindingModel = shipRunBindingModel;
            _shipBreachCondition = shipBreachCondition;
            _shipHull = shipHull;
            _shipDamageConfiguration = shipDamageConfiguration;
        }

        public void Tick() {
            if (IsLeaking() == false) {
                _pendingLeak = 0f;
                return;
            }

            _pendingLeak += _shipDamageConfiguration.LeakHullPerSecond * _shipBreachCondition.OpenCount * Time.deltaTime;
            if (_pendingLeak < LEAK_STEP_HULL)
                return;

            _shipHull.ServerApplyDamage(new ShipDamage(_pendingLeak, ShipDamageCause.HullLeak, _shipRunBindingModel.Ship.transform.position));
            _pendingLeak = 0f;
        }

        private bool IsLeaking() {
            ShipBase ship = _shipRunBindingModel.Ship;
            return NetworkServer.active && ship != null && ship.IsFlying && _shipBreachCondition.OpenCount > 0;
        }
    }
}
