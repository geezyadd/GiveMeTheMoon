#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.GameCoreModule.Contracts;
using Features.ShipModule.Scripts.Hull;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Features.ShipModule.Scripts.Debug {
    // Debug only, on the host: F7 hits the ship's hull with the configured debug damage, F10 takes it to 0. Both go
    // through the hull service like a real hit, so armor, the synced stats and the hull events all see them.
    public sealed class ShipHullDebugSystem : ITickable, IShipHullDebug {
        // More than any hull and armor can absorb: the hull goes to 0 whatever its max and armor.
        private const float LETHAL_DAMAGE = float.MaxValue;

        private readonly IShipHull _shipHull;
        private readonly ShipHullConfiguration _shipHullConfiguration;

        public ShipHullDebugSystem(IShipHull shipHull, ShipHullConfiguration shipHullConfiguration) {
            _shipHull = shipHull;
            _shipHullConfiguration = shipHullConfiguration;
        }

        public void Tick() {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || NetworkServer.active == false)
                return;

            if (keyboard.f7Key.wasPressedThisFrame)
                DebugDamage(_shipHullConfiguration.DebugDamage);
            else if (keyboard.f10Key.wasPressedThisFrame)
                DebugDestroy();
        }

        public void DebugDamage(float amount) =>
            _shipHull.ServerApplyDamage(new ShipDamage(amount, ShipDamageCause.Debug, Vector3.zero));

        public void DebugDestroy() =>
            DebugDamage(LETHAL_DAMAGE);
    }
}
#endif
