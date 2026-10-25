#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.DebugModule.Contracts;
using Features.GameCoreModule.Contracts;
using Features.ShipModule.Scripts.Hull;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Debug {
    // Debug only, on the host: the debug window's "Hit the hull" and "Destroy the hull" actions. Both go through the
    // hull service like a real hit, so armor, the synced stats and the hull events all see them.
    public sealed class ShipHullDebugSystem : IInitializable, IDisposable, IShipHullDebug {
        // More than any hull and armor can absorb: the hull goes to 0 whatever its max and armor.
        private const float LETHAL_DAMAGE = float.MaxValue;
        private const string DAMAGE_LABEL = "Hit the hull";
        private const string DAMAGE_DESCRIPTION = "Damages the ship's hull by the amount, like a real hit: armor applies and the hull bar flashes.";
        private const string DAMAGE_VALUE_LABEL = "Damage";
        private const string DESTROY_LABEL = "Destroy the hull (hull = 0)";
        private const string DESTROY_DESCRIPTION = "Takes the hull to 0: the whole crew goes back to the lobby, as when the hull is destroyed in a run.";

        private readonly IShipHull _shipHull;
        private readonly ShipHullConfiguration _shipHullConfiguration;
        private readonly IDebugActionRegistry _debugActionRegistry;
        private readonly IShipDebugActionConditions _shipDebugActionConditions;
        private readonly List<IDisposable> _registrations = new();

        public ShipHullDebugSystem(
            IShipHull shipHull,
            ShipHullConfiguration shipHullConfiguration,
            IDebugActionRegistry debugActionRegistry,
            IShipDebugActionConditions shipDebugActionConditions) {
            _shipHull = shipHull;
            _shipHullConfiguration = shipHullConfiguration;
            _debugActionRegistry = debugActionRegistry;
            _shipDebugActionConditions = shipDebugActionConditions;
        }

        public void Initialize() {
            DebugActionValue damage = new(DAMAGE_VALUE_LABEL, _shipHullConfiguration.DebugDamage);
            _registrations.Add(_debugActionRegistry.Register(new DebugActionDefinition(DebugSections.SHIP, DAMAGE_LABEL, DAMAGE_DESCRIPTION, _shipDebugActionConditions.ReadHostReason, damage, DebugDamage)));
            _registrations.Add(_debugActionRegistry.Register(new DebugActionDefinition(DebugSections.SHIP, DESTROY_LABEL, DESTROY_DESCRIPTION, _shipDebugActionConditions.ReadHostReason, DebugDestroy)));
        }

        public void Dispose() {
            foreach (IDisposable registration in _registrations)
                registration.Dispose();

            _registrations.Clear();
        }

        public void DebugDamage(float amount) =>
            _shipHull.ServerApplyDamage(new ShipDamage(amount, ShipDamageCause.Debug, Vector3.zero));

        public void DebugDestroy() =>
            DebugDamage(LETHAL_DAMAGE);
    }
}
#endif
