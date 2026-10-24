using UnityEngine;

namespace Features.ShipModule.Scripts.Damage {
    // Every open breach leaks the configured HP per second. The leak is gathered and dealt in steps of the configured
    // size, so the hull bar flashes about once per step instead of every frame; with no breach nothing is kept.
    public sealed class ShipHullLeakRule : IShipHullLeakRule {
        private readonly ShipDamageConfiguration _shipDamageConfiguration;

        public ShipHullLeakRule(ShipDamageConfiguration shipDamageConfiguration) =>
            _shipDamageConfiguration = shipDamageConfiguration;

        public ShipHullLeakStep Advance(float pendingLeak, int openBreaches, float deltaTime) {
            if (openBreaches <= 0)
                return new ShipHullLeakStep(0f, 0f);

            float pending = pendingLeak + _shipDamageConfiguration.LeakHullPerSecond * openBreaches * Mathf.Max(0f, deltaTime);
            if (pending < _shipDamageConfiguration.LeakStepHull)
                return new ShipHullLeakStep(pending, 0f);

            return new ShipHullLeakStep(0f, pending);
        }
    }
}
